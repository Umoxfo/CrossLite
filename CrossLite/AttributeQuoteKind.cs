namespace CrossLite
{
    /// <summary>
    /// If you want to use a keyword as an attribute name, you need to quote it. 
    /// There are four ways of quoting keywords in SQLite
    /// </summary>
    public enum AttributeQuoteKind
    {
        /// <summary>
        /// Quotes identifiers using the default SQLite quoting mode (Double Quotes).
        /// </summary>
        Default,

        /// <summary>
        /// Quotes identifiers using the single quotes.
        /// </summary>
        SingleQuotes,

        /// <summary>
        /// Quotes identifiers using the square brackets. This is not standard SQL. 
        /// This quoting mechanism is used by MS Access and SQL Server and is included in 
        /// SQLite for compatibility.
        /// </summary>
        SquareBrackets,

        /// <summary>
        /// A keyword enclosed in grave accents (ASCII code 96) is an identifier. 
        /// This is not standard SQL. This quoting mechanism is used by MySQL and is 
        /// included in SQLite for compatibility.
        /// </summary>
        Accents
    }
}
