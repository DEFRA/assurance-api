namespace AssuranceApi.Project.Models;

/// <summary>
/// Represents a profession summary within a standard.
/// </summary>
public class StandardSummaryProfessionModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the profession.
    /// </summary>
    public string ProfessionId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the status of the profession.
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the commentary for the profession.
    /// </summary>
    public string Commentary { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date and time when the profession was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Represents a summary of a standard, including aggregated information and associated professions.
/// </summary>
public class StandardSummaryModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the standard.
    /// </summary>
    public string StandardId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the aggregated status of the standard.
    /// </summary>
    public string AggregatedStatus { get; set; } = null!;

    /// <summary>
    /// Gets or sets the aggregated commentary for the standard.
    /// </summary>
    public string AggregatedCommentary { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date and time when the standard was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Gets or sets the list of professions associated with the standard.
    /// </summary>
    public List<StandardSummaryProfessionModel> Professions { get; set; } = new();
}
