using System.Reflection;
using CrossLite.CodeFirst;

namespace CrossLite
{
    public class AttributeInfo
    {
        /// <summary>
        /// Gets the attribute (column) name in the database
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Indicates whether this attribute is a Key
        /// </summary>
        public bool PrimaryKey { get; internal set; } = false;

        /// <summary>
        /// Indicates whether this attribute Auto Increments (Must be a Key!).
        /// AUTOINCREMENT is to prevent the reuse of ROWIDs from previously deleted rows.
        /// </summary>
        public bool AutoIncrement { get; internal set; } = false;

        /// <summary>
        /// Indicates whether this attribute value is unique
        /// </summary>
        public bool Unique { get; internal set; } = false;

        /// <summary>
        /// Gets the default value for this attribute
        /// </summary>
        public object DefaultValue { get; internal set; } = null;

        /// <summary>
        /// Indicates whether this Attribute Requires a value and
        /// cannot be NULL during Entity insertion into the database
        /// </summary>
        public bool HasRequiredAttribute { get; internal set; } = false;

        /// <summary>
        /// Gets the COLLATE type definition that is used to define alternative 
        /// collating functions for a column.
        /// </summary>
        public Collation Collation { get; internal set; } = Collation.Default;

        /// <summary>
        /// Gets the Property for this attribute
        /// </summary>
        public PropertyInfo Property { get; internal set; }

        /// <summary>
        /// If this attribute is a foreign key, then that information is stored here
        /// </summary>
        public ForeignKeyAttribute ForeignKey { get; internal set; }
    }
}
