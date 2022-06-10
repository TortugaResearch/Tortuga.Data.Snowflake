/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Tortuga.Data.Snowflake.Core.Messages;
using Tortuga.Data.Snowflake.Core.Sessions;
using static Tortuga.Data.Snowflake.Core.Sessions.SFSessionProperty;
using static Tortuga.Data.Snowflake.SFError;

namespace Tortuga.Data.Snowflake.Core.Authenticators;

/// <summary>
/// KeyPairAuthenticator is used for Key pair based authentication.
/// See <see cref="https://docs.snowflake.com/en/user-guide/key-pair-auth.html"/> for more information.
/// </summary>
class KeyPairAuthenticator : Authenticator
{
	// The authenticator setting value to use to authenticate using key pair authentication.
	public const string AUTH_NAME = "snowflake_jwt";

	// The RSA provider to use to sign the tokens
	readonly RSACryptoServiceProvider m_RsaProvider;

	// The jwt token to send in the login request.
	string? m_JwtToken;

	/// <summary>
	/// Constructor for the Key-Pair authenticator.
	/// </summary>
	/// <param name="session">Session which created this authenticator</param>
	internal KeyPairAuthenticator(SFSession session) : base(session)
	{
		// Get private key path or private key from connection settings
		if (!session.m_Properties.ContainsKey(PRIVATE_KEY_FILE) && !session.m_Properties.ContainsKey(PRIVATE_KEY))
		{
			// There is no PRIVATE_KEY_FILE defined, can't authenticate with key-pair
			throw new SnowflakeDbException(INVALID_CONNECTION_STRING, "Missing required PRIVATE_KEY_FILE or PRIVATE_KEY for key pair authentication");
		}

		m_RsaProvider = new RSACryptoServiceProvider();
	}

	protected override string AuthName => AUTH_NAME;

	/// <see cref="IAuthenticator.Login"/>
	public override void Login()
	{
		m_JwtToken = GenerateJwtToken();

		// Send the http request with the generate token
		base.Login();
	}

	/// <see cref="IAuthenticator.LoginAsync"/>
	async public override Task LoginAsync(CancellationToken cancellationToken)
	{
		m_JwtToken = GenerateJwtToken();

		// Send the http request with the generate token
		await base.LoginAsync(cancellationToken).ConfigureAwait(false);
	}

	/// <see cref="BaseAuthenticator.SetSpecializedAuthenticatorData(LoginRequestData)"/>
	protected override void SetSpecializedAuthenticatorData(LoginRequestData data)
	{
		// Add the token to the Data attribute
		data.Token = m_JwtToken;
	}

	/// <summary>
	/// Generates a JwtToken to use for login.
	/// </summary>
	/// <returns>The generated JWT token.</returns>
	private string GenerateJwtToken()
	{
		var hasPkPath = Session.m_Properties.TryGetValue(PRIVATE_KEY_FILE, out var pkPath);
		var hasPkContent = Session.m_Properties.TryGetValue(PRIVATE_KEY, out var pkContent);
		Session.m_Properties.TryGetValue(PRIVATE_KEY_PWD, out var pkPwd);

		if (!hasPkPath && !hasPkContent)
			throw new InvalidOperationException($"Either {nameof(PRIVATE_KEY_FILE)} or {nameof(PRIVATE_KEY)} needs to be set in the {nameof(Session.m_Properties)}");

		// Extract the public key from the private key to generate the fingerprints
		RSAParameters rsaParams;
		String? publicKeyFingerPrint = null;
		AsymmetricCipherKeyPair? keypair = null;
		using (TextReader tr = hasPkPath ? (TextReader)new StreamReader(pkPath!) : new StringReader(pkContent!))
		{
			try
			{
				PemReader? pr = null;
				if (null != pkPwd)
				{
					var ipwdf = new PasswordFinder(pkPwd);
					pr = new PemReader(tr, ipwdf);
				}
				else
				{
					pr = new PemReader(tr);
				}

				object key = pr.ReadObject();
				// Infer what the pem reader is sending back based on the object properties
				if (key.GetType().GetProperty("Private") != null)
				{
					// PKCS1 key
					keypair = (AsymmetricCipherKeyPair)key;
					rsaParams = DotNetUtilities.ToRSAParameters(
					keypair.Private as RsaPrivateCrtKeyParameters);
				}
				else
				{
					// PKCS8 key
					RsaPrivateCrtKeyParameters pk = (RsaPrivateCrtKeyParameters)key;
					rsaParams = DotNetUtilities.ToRSAParameters(pk);
					keypair = DotNetUtilities.GetRsaKeyPair(rsaParams);
				}
				if (keypair == null)
				{
					throw new Exception("Unknown error.");
				}
			}
			catch (Exception e)
			{
				throw new SnowflakeDbException(JWT_ERROR_READING_PK, hasPkPath ? pkPath : "with value passed in connection string", e.ToString(), e);
			}
		}

		// Generate the public key fingerprint
		var publicKey = keypair.Public;
		var publicKeyEncoded = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(publicKey).GetDerEncoded();
		using (var SHA256Encoder = SHA256.Create())
		{
			var sha256Hash = SHA256Encoder.ComputeHash(publicKeyEncoded);
			publicKeyFingerPrint = "SHA256:" + Convert.ToBase64String(sha256Hash);
		}

		// Generating the token
		var now = DateTime.UtcNow;
		var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		var secondsSinceEpoch = (long)((now - dtDateTime).TotalSeconds);

		/*
		 * Payload content
		 *      iss : $accountName.$userName.$pulicKeyFingerprint
		 *      sub : $accountName.$userName
		 *      iat : $now
		 *      exp : $now + LIFETIME
		 *
		 * Note : Lifetime = 120sec for Python impl, 60sec for Jdbc and Odbc
		*/
		var accountUser = Session.m_Properties[SFSessionProperty.ACCOUNT].ToUpper() + "." + Session.m_Properties[SFSessionProperty.USER].ToUpper();
		var issuer = accountUser + "." + publicKeyFingerPrint;
		var claims = new[] {
						new Claim(JwtRegisteredClaimNames.Iat, secondsSinceEpoch.ToString(), ClaimValueTypes.Integer64),
						new Claim(JwtRegisteredClaimNames.Sub, accountUser),
					};

		m_RsaProvider.ImportParameters(rsaParams);
		var token = new JwtSecurityToken(
			issuer: issuer,
			audience: null,
			claims: claims,
			notBefore: null,
			expires: now.AddSeconds(60),
			signingCredentials: new SigningCredentials(new RsaSecurityKey(m_RsaProvider), SecurityAlgorithms.RsaSha256)
		);

		// Serialize the jwt token
		// Base64URL-encoded parts delimited by period ('.'), with format :
		//     [header-base64url].[payload-base64url].[signature-base64url]
		var handler = new JwtSecurityTokenHandler();
		var jwtToken = handler.WriteToken(token);

		return jwtToken;
	}

	/// <summary>
	/// Helper class to handle the password for the certificate if there is one.
	/// </summary>
	class PasswordFinder : IPasswordFinder
	{
		// The password.
		string m_Password;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="password">The password.</param>
		public PasswordFinder(string password)
		{
			m_Password = password;
		}

		/// <summary>
		/// Returns the password or null if the password is empty or null.
		/// </summary>
		/// <returns>The password or null if the password is empty or null.</returns>
		public char[]? GetPassword()
		{
			if (string.IsNullOrEmpty(m_Password)) // No password.
				return null;
			else
				return m_Password.ToCharArray();
		}
	}
}
