/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tortuga.Data.Snowflake.Core;

// Retrieved from: https://stackoverflow.com/a/18997172
internal class SingleOrArrayConverter<T> : JsonConverter
{
    public override bool CanConvert(Type objecType)
    {
        return objecType == typeof(List<T>);
    }

    public override object? ReadJson(JsonReader reader, Type objecType, object? existingValue,
        JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);
        if (token.Type == JTokenType.Array)
        {
            return token.ToObject<List<T>>();
        }
        return new List<T?> { token.ToObject<T>() };
    }

    public override bool CanWrite
    {
        get { return false; }
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
