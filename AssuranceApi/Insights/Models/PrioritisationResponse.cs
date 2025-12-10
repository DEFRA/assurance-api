namespace AssuranceApi.Insights.Models;

/// <summary>
/// Response model for the prioritisation endpoint.
/// </summary>
public class PrioritisationResponse
{
    /// <summary>
    /// Gets or sets the list of deliveries that haven't had a service standard update recently.
    /// </summary>
    public List<DeliveryNeedingUpdate> DeliveriesNeedingStandardUpdates { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of deliveries where service standards have worsened.
    /// </summary>
    public List<WorseningStandardsDelivery> DeliveriesWithWorseningStandards { get; set; } = new();
}

/// <summary>
/// Represents a delivery that needs a service standard update.
/// </summary>
public class DeliveryNeedingUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier for the project.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the project.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the overall RAG status of the project.
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the timestamp of the last service standard update.
    /// </summary>
    public DateTime? LastServiceStandardUpdate { get; set; }

    /// <summary>
    /// Gets or sets the number of days since the last service standard update.
    /// </summary>
    public int DaysSinceStandardUpdate { get; set; }
}

/// <summary>
/// Represents a delivery with worsening service standards.
/// </summary>
public class WorseningStandardsDelivery
{
    /// <summary>
    /// Gets or sets the unique identifier for the project.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the project.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the overall RAG status of the project.
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the list of standards that have worsened.
    /// </summary>
    public List<StandardChange> StandardChanges { get; set; } = new();
}

/// <summary>
/// Represents a change in a service standard's status.
/// </summary>
public class StandardChange
{
    /// <summary>
    /// Gets or sets the standard number.
    /// </summary>
    public int StandardNumber { get; set; }

    /// <summary>
    /// Gets or sets the name of the standard.
    /// </summary>
    public string StandardName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the status history (oldest to newest, last 5 entries).
    /// </summary>
    public List<string> StatusHistory { get; set; } = new();
}

