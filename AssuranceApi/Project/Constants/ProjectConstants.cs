namespace AssuranceApi.Project.Constants;

/// <summary>
/// Provides constants and validation methods for project and service standard statuses.
/// </summary>
public static class ProjectConstants
{
    /// <summary>
    /// Valid project statuses in a 5 RAG system plus TBC.
    /// </summary>
    public static readonly string[] ValidProjectStatuses = new[]
    {
        "RED",
        "AMBER_RED",
        "AMBER",
        "GREEN_AMBER",
        "GREEN",
        "TBC",
    };

    /// <summary>
    /// Valid service standard statuses in a 3 RAG system plus TBC.
    /// </summary>
    public static readonly string[] ValidServiceStandardStatuses = new[]
    {
        "RED",
        "AMBER",
        "GREEN",
        "TBC",
    };

    /// <summary>
    /// Determines whether the specified project status is valid.
    /// </summary>
    /// <param name="status">The project status to validate.</param>
    /// <returns><c>true</c> if the status is valid; otherwise, <c>false</c>.</returns>
    public static bool IsValidProjectStatus(string? status) =>
        !string.IsNullOrEmpty(status) && ValidProjectStatuses.Contains(status);

    /// <summary>
    /// Determines whether the specified service standard status is valid.
    /// </summary>
    /// <param name="status">The service standard status to validate.</param>
    /// <returns><c>true</c> if the status is valid; otherwise, <c>false</c>.</returns>
    public static bool IsValidServiceStandardStatus(string? status) =>
        !string.IsNullOrEmpty(status) && ValidServiceStandardStatuses.Contains(status);
}
