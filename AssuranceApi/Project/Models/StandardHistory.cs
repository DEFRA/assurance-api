namespace AssuranceApi.Project.Models;

public class StandardHistory
{
    public string Id { get; set; } = null!;
    public string ProjectId { get; set; } = null!;
    public string StandardId { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public string ChangedBy { get; set; } = null!;
    public StandardChanges Changes { get; set; } = null!;
}

public class StandardChanges
{
    public StatusChange? Status { get; set; }
    public CommentaryChange? Commentary { get; set; }
}

public class StatusChange
{
    public string From { get; set; } = null!;
    public string To { get; set; } = null!;
}

public class CommentaryChange
{
    public string From { get; set; } = null!;
    public string To { get; set; } = null!;
} 