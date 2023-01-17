using Checker.Checks;
using Checker.Common.JsonConverters;
using System.Text.Json.Serialization;

namespace Checker.Validations
{
    [JsonConverter(typeof(JsonConverterForValidations<IExternalAppValidation>))]
    public interface IExternalAppValidation : IValidation
    {
        Task<CheckResult> Validate(int? exitCode, string? stdOut, string? stdError);
    }
}

