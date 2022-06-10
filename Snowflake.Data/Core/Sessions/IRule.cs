/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Microsoft.Extensions.Primitives;

namespace Tortuga.Data.Snowflake.Core.Sessions;

/// <summary>
/// IRule defines how the queryParams of a uri should be updated in each retry
/// </summary>
interface IRule
{
	void Apply(Dictionary<string, StringValues> queryParams);
}
