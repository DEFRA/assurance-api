namespace AssuranceApi.Data.ChangeHistory

{
    /// <summary>
    /// Represents a change to a piece of data, with an original From value and a new To value.
    /// </summary>
    /// <typeparam name="T">The type of the value being changed.</typeparam>
    public class Change<T>
    {
        /// <summary>
        /// Gets or sets the original value.
        /// </summary>
        public T From { get; set; } = default!;

        /// <summary>
        /// Gets or sets the new value.
        /// </summary>
        public T To { get; set; } = default!;
    }
}
