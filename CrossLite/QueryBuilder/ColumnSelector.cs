using System;
using System.Text;

namespace CrossLite.QueryBuilder
{
    /// <summary>
    /// Represents a column selection in a SELECT query
    /// </summary>
    public sealed class ColumnSelector
    {
        /// <summary>
        /// The name of the attribute or column
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The alias of this attribute or column
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Indicates the aggregate method to perform on this attribute.
        /// </summary>
        public AggregateFunction Aggregate = AggregateFunction.None;

        /// <summary>
        /// If true, then this attribute will be escaped using the quoting settings
        /// defined in the query builder. If false, then this attribute will never be
        /// quoted, even if it is an SQLite keyword.
        /// </summary>
        public bool Escape { get; set; } = true;

        public ColumnSelector(string name, string alias = null, bool escape = true)
        {
            Name = name;
            Alias = alias;
            Escape = escape;
        }

        /// <summary>
        /// Gets the string representation of this column selector.
        /// </summary>
        /// <param name="context">The SQLiteContext used for quoting the attribute</param>
        /// <param name="tablePrefix">The table prefix, if any</param>
        /// <returns></returns>
        public string GetString(SQLiteContext context, string tablePrefix = "")
        {
            StringBuilder builder = new StringBuilder(50);
            AppendToQuery(builder, context, tablePrefix);
            return builder.ToString();
        }

        /// <summary>
        /// Appends the string representation of this column selector to the query builder.
        /// </summary>
        /// <param name="builder">The current query buffer</param>
        /// <param name="context">The SQLiteContext used for quoting the attribute</param>
        /// <param name="tablePrefix">The table prefix, if any</param>
        internal void AppendToQuery(StringBuilder builder, SQLiteContext context, string tablePrefix)
        {
            // Get indentifier name
            string name = (!String.IsNullOrWhiteSpace(tablePrefix)) ? $"{tablePrefix}.{Name}" : Name;

            // Fix for aggregates!
            if (Aggregate != AggregateFunction.None && Name.Equals("*"))
                name = Name;

            // Add the selector to the string
            builder.AppendFormat(GetAggregateString(Aggregate), (Escape) ? context.QuoteAttribute(name) : name);

            // Do we alias?
            if (!String.IsNullOrWhiteSpace(Alias))
                builder.Append($" AS {context.QuoteAttribute(Alias)}");
            else if (Aggregate == AggregateFunction.None)
                builder.Append($" AS {context.QuoteAttribute(Name)}");
        }

        /// <summary>
        /// Returns an unformated string based on the aggregate name
        /// </summary>
        /// <param name="aggregate"></param>
        /// <returns></returns>
        internal static string GetAggregateString(AggregateFunction aggregate)
        {
            switch (aggregate)
            {
                default:
                case AggregateFunction.None: return "{0}";
                case AggregateFunction.Average: return "avg({0})";
                case AggregateFunction.Count: return "count({0})";
                case AggregateFunction.DistinctCount: return "count(distinct {0})";
                case AggregateFunction.Max: return "max({0})";
                case AggregateFunction.Min: return "min({0})";
                case AggregateFunction.Sum: return "sum({0})";
            }
        }
    }
}
