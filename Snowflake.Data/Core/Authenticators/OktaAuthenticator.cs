/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

#nullable enable

using System.Net;
using Tortuga.Data.Snowflake.Core.Messages;
using Tortuga.Data.Snowflake.Core.RequestProcessing;
using Tortuga.Data.Snowflake.Core.Sessions;
using Tortuga.HttpClientUtilities;

namespace Tortuga.Data.Snowflake.Core.Authenticators;

/// <summary>
/// OktaAuthenticator would perform serveral steps of authentication with Snowflake and Okta idp
/// </summary>
class OktaAuthenticator : Authenticator
{
	readonly string m_AuthName;

	/// <summary>
	/// url of the okta idp
	/// </summary>
	readonly Uri m_OktaUrl;

	// The raw Saml token.
	string? m_SamlRawHtmlString;

	/// <summary>
	/// Constructor of the Okta authenticator
	/// </summary>
	/// <param name="session"></param>
	/// <param name="oktaUriString"></param>
	internal OktaAuthenticator(SFSession session, string oktaUriString) :
		base(session)
	{
		m_AuthName = oktaUriString;
		m_OktaUrl = new Uri(oktaUriString);
	}

	protected override string AuthName => m_AuthName;

	public override void Login()
	{
		var authenticatorRestRequest = BuildAuthenticatorRestRequest();
		var authenticatorResponse = Session.restRequester.Post<AuthenticatorResponse>(authenticatorRestRequest);
		authenticatorResponse.FilterFailedResponse();
		var ssoUrl = new Uri(authenticatorResponse.data!.ssoUrl!);
		var tokenUrl = new Uri(authenticatorResponse.data!.tokenUrl!);

		VerifyUrls(ssoUrl, m_OktaUrl);
		VerifyUrls(tokenUrl, m_OktaUrl);

		var idpTokenRestRequest = BuildIdpTokenRestRequest(tokenUrl);
		var idpResponse = Session.restRequester.Post<IdpTokenResponse>(idpTokenRestRequest);
		var onetimeToken = idpResponse.CookieToken;

		var samlRestRequest = BuildSAMLRestRequest(ssoUrl, onetimeToken);
		using (var samlRawResponse = Session.restRequester.Get(samlRestRequest))
		{
			m_SamlRawHtmlString = samlRawResponse.Content.ReadAsString();
		}

		VerifyPostbackUrl();

		base.Login();
	}

	/// <see cref="IAuthenticator"/>
	async public override Task LoginAsync(CancellationToken cancellationToken)
	{
		var authenticatorRestRequest = BuildAuthenticatorRestRequest();
		var authenticatorResponse = await Session.restRequester.PostAsync<AuthenticatorResponse>(authenticatorRestRequest, cancellationToken).ConfigureAwait(false);
		authenticatorResponse.FilterFailedResponse();
		var ssoUrl = new Uri(authenticatorResponse.data!.ssoUrl!);
		var tokenUrl = new Uri(authenticatorResponse.data!.tokenUrl!);

		VerifyUrls(ssoUrl, m_OktaUrl);
		VerifyUrls(tokenUrl, m_OktaUrl);

		var idpTokenRestRequest = BuildIdpTokenRestRequest(tokenUrl);
		var idpResponse = await Session.restRequester.PostAsync<IdpTokenResponse>(idpTokenRestRequest, cancellationToken).ConfigureAwait(false);
		var onetimeToken = idpResponse.CookieToken;

		var samlRestRequest = BuildSAMLRestRequest(ssoUrl, onetimeToken);
		using (var samlRawResponse = await Session.restRequester.GetAsync(samlRestRequest, cancellationToken).ConfigureAwait(false))
		{
			m_SamlRawHtmlString = await samlRawResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
		}

		VerifyPostbackUrl();

		await base.LoginAsync(cancellationToken).ConfigureAwait(false);
	}

	/// <see cref="BaseAuthenticator.SetSpecializedAuthenticatorData(LoginRequestData)"/>
	protected override void SetSpecializedAuthenticatorData(LoginRequestData data)
	{
		data.RawSamlResponse = m_SamlRawHtmlString;
	}

	SFRestRequest BuildAuthenticatorRestRequest()
	{
		var fedUrl = Session.BuildUri(RestPath.SF_AUTHENTICATOR_REQUEST_PATH);
		var data = new AuthenticatorRequestData()
		{
			AccountName = Session.properties[SFSessionProperty.ACCOUNT],
			Authenticator = m_OktaUrl.ToString(),
		};

		//int connectionTimeoutSec = int.Parse(Session.Properties[SFSessionProperty.ConnectionTimeout]);

		return Session.BuildTimeoutRestRequest(fedUrl, new AuthenticatorRequest() { Data = data });
	}

	IdpTokenRestRequest BuildIdpTokenRestRequest(Uri tokenUrl)
	{
		return new IdpTokenRestRequest()
		{
			Url = tokenUrl,
			RestTimeout = Session.connectionTimeout,
			HttpTimeout = TimeSpan.FromSeconds(16),
			JsonBody = new IdpTokenRequest()
			{
				Username = Session.properties[SFSessionProperty.USER],
				Password = Session.properties[SFSessionProperty.PASSWORD],
			},
		};
	}

	SAMLRestRequest BuildSAMLRestRequest(Uri ssoUrl, string? onetimeToken)
	{
		return new SAMLRestRequest()
		{
			Url = ssoUrl,
			RestTimeout = Session.connectionTimeout,
			HttpTimeout = Timeout.InfiniteTimeSpan,
			OnetimeToken = onetimeToken,
		};
	}

	void VerifyPostbackUrl()
	{
		if (m_SamlRawHtmlString == null)
			throw new NullReferenceException($"Internal error. {nameof(m_SamlRawHtmlString)} should have been set previously.");

		var formIndex = m_SamlRawHtmlString.IndexOf("<form");

		// skip 'action="' (length = 8)
		var startIndex = m_SamlRawHtmlString.IndexOf("action=", formIndex) + 8;
		var length = m_SamlRawHtmlString.IndexOf('"', startIndex) - startIndex;

		Uri postBackUrl;
		try
		{
			postBackUrl = new Uri(WebUtility.HtmlDecode(m_SamlRawHtmlString.Substring(startIndex, length)));
		}
		catch (Exception e)
		{
			throw new SnowflakeDbException(SFError.IDP_SAML_POSTBACK_NOTFOUND, e);
		}

		var sessionHost = Session.properties[SFSessionProperty.HOST];
		var sessionScheme = Session.properties[SFSessionProperty.SCHEME];
		if (postBackUrl.Host != sessionHost ||
			postBackUrl.Scheme != sessionScheme)
		{
			throw new SnowflakeDbException(SFError.IDP_SAML_POSTBACK_INVALID, postBackUrl.ToString(), sessionScheme + ":\\\\" + sessionHost);
		}
	}

	void VerifyUrls(Uri tokenOrSsoUrl, Uri sessionUrl)
	{
		if (tokenOrSsoUrl.Scheme != sessionUrl.Scheme || tokenOrSsoUrl.Host != sessionUrl.Host)
		{
			throw new SnowflakeDbException(SFError.IDP_SSO_TOKEN_URL_MISMATCH, tokenOrSsoUrl.ToString(), m_OktaUrl.ToString());
		}
	}
}
