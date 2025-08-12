namespace AssuranceApi.Data
{
    /// <summary>
    /// Represents the project ratings used for assurance purposes.
    /// </summary>
    public enum ProjectRatings
    {
        /// <summary>
        /// Indicates that the rating is pending.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Alias for the Pending rating.
        /// </summary>
        Tbc = Pending,

        /// <summary>
        /// Indicates a red rating.
        /// </summary>
        Red = 1,

        /// <summary>
        /// Indicates an amber red rating.
        /// </summary>
        Amber_Red = 2,

        /// <summary>
        /// Indicates an amber rating.
        /// </summary>
        Amber = 3,

        /// <summary>
        /// Indicates an amber green rating.
        /// </summary>
        Green_Amber = 4,

        /// <summary>
        /// Indicates a green rating.
        /// </summary>
        Green = 5,

        /// <summary>
        /// Indicates the rating is not applicable.
        /// </summary>
        Excluded = 100
    }
}
