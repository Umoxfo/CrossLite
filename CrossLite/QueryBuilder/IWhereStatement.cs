using System.Collections.Generic;
using System.Data.SQLite;

namespace CrossLite.QueryBuilder
{
    /// <summary>
    /// Represents a WHERE statement inside an SQL query
    /// </summary>
    public interface IWhereStatement
    {
        /// <summary>
        /// Gets or Sets the Logic Operator to use in Clauses. The opposite operator
        /// will be used to seperate clauses. The Default is And and should not be changed
        /// unless you know what you are doing!
        /// </summary>
        LogicOperator InnerClauseOperator { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="CrossLite.AttributeQuoteMode"/> this instance will use for queries
        /// </summary>
        AttributeQuoteMode AttributeQuoteMode { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="CrossLite.AttributeQuoteKind"/> this instance will use for queries
        /// </summary>
        AttributeQuoteKind AttributeQuoteKind { get; set; }

        /// <summary>
        /// Indicates whether this WhereStatement has any clauses, or if its empty.
        /// </summary>
        bool HasClause { get; }

        /// <summary>
        /// Builds the current set of Clauses and returns the output as a string.
        /// </summary>
        string BuildStatement();

        /// <summary>
        /// Builds the current set of Clauses and returns the full statement as a string.
        /// </summary>
        /// <param name="parameters">A list that will be filled with the statements parameters</param>
        string BuildStatement(List<SQLiteParameter> parameters);
    }
}