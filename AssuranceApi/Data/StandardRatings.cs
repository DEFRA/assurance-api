namespace AssuranceApi.Data
{
    /// <summary>
    /// Represents the standard ratings used for assurance purposes.
    /// </summary>
    public enum StandardRatings
    {
        /// <summary>
        /// Indicates that the rating is pending.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Indicates a red rating.
        /// </summary>
        Red = 1,

        /// <summary>
        /// Indicates an amber rating.
        /// </summary>
        Amber = 2,

        /// <summary>
        /// Indicates a green rating.
        /// </summary>
        Green = 3,
    }
}
