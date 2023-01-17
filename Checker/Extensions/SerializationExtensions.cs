using Checker.Common.JsonConverters;
using Checker.Configuration;
using Checker.Validations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Checker.Extensions
{
    public static class SerializationExtensions
    {
        public static JsonSerializerOptions GetDefaultSerializationOptions(bool indented = false) =>
             new JsonSerializerOptions
             {
                 WriteIndented = indented,
                 DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault,
                 Converters = {
                     new JsonStringEnumConverter()
                 }
             };

        //public static T? Deserialize<T>(this string json, JsonSerializerOptions? options = null)
        //{
        //    options = UpdateSerializationOptionsForConfig(options);
        //    //options = typeof(T).IsAssignableFrom(typeof(CheckerConfiguration))
        //    //    ? UpdateSerializationOptionsForConfig(options)
        //    //    : options;

        //    return JsonSerializer.Deserialize<T>(json, options);
        //}

        //public static string Serialize(this object? value, Type inputType, JsonSerializerOptions? options = null)
        //{
        //    options = UpdateSerializationOptionsForConfig(options);
        //    //options = typeof(T).IsAssignableFrom(typeof(CheckerConfiguration))
        //    //    ? UpdateSerializationOptionsForConfig(options)
        //    //    : options;

        //    return JsonSerializer.Serialize(value, inputType, options);
        //}

        //public static string Serialize(this object? value, JsonSerializerOptions? options = null)
        //{
        //    options = UpdateSerializationOptionsForConfig(options);
        //    //options = typeof(T).IsAssignableFrom(typeof(CheckerConfiguration))
        //    //    ? UpdateSerializationOptionsForConfig(options)
        //    //    : options;

        //    return JsonSerializer.Serialize(value, options);
        //}

        //public static string Serialize<T>(this T value, JsonSerializerOptions? options = null)
        //{
        //    options = UpdateSerializationOptionsForConfig(options);
        //    //options = typeof(T).IsAssignableFrom(typeof(CheckerConfiguration))
        //    //    ? UpdateSerializationOptionsForConfig(options)
        //    //    : options;

        //    return JsonSerializer.Serialize(value, options);
        //}

        //private static JsonConverter[] configConverter = new JsonConverter[]
        //{
        //    //new JsonConverterForICheckConfiguration(),
        //    //new JsonConverterForIReportConfiguration(),
        //    //new JsonConverterForValidations<IHttpValidation>(),
        //    //new JsonConverterForValidations<ITextValidation>(),
        //    //new JsonConverterForValidations<IIPValidation>(),
        //    //new JsonConverterForValidations<IExternalAppValidation>(),
        //    new JsonStringEnumConverter()
        //};

        //private static JsonSerializerOptions UpdateSerializationOptionsForConfig(JsonSerializerOptions? options)
        //{
        //    options = options == null
        //        ? new JsonSerializerOptions()
        //        : new JsonSerializerOptions(options);

        //    foreach (var converter in configConverter)
        //    {
        //        if (options.Converters.Any(c => c.GetType() == converter.GetType()) != true)
        //        {
        //            options.Converters.Add(converter);
        //        }
        //    }

        //    return options;
        //}
    }
}
