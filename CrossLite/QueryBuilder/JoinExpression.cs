namespace CrossLite.QueryBuilder
{
    public class JoinExpression
    {
        /// <summary>
        /// Gets the <see cref="JoinClause"/> this expression is bound to.
        /// </summary>
        public JoinClause Clause { get; protected set; }

        public JoinExpression(JoinClause clause)
        {
            Clause = clause;
        }

        /// <summary>
        /// Internal bits... sets the clause values
        /// </summary>
        /// <param name="operator"></param>
        /// <param name="table"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        private SelectQueryBuilder Set(Comparison @operator, string table, string column)
        {
            Clause.ComparisonOperator = @operator;
            Clause.FromTable = table;
            Clause.FromColumn = column;
            return Clause?.Query;
        }

        /// <summary>
        /// Specifies the comparison of this expression with an Equal operator
        /// </summary>
        public SelectQueryBuilder Equals(string table, string column) 
            => Set(Comparison.Equals, table, column);

        /// <summary>
        /// Specifies the comparison of this expression with an Equal operator
        /// </summary>
        public SelectQueryBuilder NotEqualTo(string table, string column)
            => Set(Comparison.NotEqualTo, table, column);

        /// <summary>
        /// Specifies the comparison of this expression with a Greater than or Equal to operator
        /// </summary>
        public SelectQueryBuilder GreaterOrEquals(string table, string column)
            => Set(Comparison.GreaterOrEquals, table, column);

        /// <summary>
        /// Specifies the comparison of this expression with a Greater than operator
        /// </summary>
        public SelectQueryBuilder GreaterThan(string table, string column)
            => Set(Comparison.GreaterThan, table, column);

        /// <summary>
        /// Specifies the comparison of this expression with a Less than or Equal to operator
        /// </summary>
        public SelectQueryBuilder LessOrEquals(string table, string column)
            => Set(Comparison.LessOrEquals, table, column);

        /// <summary>
        /// Specifies the comparison of this expression with a Less than operator
        /// </summary>
        public SelectQueryBuilder LessThan(string table, string column)
            => Set(Comparison.LessThan, table, column);
    }
}
