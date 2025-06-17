namespace MonitoringGrid.Core.ValueObjects;

/// <summary>
/// Value object representing an execution context for indicators
/// </summary>
public record ExecutionContext
{
    public static readonly ExecutionContext Manual = new("Manual");
    public static readonly ExecutionContext Scheduled = new("Scheduled");
    public static readonly ExecutionContext Test = new("Test");
    public static readonly ExecutionContext System = new("System");
    public static readonly ExecutionContext Api = new("API");
    public static readonly ExecutionContext Webhook = new("Webhook");

    private static readonly HashSet<string> ValidContexts = new(StringComparer.OrdinalIgnoreCase)
    {
        "Manual", "Scheduled", "Test", "System", "API", "Webhook", "Debug", "Maintenance"
    };

    public string Value { get; }

    public ExecutionContext(string context)
    {
        if (string.IsNullOrWhiteSpace(context))
            throw new ArgumentException("Execution context cannot be null or empty", nameof(context));

        var trimmedContext = context.Trim();

        if (!IsValidContext(trimmedContext))
            throw new ArgumentException($"Invalid execution context: {context}. Valid contexts are: {string.Join(", ", ValidContexts)}", nameof(context));

        Value = trimmedContext;
    }

    public static implicit operator string(ExecutionContext context) => context.Value;

    public static explicit operator ExecutionContext(string context) => new(context);

    public bool IsManual() => Value.Equals("Manual", StringComparison.OrdinalIgnoreCase);

    public bool IsScheduled() => Value.Equals("Scheduled", StringComparison.OrdinalIgnoreCase);

    public bool IsTest() => Value.Equals("Test", StringComparison.OrdinalIgnoreCase);

    public bool IsSystem() => Value.Equals("System", StringComparison.OrdinalIgnoreCase);

    public bool IsApi() => Value.Equals("API", StringComparison.OrdinalIgnoreCase);

    public bool IsAutomated() => IsScheduled() || IsSystem() || IsWebhook();

    public bool IsWebhook() => Value.Equals("Webhook", StringComparison.OrdinalIgnoreCase);

    public bool RequiresUserPermission() => IsManual() || IsApi();

    private static bool IsValidContext(string context)
    {
        return ValidContexts.Contains(context);
    }

    public override string ToString() => Value;
}
