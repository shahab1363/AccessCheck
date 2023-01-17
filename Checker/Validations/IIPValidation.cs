using Checker.Checks;
using Checker.Common.JsonConverters;
using System.Net;
using System.Text.Json.Serialization;

namespace Checker.Validations
{
    [JsonConverter(typeof(JsonConverterForValidations<IIPValidation>))]
    public interface IIPValidation : IValidation
    {
        Task<CheckResult> Validate(IPHostEntry ipHostEntry);
    }
}

