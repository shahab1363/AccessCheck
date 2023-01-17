using Checker.Validations;

namespace Checker.Common.JsonConverters
{
    public class JsonConverterForValidations<T> : JsonConverterWithTypeDiscriminator<T>
    {
        public override string TypeDescriminatorProperty => "ValidationType";
        public override string TypeValueProperty => "ValidationConfiguration";
        public override string GetTypeDescriminatorValue(T toBeSerialized)
        {
            if (toBeSerialized is MustNotContain)
            {
                return nameof(MustNotContain);
            }
            if (toBeSerialized is MustContain)
            {
                return nameof(MustContain);
            }
            if (toBeSerialized is ExpectStatusCodes)
            {
                return nameof(ExpectStatusCodes);
            }
            if (toBeSerialized is ExpectContentLength)
            {
                return nameof(ExpectContentLength);
            }
            if (toBeSerialized is ExpectExitCode)
            {
                return nameof(ExpectExitCode);
            }

            throw new NotSupportedException();
        }

        public override Type GetTypeFromDescriminator(string? descriminatorValue)
        {
            if (descriminatorValue == nameof(MustNotContain))
            {
                return typeof(MustNotContain);
            }
            if (descriminatorValue == nameof(MustContain))
            {
                return typeof(MustContain);
            }
            if (descriminatorValue == nameof(ExpectStatusCodes))
            {
                return typeof(ExpectStatusCodes);
            }
            if (descriminatorValue == nameof(ExpectContentLength))
            {
                return typeof(ExpectContentLength);
            }
            if (descriminatorValue == nameof(ExpectExitCode))
            {
                return typeof(ExpectExitCode);
            }

            throw new NotSupportedException();
        }
    }
}
