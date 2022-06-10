/*
 * Copyright (c) 2012-2021 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;

namespace Tortuga.Data.Snowflake.Core.Messages;

class LoginRequestClientEnv
{
    [JsonProperty(PropertyName = "APPLICATION")]
    internal string? application { get; set; }

    [JsonProperty(PropertyName = "OS_VERSION")]
    internal string? osVersion { get; set; }

    [JsonProperty(PropertyName = "NET_RUNTIME")]
    internal string? netRuntime { get; set; }

    [JsonProperty(PropertyName = "NET_VERSION")]
    internal string? netVersion { get; set; }

    [JsonProperty(PropertyName = "INSECURE_MODE")]
    internal string? insecureMode { get; set; }

    public override string ToString() => $"{{ APPLICATION: {application}, OS_VERSION: {osVersion}, NET_RUNTIME: {netRuntime}, NET_VERSION: {netVersion}, INSECURE_MODE: {insecureMode} }}";
}
