using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Trading
{
    public static class JsonHandler
    {
        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new DateTimeConverter() }
        };


        private class DateTimeConverter : JsonConverter<DateTime>
        {
            public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var dateString = reader.GetString();

                if (DateTime.TryParse(dateString, out var result))
                    return result;

                if (long.TryParse(dateString, out var unixTime))
                    return DateTime.UnixEpoch.AddSeconds(unixTime);

                throw new JsonException($"Unable to convert \"{dateString}\" to DateTime.");
            }

            public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
            }
        }
    }
}
