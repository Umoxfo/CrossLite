using System.Reflection;

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
        /// Indicates whether this attribute Auto Increments (Must be a Key!)
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
        /// Indicates whether this Attribute is forcibly NOT NULL
        /// </summary>
        public bool HasNotNullableAttribute { get; internal set; } = false;

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
