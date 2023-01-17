using System.Text.Json.Serialization;
using System.Text.Json;
using Checker.Validations;

namespace Checker.Common.JsonConverters
{
    public abstract class JsonConverterWithTypeDiscriminator<T> : JsonConverter<T>
    {
        public virtual string TypeDescriminatorProperty => "TypeDiscriminator";
        public virtual string TypeValueProperty => "TypeValue";

        public abstract string GetTypeDescriminatorValue(T toBeSerialized);

        public abstract Type GetTypeFromDescriminator(string? descriminatorValue);

        public override bool CanConvert(Type typeToConvert) =>
            typeof(T).IsAssignableFrom(typeToConvert);

        public override T? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || reader.GetString() != TypeDescriminatorProperty)
            {
                throw new JsonException();
            }

            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException();
            }

            var typeDiscriminator = reader.GetString();

            if (!reader.Read() || reader.GetString() != TypeValueProperty)
            {
                throw new JsonException();
            }
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            var targetType = GetTypeFromDescriminator(typeDiscriminator);

            if (!CanConvert(targetType))
            {
                throw new NotSupportedException();
            }

            var optionsToUse = updateSerializationOptions(options);

            var result = JsonSerializer.Deserialize(ref reader, targetType, optionsToUse);

            if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException();
            }

            return (T?)result;
        }

        public override void Write(
            Utf8JsonWriter writer,
            T value,
            JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            if (value != null)
            {
                var typeDescriminator = GetTypeDescriminatorValue(value);
                writer.WriteString(TypeDescriminatorProperty, typeDescriminator);

                var optionsToUse = updateSerializationOptions(options);

                writer.WritePropertyName(TypeValueProperty);
                JsonSerializer.Serialize(writer, value, GetTypeFromDescriminator(typeDescriminator), optionsToUse);
            }
            writer.WriteEndObject();
        }

        private static JsonSerializerOptions updateSerializationOptions(JsonSerializerOptions options)
        {
            var optionsToUse = new JsonSerializerOptions(options);
            foreach (var converter in options.Converters)
            {
                var converterType = converter.GetType();
                bool shouldRemoveConverter = converterType.IsAssignableTo(typeof(JsonConverterWithTypeDiscriminator<T>));

                if (!shouldRemoveConverter && typeof(T).IsAssignableTo(typeof(IValidation)))
                {
                    if (converterType.IsAssignableTo(typeof(JsonConverterForValidations<T>)) ||
                        (converterType.IsGenericType && converterType.GetGenericTypeDefinition() == typeof(JsonConverterForValidations<>)))
                    {
                        shouldRemoveConverter = true;
                    }
                }

                if (shouldRemoveConverter)
                {
                    optionsToUse.Converters.Remove(converter);
                }
            }

            return optionsToUse;
        }
    }
}
