namespace CrossLite
{
    /// <summary>
    /// Defines the different identifier qouting options for queries
    /// </summary>
    public enum IdentifierQuoteMode
    {
        /// <summary>
        /// Does not escape any attribute names in the query.
        /// </summary>
        None,

        /// <summary>
        /// Escapes only the attribute names that are also SQLite keywords. This setting can
        /// cause some slowdown on large column selections, due to the large amount of reserved words.
        /// </summary>
        KeywordsOnly,

        /// <summary>
        /// Escapes all attribute names to avoid conflicts with keywords in a query.
        /// </summary>
        All
    }
}
