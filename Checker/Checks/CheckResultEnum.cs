using System.Text.Json.Serialization;

namespace Checker.Checks
{
    public enum CheckResultEnum
    {
        Success,
        Failure,
        NonConclusive,
        BadConfiguration,
    }
}
