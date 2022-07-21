/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Tortuga.Data.Snowflake;

#pragma warning disable CA1710 // Identifiers should have correct suffix
#pragma warning disable CA1308 // Normalize strings to uppercase

/// <summary>
/// Class SnowflakeDbConnectionStringBuilder.
/// Implements the <see cref="DbConnectionStringBuilder" />
/// Implements the <see cref="ICollection{KeyValuePair{string, object}}" />
/// Implements the <see cref="INotifyPropertyChanged" />
/// </summary>
/// <seealso cref="DbConnectionStringBuilder" />
/// <seealso cref="ICollection{KeyValuePair{string, object}}" />
/// <seealso cref="INotifyPropertyChanged" />
public class SnowflakeDbConnectionStringBuilder : DbConnectionStringBuilder, ICollection<KeyValuePair<string, object>>, INotifyPropertyChanged
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SnowflakeDbConnectionStringBuilder"/> class.
	/// </summary>
	public SnowflakeDbConnectionStringBuilder()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SnowflakeDbConnectionStringBuilder"/> class.
	/// </summary>
	/// <param name="conn">The connection.</param>
	public SnowflakeDbConnectionStringBuilder(string conn)
	{
		ConnectionString = conn;
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>
	/// Your full account name. Might include additional segments that identify the region and cloud platform where your account is hosted.
	/// </summary>
	/// <value>The account.</value>
	[Category("Required")]
	[Description("Your full account name. Might include additional segments that identify the region and cloud platform where your account is hosted.")]
	public string? Account
	{
		get => GetString(nameof(Account).ToLowerInvariant());
		set => this[nameof(Account).ToLowerInvariant()] = value;
	}

	/// <summary>
	/// Snowflake partner use only: Specifies the name of a partner application to connect through .NET. The name must match the following pattern: ^[A-Za-z]([A-Za-z0-9.-]){1,50}$ (one letter followed by 1 to 50 letter, digit, .,- or, _ characters).
	/// </summary>
	/// <value>The application.</value>
	[Category("Partner")]
	[Description("Snowflake partner use only: Specifies the name of a partner application to connect through .NET. The name must match the following pattern: ^[A-Za-z]([A-Za-z0-9.-]){1,50}$ (one letter followed by 1 to 50 letter, digit, .,- or, _ characters).")]
	public string? Application
	{
		get => GetString(nameof(Application).ToLowerInvariant());
		set => this[nameof(Application).ToLowerInvariant()] = value;
	}

	/// <summary>
	/// The method of authentication. Currently supports the following values:
	/// - snowflake(default) : You must also set USER and PASSWORD.
	/// - the URL for native SSO through Okta: You must also set USER and PASSWORD.
	/// - externalbrowser: You must also set USER.
	/// - snowflake_jwt: You must also set PRIVATE_KEY_FILE or PRIVATE_KEY.
	/// - oauth: You must also set TOKEN.
	/// </summary>
	/// <value>The authenticator.</value>
	[Category("Required")]
	[DefaultValue("snowflake")]
	[Description(@"The method of authentication. Currently supports the following values:
- snowflake (default): You must also set USER and PASSWORD.
- the URL for native SSO through Okta: You must also set USER and PASSWORD.
- externalbrowser: You must also set USER.
- snowflake_jwt: You must also set PRIVATE_KEY_FILE or PRIVATE_KEY.
- oauth: You must also set TOKEN.")]
	public string? Authenticator
	{
		get => GetString(nameof(Authenticator).ToLowerInvariant());
		set => this[nameof(Authenticator).ToLowerInvariant()] = value;
	}

	/// <summary>
	/// Total timeout in seconds when connecting to Snowflake. Default to 120 seconds.
	/// </summary>
	/// <value>The connection timeout.</value>
	[Category("Connection")]
	[DefaultValue(120)]
	[Description("Total timeout in seconds when connecting to Snowflake. Default to 120 seconds.")]
	public int ConnectionTimeout
	{
		get => GetInt("connection_timeout");
		set => this["connection_timeout"] = value;
	}

	/// <summary>
	/// Gets or sets the database.
	/// </summary>
	/// <value>The database.</value>
	[Category("Connection")]
	public string? DB
	{
		get => GetString(nameof(DB).ToLowerInvariant());
		set => this[nameof(DB).ToLowerInvariant()] = value;
	}

	/// <summary>
	/// Set this property to true to prevent the 6driver from reconnecting automatically when the connection fails or drops. The default value is false.
	/// </summary>
	[Category("Connection")]
	[DefaultValue(false)]
	[Description("Set this property to true to prevent the 6driver from reconnecting automatically when the connection fails or drops. The default value is false.")]
	public bool DisableRetry
	{
		get => GetBool(nameof(DisableRetry).ToLowerInvariant());
		set => this[nameof(DisableRetry).ToLowerInvariant()] = value;
	}

	/// <summary>
	/// Specifies the hostname for your account in the following format: <HOST>.snowflakecomputing.com.	If no value is specified, the driver uses<ACCOUNT>.snowflakecomputing.com.
	/// </summary>
	/// <value>The host.</value>
	[Category("Connection")]
	[Description(@"Specifies the hostname for your account in the following format: <HOST>.snowflakecomputing.com.
If no value is specified, the driver uses<ACCOUNT>.snowflakecomputing.com.")]
	public string? Host
	{
		get => GetString(nameof(Host).ToLowerInvariant());
		set => this[nameof(Host).ToLowerInvariant()] = value;
	}

	/// <summary>
	/// Set to true to disable the certificate revocation list check. Default is false.
	/// </summary>
	[Category("Connection")]
	[DefaultValue(false)]
	[Description("Set to true to disable the certificate revocation list check. Default is false.")]
	public bool InsecureMode
	{
		get => GetBool(nameof(InsecureMode).ToLowerInvariant());
		set => this[nameof(InsecureMode).ToLowerInvariant()] = value;
	}

	/// <summary>
	/// The list of hosts that the driver should connect to directly, bypassing the proxy server. Separate the hostnames with a pipe symbol (|). You can also use an asterisk (*) as a wildcard.
	/// </summary>
	/// <value>The non proxy hosts.</value>
	[Category("Proxy")]
	[Description("The list of hosts that the driver should connect to directly, bypassing the proxy server. Separate the hostnames with a pipe symbol (|). You can also use an asterisk (*) as a wildcard.")]
	public string? NonProxyHosts
	{
		get => GetString(nameof(NonProxyHosts).ToLowerInvariant());
		set => this[nameof(NonProxyHosts).ToLowerInvariant()] = value;
	}

	/// <summary>
	/// Required if AUTHENTICATOR is set to 'snowflake' (the default value) or the URL for native SSO through Okta. Ignored for all the other authentication types.
	/// </summary>
	/// <value>The password.</value>
	[Category("Auth")]
	[Description("Required if AUTHENTICATOR is set to 'snowflake' (the default value) or the URL for native SSO through Okta. Ignored for all the other authentication types.")]
	public string? Password
	{
		get => GetString(nameof(Password).ToLowerInvariant());
		set => this[nameof(Password).ToLowerInvariant()] = value;
	}

	/// <summary>
	/// The private key to use for key-pair authentication. Must be used in combination with AUTHENTICATOR=snowflake_jwt.
	/// </summary>
	/// <value>The private key.</value>
	[Category("Auth")]
	[Description("The private key to use for key-pair authentication. Must be used in combination with AUTHENTICATOR=snowflake_jwt.")]
	public string? PrivateKey
	{
		get => GetString("private_key");
		set => this["private_key"] = value;
	}

	/// <summary>
	/// The path to the private key file to use for key-pair authentication. Must be used in combination with AUTHENTICATOR=snowflake_jwt
	/// </summary>
	/// <value>The private key file.</value>
	[Category("Auth")]
	[Description("The path to the private key file to use for key-pair authentication. Must be used in combination with AUTHENTICATOR=snowflake_jwt")]
	public string? PrivateKeyFile
	{
		get => GetString("private_key_file");
		set => this["private_key_file"] = value;
	}

	/// <summary>
	/// The passphrase to use for decrypting the private key, if the key is encrypted.
	/// </summary>
	/// <value>The private key password.</value>
	[Category("Auth")]
	[Description("The passphrase to use for decrypting the private key, if the key is encrypted.")]
	public string? PrivateKeyPwd
	{
		get => GetString("private_key_pwd");
		set => this["private_key_pwd"] = value;
	}

	/// <summary>
	/// The hostname of the proxy server. If USEPROXY is set to true, you must set this parameter.
	/// </summary>
	/// <value>The proxy host.</value>
	[Category("Proxy")]
	[Description(@"The hostname of the proxy server.
If USEPROXY is set to true, you must set this parameter. ")]
	public string? ProxyHost
	{
		get => GetString(nameof(ProxyHost).ToLowerInvariant());
		set => this[nameof(ProxyHost).ToLowerInvariant()] = value;
	}

	/// <summary>
	/// The password for authenticating to the proxy server. If USEPROXY is true and PROXYUSER is set, you must set this parameter.
	/// </summary>
	/// <value>The proxy password.</value>
	[Category("Proxy")]
	[Description(@"The password for authenticating to the proxy server. If USEPROXY is true and PROXYUSER is set, you must set this parameter.")]
	public string? ProxyPassword
	{
		get => GetString(nameof(ProxyPassword).ToLowerInvariant());
		set => this[nameof(ProxyPassword).ToLowerInvariant()] = value;
	}

	/// <summary>
	/// The port number of the proxy server. If USEPROXY is set to true, you must set this parameter.
	/// </summary>
	/// <value>The proxy port.</value>
	[Category("Proxy")]
	[Description(@"The port number of the proxy server.
If USEPROXY is set to true, you must set this parameter.")]
	public int ProxyPort
	{
		get => GetInt(nameof(ProxyPort).ToLowerInvariant());
		set => this[nameof(ProxyPort).ToLowerInvariant()] = value;
	}

	/// <summary>
	/// The user name for authenticating to the proxy server.
	/// </summary>
	/// <value>The proxy user.</value>
	[Category("Proxy")]
	[Description("The user name for authenticating to the proxy server.")]
	public string? ProxyUser
	{
		get => GetString(nameof(ProxyUser).ToLowerInvariant());
		set => this[nameof(ProxyUser).ToLowerInvariant()] = value;
	}

	/// <summary>
	/// Gets or sets the role.
	/// </summary>
	/// <value>The role.</value>
	[Category("Connection")]
	public string? Role
	{
		get => GetString(nameof(Role).ToLowerInvariant());
		set => this[nameof(Role).ToLowerInvariant()] = value;
	}

	/// <summary>
	/// Gets or sets the schema.
	/// </summary>
	/// <value>The schema.</value>
	[Category("Connection")]
	public string? Schema
	{
		get => GetString(nameof(Schema).ToLowerInvariant());
		set => this[nameof(Schema).ToLowerInvariant()] = value;
	}

	[Category("Auth")]
	[Description("The OAuth token to use for OAuth authentication. Must be used in combination with AUTHENTICATOR=oauth.")]
	public string? Token
	{
		get => GetString(nameof(Token).ToLowerInvariant());
		set => this[nameof(Token).ToLowerInvariant()] = value;
	}

	/// <summary>
	/// Set to true if you need to use a proxy server. The default value is false.
	/// </summary>
	[Category("Proxy")]
	[DefaultValue(false)]
	[Description("Set to true if you need to use a proxy server. The default value is false.")]
	public bool UseProxy
	{
		get => GetBool(nameof(UseProxy).ToLowerInvariant());
		set => this[nameof(UseProxy).ToLowerInvariant()] = value;
	}

	/// <summary>
	/// Username.  If Authenticator is set to 'externalbrowser' or the URL for native SSO through Okta, set this to the login name for your identity provider (IdP).
	/// </summary>
	/// <value>The user.</value>
	[Category("Required")]
	[Description("Username.  If Authenticator is set to 'externalbrowser' or the URL for native SSO through Okta, set this to the login name for your identity provider (IdP).")]
	public string? User
	{
		get => GetString(nameof(User).ToLowerInvariant());
		set => this[nameof(User).ToLowerInvariant()] = value;
	}

	/// <summary>
	/// Whether DB, SCHEMA and WAREHOUSE should be verified when making connection. Default to be true.
	/// </summary>
	[Category("Connection")]
	[DefaultValue(true)]
	[Description("Whether DB, SCHEMA and WAREHOUSE should be verified when making connection. Default to be true.")]
	public bool ValidateDefaultParameters
	{
		get => GetBool("validate_default_parameters");
		set => this["validate_default_parameters"] = value;
	}

	/// <summary>
	/// Gets or sets the warehouse.
	/// </summary>
	/// <value>The warehouse.</value>
	[Category("Connection")]
	public string? Warehouse
	{
		get => GetString(nameof(Warehouse).ToLowerInvariant());
		set => this[nameof(Warehouse).ToLowerInvariant()] = value;
	}

	/// <summary>
	/// Gets or sets the <see cref="System.Object"/> with the specified key.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <returns>System.Object.</returns>
	[AllowNull]
	public override object this[string key]
	{
		get => base[key];
		set
		{
			TryGetValue(key, out var existing);
			if (existing == null)
			{
				if (value == null)
				{
					return;
				}
			}
			else if (existing.Equals(value))
			{
				return;
			}
			base[key] = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(key));
		}
	}

	/// <summary>
	/// Adds an item to the <see cref="ICollection{KeyValuePair{string, object}}" />.
	/// </summary>
	/// <param name="item">The object to add to the <see cref="ICollection{KeyValuePair{string, object}}" />.</param>
	public void Add(KeyValuePair<string, object> item) => Add(item.Key, item.Value);

	/// <summary>
	/// Determines whether the <see cref="ICollection{KeyValuePair{string, object}}" /> contains a specific value.
	/// </summary>
	/// <param name="item">The object to locate in the <see cref="ICollection{KeyValuePair{string, object}}" />.</param>
	/// <returns><see langword="true" /> if <paramref name="item" /> is found in the <see cref="ICollection{KeyValuePair{string, object}}" />; otherwise, <see langword="false" />.</returns>
	public bool Contains(KeyValuePair<string, object> item)
	{
		return base.TryGetValue(item.Key, out var value) && Equals(value, item.Value);
	}

	/// <summary>
	/// Copies the elements of the <see cref="ICollection{KeyValuePair{string, object}}" /> to an array, starting at a particular array index.
	/// </summary>
	/// <param name="array">The one-dimensional array that is the destination of the elements copied from <see cref="ICollection{KeyValuePair{string, object}}" />. The array must have zero-based indexing.</param>
	/// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
	/// <exception cref="System.ArgumentNullException">array</exception>
	public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
	{
		if (array == null)
			throw new ArgumentNullException(nameof(array));

		var i = arrayIndex;
		foreach (string? key in base.Keys) //key is never null. .NET Core 3.1 has a nullability bug.
			array[i] = new(key!, base[key!]);
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator that can be used to iterate through the collection.</returns>
	public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
	{
		foreach (string? key in base.Keys) //key is never null. .NET Core 3.1 has a nullability bug.
			yield return new(key!, base[key!]);
	}

	/// <summary>
	/// Removes the first occurrence of a specific object from the <see cref="ICollection{KeyValuePair{string, object}}" />.
	/// </summary>
	/// <param name="item">The object to remove from the <see cref="ICollection{KeyValuePair{string, object}}" />.</param>
	/// <returns><see langword="true" /> if <paramref name="item" /> was successfully removed from the <see cref="ICollection{KeyValuePair{string, object}}" />; otherwise, <see langword="false" />. This method also returns <see langword="false" /> if <paramref name="item" /> is not found in the original <see cref="ICollection{KeyValuePair{string, object}}" />.</returns>
	public bool Remove(KeyValuePair<string, object> item)
	{
		var found = base.TryGetValue(item.Key, out var value) && Equals(value, item.Value);
		if (found)
			base.Remove(item.Key);
		return found;
	}

	/// <summary>
	/// Gets the bool.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <returns><c>true</c> if the value is found, <c>false</c> otherwise.</returns>
	protected bool GetBool(string key) => TryGetValue(key, out var value) ? Convert.ToBoolean(value, CultureInfo.InvariantCulture) : false;

	/// <summary>
	/// Gets the int.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <returns>System.Int32.</returns>
	protected int GetInt(string key)
	{
		var value = GetString(key);
		if (value != null && int.TryParse(value, out int result))
		{
			return result;
		}
		return 0;
	}

	/// <summary>
	/// Gets the string.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <returns>This will return a null if the value was not found.</returns>
	protected string? GetString(string key) => TryGetValue(key, out var value) ? (string)value : null;
}
