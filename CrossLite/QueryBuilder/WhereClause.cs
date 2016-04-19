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

        /// <summary>
        /// Adds a new Expresion to this Clause
        /// </summary>
        /// <param name="fieldName">The attribute name we are performing the evaluation on</param>
        /// <param name="operator">The Comparison we are performing on this attribute</param>
        /// <param name="value">The value at which we require in this evaluation</param>
        public void Add(string fieldName, Comparison @operator, object value)
        {
            Expressions.Add(new SqlExpression(fieldName, @operator, value));
        }

        /// <summary>
        /// Adds a new Expresion to this Clause
        /// </summary>
        /// <param name="expression"></param>
        public void Add(SqlExpression expression)
        {
            Expressions.Add(expression);
        }
    }
}
