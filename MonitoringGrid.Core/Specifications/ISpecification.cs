using System.Linq.Expressions;

namespace MonitoringGrid.Core.Specifications;

/// <summary>
/// Base specification interface for encapsulating business rules and queries
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Gets the criteria expression for the specification
    /// </summary>
    Expression<Func<T, bool>> Criteria { get; }

    /// <summary>
    /// Gets the include expressions for navigation properties
    /// </summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Gets the include strings for navigation properties
    /// </summary>
    List<string> IncludeStrings { get; }

    /// <summary>
    /// Gets the order by expression
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Gets the order by descending expression
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Gets the group by expression
    /// </summary>
    Expression<Func<T, object>>? GroupBy { get; }

    /// <summary>
    /// Gets the number of items to take (for pagination)
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Gets the number of items to skip (for pagination)
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Indicates whether paging is enabled
    /// </summary>
    bool IsPagingEnabled { get; }

    /// <summary>
    /// Indicates whether the specification is satisfied by the entity
    /// </summary>
    bool IsSatisfiedBy(T entity);
}
