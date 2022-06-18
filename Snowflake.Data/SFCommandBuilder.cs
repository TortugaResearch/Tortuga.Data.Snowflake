/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Tortuga.Data.Snowflake;

public class SFCommandBuilder : DbCommandBuilder
{
	const string QuoteCharacter = "\"";

	/// <summary>
	/// Initializes a new instance of the <see cref="SFCommandBuilder"/> class.
	/// </summary>
	public SFCommandBuilder()
		: this(null)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SFCommandBuilder"/> class.
	/// </summary>
	/// <param name="adapter">The adapter.</param>
	public SFCommandBuilder(SFDataAdapter? adapter)
	{
		DataAdapter = adapter;
		QuotePrefix = QuoteCharacter;
		QuoteSuffix = QuoteCharacter;
	}

	/// <summary>
	/// Gets or sets the beginning character or characters to use when specifying database objects (for example, tables or columns) whose names contain characters such as spaces or reserved tokens.
	/// </summary>
	/// <returns>
	/// The beginning character or characters to use. The default is an empty string.
	///   </returns>
	///   <PermissionSet>
	///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" PathDiscovery="*AllFiles*" />
	///   </PermissionSet>
	[AllowNull]
	public sealed override string QuotePrefix
	{
		get => QuoteCharacter;
		[Obsolete($"The property {nameof(QuoteSuffix)} cannot be changed.", true)]
		set => throw new NotSupportedException($"The property {nameof(QuotePrefix)} cannot be changed.");
	}

	/// <summary>
	/// Gets or sets the ending character or characters to use when specifying database objects (for example, tables or columns) whose names contain characters such as spaces or reserved tokens.
	/// </summary>
	/// <returns>
	/// The ending character or characters to use. The default is an empty string.
	///   </returns>
	///   <PermissionSet>
	///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" PathDiscovery="*AllFiles*" />
	///   </PermissionSet>
	[AllowNull]
	public sealed override string QuoteSuffix
	{
		get => QuoteCharacter;
		[Obsolete($"The property {nameof(QuoteSuffix)} cannot be changed.", true)]
		set => throw new NotSupportedException($"The property {nameof(QuoteSuffix)} cannot be changed.");
	}

	/// <summary>
	/// Applies the parameter information.
	/// </summary>
	/// <param name="parameter">The parameter.</param>
	/// <param name="row">The row.</param>
	/// <param name="statementType">Type of the statement.</param>
	/// <param name="whereClause">if set to <c>true</c> [where clause].</param>
	protected override void ApplyParameterInfo(DbParameter parameter, DataRow row, StatementType statementType, bool whereClause)
	{
		if (parameter == null)
			throw new ArgumentNullException(nameof(parameter), $"{nameof(parameter)} is null.");

		if (row == null)
			throw new ArgumentNullException(nameof(row), $"{nameof(row)} is null.");

		var param = (SFParameter)parameter;
		param.DbType = (DbType)row[SchemaTableColumn.ProviderType];
	}

	/// <summary>
	/// Returns the name of the specified parameter in the format of #.
	/// </summary>
	/// <param name="parameterOrdinal">The number to be included as part of the parameter's name..</param>
	/// <returns>
	/// The name of the parameter with the specified number appended as part of the parameter name.
	/// </returns>
	protected override string GetParameterName(int parameterOrdinal)
	{
		return parameterOrdinal.ToString(CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// Returns the full parameter name, given the partial parameter name.
	/// </summary>
	/// <param name="parameterName">The partial name of the parameter.</param>
	/// <returns>
	/// The full parameter name corresponding to the partial parameter name requested.
	/// </returns>
	protected override string GetParameterName(string parameterName)
	{
		return parameterName;
	}

	/// <summary>
	/// Returns the placeholder for the parameter in the associated SQL statement.
	/// </summary>
	/// <param name="parameterOrdinal">The number to be included as part of the parameter's name.</param>
	/// <returns>
	/// The name of the parameter with the specified number appended.
	/// </returns>
	protected override string GetParameterPlaceholder(int parameterOrdinal)
	{
		return GetParameterName(parameterOrdinal);
	}

	/// <inheritdoc />
	protected override void SetRowUpdatingHandler(DbDataAdapter adapter)
	{
	}
}
