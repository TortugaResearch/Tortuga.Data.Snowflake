/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core.Sessions;

class SFSessionPropertyAttr : Attribute
{
	public bool required { get; set; }

	public string defaultValue { get; set; }
}
