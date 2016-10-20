namespace CrossLite.QueryBuilder
{
    /// <summary>
    /// This class represents a Literal value
    /// to be used in a SQL query. No wrapping quotations
    /// will be wrapped in this value when inserted into the
    /// query string.
    /// </summary>
    /// <remarks>
    /// This object should only be used on value that are pre-checked
    /// for SQL injection strings. Miss use of this class can leave the
    /// database vulnerable to an attack.
    /// </remarks>
    public struct SqlLiteral
    {
        /// <summary>
        /// The Literal value
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Creates a new instance of SqlLiteral with the specified value
        /// </summary>
        /// <param name="Value">The Literal value</param>
        public SqlLiteral(string Value)
        {
            this.Value = Value;
        }
    }
}
