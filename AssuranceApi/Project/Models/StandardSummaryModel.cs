namespace AssuranceApi.Project.Models;

public class StandardSummaryProfessionModel
{
    public string ProfessionId { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string Commentary { get; set; } = null!;
    public DateTime LastUpdated { get; set; }
}

public class StandardSummaryModel
{
    public string StandardId { get; set; } = null!;
    public string AggregatedStatus { get; set; } = null!;
    public string AggregatedCommentary { get; set; } = null!;
    public DateTime LastUpdated { get; set; }
    public List<StandardSummaryProfessionModel> Professions { get; set; } = new();
}
