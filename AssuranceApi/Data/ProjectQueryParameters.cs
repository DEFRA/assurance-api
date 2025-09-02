using System.Globalization;

namespace AssuranceApi.Data
{
    /// <summary>
    /// Represents query parameters for filtering projects.
    /// </summary>
    public class ProjectQueryParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectQueryParameters"/> class.
        /// </summary>
        public ProjectQueryParameters()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectQueryParameters"/> class with specified tags and date range.
        /// </summary>
        /// <param name="tags">A comma-separated list of tags to filter projects.</param>
        /// <param name="startDate">The start date of the date range in ISO 8601 format.</param>
        /// <param name="endDate">The end date of the date range in ISO 8601 format.</param>
        /// <param name="deliveryGroupId">The ID of the delivery group to filter by.</param>
        public ProjectQueryParameters(string? tags, string? startDate, string? endDate, string? deliveryGroupId)
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

            DeliveryGroupId = deliveryGroupId;
        }

        /// <summary>
        /// Gets or sets a comma-separated list of tags to filter projects.
        /// </summary>
        public string? Tags { get; set; }

        /// <summary>
        /// Gets or sets the ID of the delivery group to filter by.
        /// </summary>
        public string? DeliveryGroupId { get; set; }

        /// <summary>
        /// Gets or sets the start date of the date range.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date of the date range.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Parses a date string into a <see cref="DateTime"/> object.
        /// </summary>
        /// <param name="date">The date string to parse.</param>
        /// <returns>A nullable <see cref="DateTime"/> object if parsing is successful; otherwise, null.</returns>
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
