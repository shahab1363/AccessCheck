using Checker.Checks;
using Checker.Common.JsonConverters;
using System.Text.Json.Serialization;

namespace Checker.Validations
{
    [JsonConverter(typeof(JsonConverterForValidations<ITextValidation>))]
    public interface ITextValidation : IValidation
    {
        Task<CheckResult> Validate(string stringToValidate);
    }
}

