using FluentValidation.Results;

namespace AssuranceApi.Utils
{
    internal static class ValidationHelper
    {
        internal static string GetValidationMessage(string message, List<string> validationErrors)
        {
            var validationMessage = $"{message}:";

            foreach (var validationError in validationErrors)
            {
                validationMessage += $"{Environment.NewLine}  {validationError}";
            }

            return validationMessage;
        }

        internal static string GetValidationMessage(
            string message,
            List<ValidationFailure> validationFailures
        )
        {
            var validationMessage = $"{message}:";

            foreach (var validationFailure in validationFailures)
            {
                validationMessage += $"\n  {validationFailure.ErrorMessage}";
            }

            return validationMessage;
        }
    }
}
