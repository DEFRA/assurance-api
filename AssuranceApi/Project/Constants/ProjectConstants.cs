namespace AssuranceApi.Project.Constants;

public static class ProjectConstants
{
    // Project status validation - 5 RAG system + TBC
    public static readonly string[] ValidProjectStatuses = new[]
    {
        "RED",
        "AMBER_RED",
        "AMBER",
        "GREEN_AMBER",
        "GREEN",
        "TBC",
    };

    // Service standard status validation - 3 RAG system + TBC
    public static readonly string[] ValidServiceStandardStatuses = new[]
    {
        "RED",
        "AMBER",
        "GREEN",
        "TBC",
    };

    public static bool IsValidProjectStatus(string? status) =>
        !string.IsNullOrEmpty(status) && ValidProjectStatuses.Contains(status);

    public static bool IsValidServiceStandardStatus(string? status) =>
        !string.IsNullOrEmpty(status) && ValidServiceStandardStatuses.Contains(status);
} 