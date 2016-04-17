namespace CrossLite.QueryBuilder
{
    /// <summary>
    /// This object represents a Join Statement for an SQL query
    /// </summary>
    public class JoinClause
    {
        /// <summary>
        /// The Join Type
        /// </summary>
        public JoinType JoinType { get; protected set; }

        /// <summary>
        /// Specifies which table we are joining INTO
        /// </summary>
        public string FromTable { get; protected set; }

        /// <summary>
        /// Specifies the From Table Comparison Field
        /// </summary>
        public string FromColumn { get; protected set; }

        /// <summary>
        /// Specifies the Comparison Operator used for the joining of the
        /// two tables
        /// </summary>
        public Comparison ComparisonOperator { get; protected set; }

        /// <summary>
        /// Specifies the Joining Table Name
        /// </summary>
        public string JoiningTable { get; protected set; }

        /// <summary>
        /// Specifies the Joining Table Comparison Field
        /// </summary>
        public string JoiningColumn { get; protected set; }

        /// <summary>
        /// Creates a new Join Clause
        /// </summary>
        /// <param name="join">Specifies the Type of Join statement this is.</param>
        /// <param name="toTableName">The Joining Table name</param>
        /// <param name="toColumnName">The Joining Table Comparison Field</param>
        /// <param name="operator">the Comparison Operator used for the joining of thetwo tables</param>
        /// <param name="fromTableName">The table name we are joining INTO</param>
        /// <param name="fromColumnName">The From Table Comparison Field</param>
        public JoinClause(JoinType join, string toTableName, string toColumnName, Comparison @operator, string fromTableName, string fromColumnName)
        {
            this.JoinType = join;
            this.FromTable = fromTableName;
            this.FromColumn = fromColumnName;
            this.ComparisonOperator = @operator;
            this.JoiningTable = toTableName;
            this.JoiningColumn = toColumnName;
        }
    }
}
