namespace CrossLite.QueryBuilder
{
    /// <summary>
    /// This class represents a Literal value to be used in an SQL query. 
    /// The string value will NOT be wrapped in quotations when inserted 
    /// into the query string.
    /// </summary>
    /// <remarks>
    /// This object should only be used on value that are pre-checked
    /// for SQL injection strings. Miss use of this class can leave the
    /// database vulnerable to an attack.
    /// </remarks>
    public struct SqlLiteral
    {
        /// <summary>
        /// The Literal value to be appended to the query string
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="SqlLiteral"/> with the specified value
        /// </summary>
        /// <param name="Value">The Literal value to be appended to the query string</param>
        public SqlLiteral(string Value)
        {
            this.Value = Value;
        }

        public override string ToString() => Value;
    }
}
