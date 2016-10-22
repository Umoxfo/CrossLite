namespace CrossLite.QueryBuilder
{
    /// <summary>
    /// This object represents a Join Statement for an SQL query
    /// </summary>
    public class JoinClause
    {
        /// <summary>
        /// Gets the <see cref="SelectQueryBuilder"/> this clause is attached to.
        /// </summary>
        public SelectQueryBuilder Query { get; protected set; }

        /// <summary>
        /// The Join Type
        /// </summary>
        public JoinType JoinType { get; protected set; }

        /// <summary>
        /// Gets the <see cref="JoinExpression"/> for this clause
        /// </summary>
        public JoinExpression Expression { get; protected set; }

        /// <summary>
        /// Specifies which table we are joining INTO
        /// </summary>
        public string FromTable { get; set; }

        /// <summary>
        /// Specifies the From Table Comparison Field
        /// </summary>
        public string FromColumn { get; set; }

        /// <summary>
        /// Specifies the Comparison Operator used for the joining of the
        /// two tables
        /// </summary>
        public Comparison ComparisonOperator { get; set; }

        /// <summary>
        /// Specifies the Joining Table Name
        /// </summary>
        public string JoiningTable { get; internal set; }

        /// <summary>
        /// Specifies the Joining Table's Alias
        /// </summary>
        public string JoiningTableAlias { get; set; } = "";

        /// <summary>
        /// Specifies the Joining Table Comparison Field
        /// </summary>
        public string JoiningColumn { get; set; }

        /// <summary>
        /// Creates a new Join Clause
        /// </summary>
        /// <param name="join">Specifies the Type of Join statement this is.</param>
        /// <param name="joiningTable">The Joining Table name</param>
        public JoinClause(SelectQueryBuilder queryBuilder, JoinType join, string joiningTable)
        {
            this.JoinType = join;
            this.JoiningTable = joiningTable;
            this.Expression = new JoinExpression(this);
            this.Query = queryBuilder;
        }

        /// <summary>
        /// Assigns a temporary rename of the joining table
        /// </summary>
        /// <param name="tableAlias"></param>
        /// <returns></returns>
        public JoinClause As(string tableAlias)
        {
            Query.TableAliases[JoiningTable] = tableAlias;
            this.JoiningTableAlias = tableAlias;
            return this;
        }

        /// <summary>
        /// Assigns the attaching column name for this clause
        /// </summary>
        /// <param name="joiningColumn"></param>
        /// <returns></returns>
        public JoinExpression On(string joiningColumn)
        {
            this.JoiningColumn = joiningColumn;
            return this.Expression;
        }
    }
}
