/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

namespace Tortuga.Data.Snowflake.Core;

public class BindingDTO
{
    public BindingDTO(string type, object? value)
    {
        this.type = type;
        this.value = value;
    }

    public string type { get; set; }

    public object? value { get; set; }
}
