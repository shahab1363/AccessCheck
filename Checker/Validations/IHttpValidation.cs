using Checker.Checks;
using Checker.Common.JsonConverters;
using System.Text.Json.Serialization;

namespace Checker.Validations
{
    [JsonConverter(typeof(JsonConverterForValidations<IHttpValidation>))]
    public interface IHttpValidation : IValidation
    {
        Task<CheckResult> Validate(HttpResponseMessage httpResponse, string httpResponseBody);
    }
}

