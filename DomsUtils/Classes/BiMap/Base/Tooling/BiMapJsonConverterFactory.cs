using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DomsUtils.Classes.BiMap.Base.Tooling;

public class BiMapJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType
        && typeToConvert.GetGenericTypeDefinition() == typeof(BiMap<,>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type[] typeArgs = typeToConvert.GetGenericArguments();
        Type converterType = typeof(BiMapJsonConverter<,>).MakeGenericType(typeArgs);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}