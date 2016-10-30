using System.Linq;
using CrossLite.Collections;

namespace CrossLite.QueryBuilder
{
    /// <summary>
    /// Represents a table identifier in a SELECT query
    /// </summary>
    public class TableIndentifier
    {
        /// <summary>
        /// The name of the table
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The alias of this table
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// A list of all the selected indetifiers
        /// </summary>
        public OrderedDictionary<string, ColumnIdentifier> Columns { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="TableIndentifier"/>
        /// </summary>
        /// <param name="tableName">The table name</param>
        /// <param name="alias">The table alias if any</param>
        public TableIndentifier(string tableName, string alias = null)
        {
            this.Name = tableName;
            this.Alias = alias;
            this.Columns = new OrderedDictionary<string, ColumnIdentifier>();
        }
    }
}
