using System.Text.RegularExpressions;

namespace MonitoringGrid.Core.ValueObjects;

/// <summary>
/// Value object representing a validated SQL query for indicators
/// </summary>
public record SqlQuery
{
    private static readonly Regex DangerousPatterns = new(
        @"\b(DROP|DELETE|INSERT|UPDATE|ALTER|CREATE|TRUNCATE|EXEC|EXECUTE|xp_|sp_)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SelectPattern = new(
        @"^\s*SELECT\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string Value { get; }

    public SqlQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("SQL query cannot be null or empty", nameof(query));

        var trimmedQuery = query.Trim();

        ValidateQuery(trimmedQuery);

        Value = trimmedQuery;
    }

    public static implicit operator string(SqlQuery query) => query.Value;

    public static explicit operator SqlQuery(string query) => new(query);

    public bool IsSelectQuery()
    {
        return SelectPattern.IsMatch(Value);
    }

    public bool ContainsParameters()
    {
        return Value.Contains("@") || Value.Contains("?");
    }

    public List<string> GetTableReferences()
    {
        var tables = new List<string>();
        var fromPattern = new Regex(@"\bFROM\s+(\w+)", RegexOptions.IgnoreCase);
        var joinPattern = new Regex(@"\bJOIN\s+(\w+)", RegexOptions.IgnoreCase);

        var fromMatches = fromPattern.Matches(Value);
        var joinMatches = joinPattern.Matches(Value);

        foreach (Match match in fromMatches)
        {
            if (match.Groups.Count > 1)
                tables.Add(match.Groups[1].Value);
        }

        foreach (Match match in joinMatches)
        {
            if (match.Groups.Count > 1)
                tables.Add(match.Groups[1].Value);
        }

        return tables.Distinct().ToList();
    }

    public int EstimateComplexity()
    {
        var complexity = 0;

        // Base complexity for SELECT
        complexity += 1;

        // Add complexity for JOINs
        complexity += Regex.Matches(Value, @"\bJOIN\b", RegexOptions.IgnoreCase).Count * 2;

        // Add complexity for subqueries
        complexity += Regex.Matches(Value, @"\bSELECT\b", RegexOptions.IgnoreCase).Count - 1;

        // Add complexity for WHERE conditions
        complexity += Regex.Matches(Value, @"\bWHERE\b", RegexOptions.IgnoreCase).Count;

        // Add complexity for GROUP BY
        complexity += Regex.Matches(Value, @"\bGROUP\s+BY\b", RegexOptions.IgnoreCase).Count;

        // Add complexity for ORDER BY
        complexity += Regex.Matches(Value, @"\bORDER\s+BY\b", RegexOptions.IgnoreCase).Count;

        // Add complexity for HAVING
        complexity += Regex.Matches(Value, @"\bHAVING\b", RegexOptions.IgnoreCase).Count;

        return complexity;
    }

    public bool IsHighComplexity()
    {
        return EstimateComplexity() > 10;
    }

    private static void ValidateQuery(string query)
    {
        // Must be a SELECT query
        if (!SelectPattern.IsMatch(query))
            throw new ArgumentException("Only SELECT queries are allowed for indicators", nameof(query));

        // Check for dangerous patterns
        if (DangerousPatterns.IsMatch(query))
            throw new ArgumentException("Query contains potentially dangerous SQL operations", nameof(query));

        // Check length
        if (query.Length > 4000)
            throw new ArgumentException("Query is too long (maximum 4000 characters)", nameof(query));

        // Check for basic SQL injection patterns
        if (query.Contains("--") || query.Contains("/*") || query.Contains("*/"))
            throw new ArgumentException("Query contains potentially unsafe comment patterns", nameof(query));

        // Ensure it doesn't contain multiple statements
        var semicolonCount = query.Count(c => c == ';');
        if (semicolonCount > 1 || (semicolonCount == 1 && !query.TrimEnd().EndsWith(";")))
            throw new ArgumentException("Query cannot contain multiple statements", nameof(query));
    }

    public override string ToString() => Value;
}
