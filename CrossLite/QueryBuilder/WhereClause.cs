using System.Collections.Generic;

namespace CrossLite.QueryBuilder
{
    /// <summary>
    /// Represents a group of Expressions to be used inside a WHERE statement
    /// </summary>
    public class WhereClause
    {
        /// <summary>
        /// Gets a list of Expressions to be evaluated in the Where statement
        /// </summary>
        public List<SqlExpression> Expressions { get; protected set; }

        /// <summary>
        /// Creates a new instance of <see cref="WhereClause"/>
        /// </summary>
        public WhereClause()
        {
            Expressions = new List<SqlExpression>();
        }
    }
}
