using CrossLite.CodeFirst;
using CrossLite.QueryBuilder;

namespace CrossLite
{
    public class IndexedColumn
    {
        /// <summary>
        /// Gets or sets the column name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the sorting order
        /// </summary>
        public Sorting SortOrder { get; set; }

        /// <summary>
        /// Gets or sets the collation, if any
        /// </summary>
        public Collation Collate { get; set; }

        public IndexedColumn(string name, Sorting order = Sorting.Ascending, Collation collate = Collation.Default)
        {
            Name = name;
            SortOrder = order;
            Collate = collate;
        }
    }
}