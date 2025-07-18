using System.Globalization;

namespace AssuranceApi.Utils
{
    internal class DateHelper
    {
        internal static DateTime? ParseUpdateDate(string updateDate)
        {
            if (
                !string.IsNullOrEmpty(updateDate)
                && DateTime.TryParse(
                    updateDate,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsedDate
                )
                && parsedDate <= DateTime.UtcNow
            )
                return parsedDate;
            return null;
        }
    }
}
