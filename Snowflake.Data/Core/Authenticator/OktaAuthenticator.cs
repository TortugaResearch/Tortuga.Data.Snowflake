/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using System.Net;

namespace Tortuga.Data.Snowflake.Core.Authenticator;

/// <summary>
/// OktaAuthenticator would perform serveral steps of authentication with Snowflake and Okta idp
/// </summary>
class OktaAuthenticator : BaseAuthenticator, IAuthenticator
{
	/// <summary>
	/// url of the okta idp
	/// </summary>
	private Uri oktaUrl;

	// The raw Saml token.
	private string samlRawHtmlString;

	/// <summary>
	/// Constructor of the Okta authenticator
	/// </summary>
	/// <param name="session"></param>
	/// <param name="oktaUriString"></param>
	internal OktaAuthenticator(SFSession session, string oktaUriString) :
		base(session, oktaUriString)
	{
		oktaUrl = new Uri(oktaUriString);
	}

	/// <see cref="IAuthenticator"/>
	async Task IAuthenticator.AuthenticateAsync(CancellationToken cancellationToken)
	{
		var lastStep = "";
		try
		{
			lastStep = "step 1: get sso and token url";
			var authenticatorRestRequest = BuildAuthenticatorRestRequest();
			var authenticatorResponse = await session.restRequester.PostAsync<AuthenticatorResponse>(authenticatorRestRequest, cancellationToken).ConfigureAwait(false);
			authenticatorResponse.FilterFailedResponse();
			Uri ssoUrl = new Uri(authenticatorResponse.data.ssoUrl);
			Uri tokenUrl = new Uri(authenticatorResponse.data.tokenUrl);

			lastStep = "step 2: verify sso url fetched from step 1";
			VerifyUrls(ssoUrl, oktaUrl);
			lastStep = "step 2: verify token url fetched from step 1";
			VerifyUrls(tokenUrl, oktaUrl);

			lastStep = "step 3: get idp onetime token";
			IdpTokenRestRequest idpTokenRestRequest = BuildIdpTokenRestRequest(tokenUrl);
			var idpResponse = await session.restRequester.PostAsync<IdpTokenResponse>(idpTokenRestRequest, cancellationToken).ConfigureAwait(false);
			string onetimeToken = idpResponse.CookieToken;

			lastStep = "step 4: get SAML reponse from sso";
			var samlRestRequest = BuildSAMLRestRequest(ssoUrl, onetimeToken);
			using (var samlRawResponse = await session.restRequester.GetAsync(samlRestRequest, cancellationToken).ConfigureAwait(false))
			{
				samlRawHtmlString = await samlRawResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
			}

			lastStep = "step 5: verify postback url in SAML reponse";
			VerifyPostbackUrl();

			lastStep = "step 6: send SAML reponse to snowflake to login";
			await base.LoginAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex) when (ex is not SnowflakeDbException)
		{
			throw new SnowflakeDbException("Okta Authentication in " + lastStep, ex, SFError.INTERNAL_ERROR);
		}
	}

	void IAuthenticator.Authenticate()
	{
		var lastStep = "";
		try
		{
			lastStep = "step 1: get sso and token url";
			var authenticatorRestRequest = BuildAuthenticatorRestRequest();
			var authenticatorResponse = session.restRequester.Post<AuthenticatorResponse>(authenticatorRestRequest);
			authenticatorResponse.FilterFailedResponse();
			Uri ssoUrl = new Uri(authenticatorResponse.data.ssoUrl);
			Uri tokenUrl = new Uri(authenticatorResponse.data.tokenUrl);

			lastStep = "step 2: verify sso url fetched from step 1";
			VerifyUrls(ssoUrl, oktaUrl);
			lastStep = "step 2: verify token url fetched from step 1";
			VerifyUrls(tokenUrl, oktaUrl);

			lastStep = "step 3: get idp onetime token";
			IdpTokenRestRequest idpTokenRestRequest = BuildIdpTokenRestRequest(tokenUrl);
			var idpResponse = session.restRequester.Post<IdpTokenResponse>(idpTokenRestRequest);
			string onetimeToken = idpResponse.CookieToken;

			lastStep = "step 4: get SAML reponse from sso";
			var samlRestRequest = BuildSAMLRestRequest(ssoUrl, onetimeToken);
			using (var samlRawResponse = session.restRequester.Get(samlRestRequest))
			{
				samlRawHtmlString = Task.Run(async () => await samlRawResponse.Content.ReadAsStringAsync().ConfigureAwait(false)).Result;
			}

			lastStep = "step 5: verify postback url in SAML reponse";
			VerifyPostbackUrl();

			lastStep = "step 6: send SAML reponse to snowflake to login";
			base.Login();
		}
		catch (Exception ex) when (ex is not SnowflakeDbException)
		{
			throw new SnowflakeDbException("Okta Authentication in " + lastStep, ex, SFError.INTERNAL_ERROR);
		}
	}

	private SFRestRequest BuildAuthenticatorRestRequest()
	{
		var fedUrl = session.BuildUri(RestPath.SF_AUTHENTICATOR_REQUEST_PATH);
		var data = new AuthenticatorRequestData()
		{
			AccountName = session.properties[SFSessionProperty.ACCOUNT],
			Authenticator = oktaUrl.ToString(),
		};

		int connectionTimeoutSec = int.Parse(session.properties[SFSessionProperty.CONNECTION_TIMEOUT]);

		return session.BuildTimeoutRestRequest(fedUrl, new AuthenticatorRequest() { Data = data });
	}

	private IdpTokenRestRequest BuildIdpTokenRestRequest(Uri tokenUrl)
	{
		return new IdpTokenRestRequest()
		{
			Url = tokenUrl,
			RestTimeout = session.connectionTimeout,
			HttpTimeout = TimeSpan.FromSeconds(16),
			JsonBody = new IdpTokenRequest()
			{
				Username = session.properties[SFSessionProperty.USER],
				Password = session.properties[SFSessionProperty.PASSWORD],
			},
		};
	}

	private SAMLRestRequest BuildSAMLRestRequest(Uri ssoUrl, string onetimeToken)
	{
		return new SAMLRestRequest()
		{
			Url = ssoUrl,
			RestTimeout = session.connectionTimeout,
			HttpTimeout = Timeout.InfiniteTimeSpan,
			OnetimeToken = onetimeToken,
		};
	}

	/// <see cref="BaseAuthenticator.SetSpecializedAuthenticatorData(ref LoginRequestData)"/>
	protected override void SetSpecializedAuthenticatorData(ref LoginRequestData data)
	{
		data.RawSamlResponse = samlRawHtmlString;
	}

	private void VerifyUrls(Uri tokenOrSsoUrl, Uri sessionUrl)
	{
		if (tokenOrSsoUrl.Scheme != sessionUrl.Scheme || tokenOrSsoUrl.Host != sessionUrl.Host)
		{
			throw new SnowflakeDbException(SFError.IDP_SSO_TOKEN_URL_MISMATCH, tokenOrSsoUrl.ToString(), oktaUrl.ToString());
		}
	}

	private void VerifyPostbackUrl()
	{
		int formIndex = samlRawHtmlString.IndexOf("<form");
		bool extractSuccess = formIndex == -1;

		// skip 'action="' (length = 8)
		int startIndex = samlRawHtmlString.IndexOf("action=", formIndex) + 8;
		int length = samlRawHtmlString.IndexOf('"', startIndex) - startIndex;

		Uri postBackUrl;
		try
		{
			postBackUrl = new Uri(WebUtility.HtmlDecode(samlRawHtmlString.Substring(startIndex, length)));
		}
		catch (Exception e)
		{
			throw new SnowflakeDbException(e, SFError.IDP_SAML_POSTBACK_NOTFOUND);
		}

		string sessionHost = session.properties[SFSessionProperty.HOST];
		string sessionScheme = session.properties[SFSessionProperty.SCHEME];
		if (postBackUrl.Host != sessionHost ||
			postBackUrl.Scheme != sessionScheme)
		{
			throw new SnowflakeDbException(SFError.IDP_SAML_POSTBACK_INVALID, postBackUrl.ToString(), sessionScheme + ":\\\\" + sessionHost);
		}
	}

	private void FilterFailedResponse(BaseRestResponse response)
	{
		if (!response.success)
		{
			throw new SnowflakeDbException("", response.code, response.message, "");
		}
	}
}
