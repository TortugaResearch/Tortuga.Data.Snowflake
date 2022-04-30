/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using System.Net;
using System.Security;
using Tortuga.Data.Snowflake.Core.Authenticator;
using Tortuga.Data.Snowflake.Log;

namespace Tortuga.Data.Snowflake.Core;

class SFSessionProperties : Dictionary<SFSessionProperty, string>
{
	static private SFLogger logger = SFLoggerFactory.GetLogger<SFSessionProperties>();

	// Connection string properties to obfuscate in the log
	static private List<SFSessionProperty> secretProps =
		new List<SFSessionProperty>{
			SFSessionProperty.PASSWORD,
			SFSessionProperty.PRIVATE_KEY,
			SFSessionProperty.TOKEN,
			SFSessionProperty.PRIVATE_KEY_PWD,
			SFSessionProperty.PROXYPASSWORD,
		};

	public override bool Equals(object obj)
	{
		if (obj == null) return false;
		try
		{
			SFSessionProperties prop = (SFSessionProperties)obj;
			foreach (SFSessionProperty sessionProperty in Enum.GetValues(typeof(SFSessionProperty)))
			{
				if (ContainsKey(sessionProperty) ^ prop.ContainsKey(sessionProperty))
				{
					return false;
				}
				if (!ContainsKey(sessionProperty))
				{
					continue;
				}
				if (!this[sessionProperty].Equals(prop[sessionProperty]))
				{
					return false;
				}
			}
			return true;
		}
		catch (InvalidCastException)
		{
			logger.Warn("Invalid casting to SFSessionProperties");
			return false;
		}
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	internal static SFSessionProperties parseConnectionString(string connectionString, SecureString password)
	{
		logger.Info("Start parsing connection string.");
		SFSessionProperties properties = new SFSessionProperties();

		string[] propertyEntry = connectionString.Split(';');

		foreach (string keyVal in propertyEntry)
		{
			if (keyVal.Length > 0)
			{
				string[] tokens = keyVal.Split(new string[] { "=" }, StringSplitOptions.None);
				if (tokens.Length != 2)
				{
					// https://docs.microsoft.com/en-us/dotnet/api/system.data.oledb.oledbconnection.connectionstring
					// To include an equal sign (=) in a keyword or value, it must be preceded
					// by another equal sign. For example, in the hypothetical connection
					// string "key==word=value" :
					// the keyword is "key=word" and the value is "value".
					int currentIndex = 0;
					int singleEqualIndex = -1;
					while (currentIndex <= keyVal.Length)
					{
						currentIndex = keyVal.IndexOf("=", currentIndex);
						if (-1 == currentIndex)
						{
							// No '=' found
							break;
						}
						if (currentIndex < keyVal.Length - 1 &&
							'=' != keyVal[currentIndex + 1])
						{
							if (0 > singleEqualIndex)
							{
								// First single '=' encountered
								singleEqualIndex = currentIndex;
								currentIndex++;
							}
							else
							{
								// Found another single '=' which is not allowed
								singleEqualIndex = -1;
								break;
							}
						}
						else
						{
							// skip the doubled one
							currentIndex += 2;
						}
					}

					if (singleEqualIndex > 0 && singleEqualIndex < keyVal.Length - 1)
					{
						// Split the key/value at the right index and deduplicate '=='
						tokens = new string[2];
						tokens[0] = keyVal.Substring(0, singleEqualIndex).Replace("==", "=");
						tokens[1] = keyVal.Substring(
							singleEqualIndex + 1,
							keyVal.Length - (singleEqualIndex + 1)).Replace("==", "="); ;
					}
					else
					{
						// An equal sign was not doubled or something else happened
						// making the connection invalid
						string invalidStringDetail =
							string.Format("Invalid key value pair {0}", keyVal);
						SnowflakeDbException e =
							new SnowflakeDbException(
								SFError.INVALID_CONNECTION_STRING,
								new object[] { invalidStringDetail });
						logger.Error("Invalid string.", e);
						throw e;
					}
				}

				try
				{
					SFSessionProperty p = (SFSessionProperty)Enum.Parse(
						typeof(SFSessionProperty), tokens[0].ToUpper());
					properties.Add(p, tokens[1]);
					logger.Info($"Connection property: {p}, value: {(secretProps.Contains(p) ? "XXXXXXXX" : tokens[1])}");
				}
				catch (ArgumentException e)
				{
					logger.Warn($"Property {tokens[0]} not found ignored.", e);
				}
			}
		}

		bool useProxy = false;
		if (properties.ContainsKey(SFSessionProperty.USEPROXY))
		{
			try
			{
				useProxy = bool.Parse(properties[SFSessionProperty.USEPROXY]);
			}
			catch (Exception e)
			{
				// The useProxy setting is not a valid boolean value
				logger.Error("Unable to connect", e);
				throw new SnowflakeDbException(e,
							SFError.INVALID_CONNECTION_STRING,
							e.Message);
			}
		}

		// Based on which proxy settings have been provided, update the required settings list
		if (useProxy)
		{
			// If useProxy is true, then proxyhost and proxy port are mandatory
			SFSessionProperty.PROXYHOST.GetAttribute<SFSessionPropertyAttr>().required = true;
			SFSessionProperty.PROXYPORT.GetAttribute<SFSessionPropertyAttr>().required = true;

			// If a username is provided, then a password is required
			if (properties.ContainsKey(SFSessionProperty.PROXYUSER))
			{
				SFSessionProperty.PROXYPASSWORD.GetAttribute<SFSessionPropertyAttr>().required = true;
			}
		}

		if (password != null)
		{
			properties[SFSessionProperty.PASSWORD] = new NetworkCredential(string.Empty, password).Password;
		}

		checkSessionProperties(properties);

		// compose host value if not specified
		if (!properties.ContainsKey(SFSessionProperty.HOST) ||
			0 == properties[SFSessionProperty.HOST].Length)
		{
			string hostName = string.Format("{0}.snowflakecomputing.com", properties[SFSessionProperty.ACCOUNT]);
			// Remove in case it's here but empty
			properties.Remove(SFSessionProperty.HOST);
			properties.Add(SFSessionProperty.HOST, hostName);
			logger.Info($"Compose host name: {hostName}");
		}

		// Trim the account name to remove the region and cloud platform if any were provided
		// because the login request data does not expect region and cloud information to be
		// passed on for account_name
		properties[SFSessionProperty.ACCOUNT] = properties[SFSessionProperty.ACCOUNT].Split('.')[0];

		return properties;
	}

	private static void checkSessionProperties(SFSessionProperties properties)
	{
		foreach (SFSessionProperty sessionProperty in Enum.GetValues(typeof(SFSessionProperty)))
		{
			// if required property, check if exists in the dictionary
			if (IsRequired(sessionProperty, properties) &&
				!properties.ContainsKey(sessionProperty))
			{
				SnowflakeDbException e = new SnowflakeDbException(SFError.MISSING_CONNECTION_PROPERTY,
					sessionProperty);
				logger.Error("Missing connection property", e);
				throw e;
			}

			// add default value to the map
			string defaultVal = sessionProperty.GetAttribute<SFSessionPropertyAttr>().defaultValue;
			if (defaultVal != null && !properties.ContainsKey(sessionProperty))
			{
				logger.Debug($"Sesssion property {sessionProperty} set to default value: {defaultVal}");
				properties.Add(sessionProperty, defaultVal);
			}
		}
	}

	private static bool IsRequired(SFSessionProperty sessionProperty, SFSessionProperties properties)
	{
		if (sessionProperty.Equals(SFSessionProperty.PASSWORD))
		{
			var authenticatorDefined =
				properties.TryGetValue(SFSessionProperty.AUTHENTICATOR, out var authenticator);

			// External browser, jwt and oauth don't require a password for authenticating
			return !(authenticatorDefined &&
					(authenticator.Equals(ExternalBrowserAuthenticator.AUTH_NAME,
						StringComparison.OrdinalIgnoreCase) ||
					authenticator.Equals(KeyPairAuthenticator.AUTH_NAME,
						StringComparison.OrdinalIgnoreCase) ||
					authenticator.Equals(OAuthAuthenticator.AUTH_NAME,
					StringComparison.OrdinalIgnoreCase)));
		}
		else if (sessionProperty.Equals(SFSessionProperty.USER))
		{
			var authenticatorDefined =
			   properties.TryGetValue(SFSessionProperty.AUTHENTICATOR, out var authenticator);

			// Oauth don't require a username for authenticating
			return !(authenticatorDefined &&
				authenticator.Equals(OAuthAuthenticator.AUTH_NAME, StringComparison.OrdinalIgnoreCase));
		}
		else
		{
			return sessionProperty.GetAttribute<SFSessionPropertyAttr>().required;
		}
	}
}
