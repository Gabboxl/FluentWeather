using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FluentWeather.Converters
{
    public class CustomDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (string.IsNullOrWhiteSpace(str))
                return default;

            // Try standard parse first
            if (DateTimeOffset.TryParse(str, null, DateTimeStyles.RoundtripKind, out var dto))
                return dto;

            // Try to handle +HHmm (no colon) offset
            if (str.Length > 5 && (str[^5] == '+' || str[^5] == '-'))
            {
                var withColon = str.Substring(0, str.Length - 2) + ":" + str.Substring(str.Length - 2);
                if (DateTimeOffset.TryParse(withColon, null, DateTimeStyles.RoundtripKind, out dto))
                    return dto;
            }

            throw new JsonException($"Invalid DateTimeOffset value: {str}");
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("o"));
        }
    }
}
