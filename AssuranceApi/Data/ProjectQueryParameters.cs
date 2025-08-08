using System.Globalization;

namespace AssuranceApi.Data
{
    public class ProjectQueryParameters
    {
        public ProjectQueryParameters()
        {

        }

        public ProjectQueryParameters(string? tags, string? startDate, string? endDate)
        {
            Tags = tags;
            StartDate = ParseDateTime(startDate);
            EndDate = ParseDateTime(endDate);

            if (StartDate.HasValue && EndDate.HasValue && StartDate > EndDate)
            {
                var tempDate = EndDate;
                StartDate = EndDate;
                EndDate = tempDate;
            }
        }

        public string? Tags { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        private static DateTime? ParseDateTime(string? date)
        {
            if (string.IsNullOrEmpty(date))
            {
                return null;
            }

            if (DateTime.TryParseExact(
                    date,
                    "yyyy-MM-ddTHH:mm:ssZ",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsedDate))
            {
                return parsedDate;
            }
           
            if (DateTime.TryParse(
                    date,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out parsedDate))
            {
                return parsedDate;
            }

            return null;
        }
    }
}
