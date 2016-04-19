using System;

namespace CrossLite.CodeFirst
{
    /// <summary>
    /// Represents a Foreign Key constraint on a table. Only used
    /// in CodeFirst table creation <see cref="SQLiteContext.CreateTable{TEntity}(bool)"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ForeignKeyAttribute : Attribute
    {
        /// <summary>
        /// Gets the parent Entity attribute names
        /// </summary>
        public string[] Attributes { get; internal set; }

        /// <summary>
        /// Gets the <see cref="ReferentialIntegrity"/> for this key restraint 
        /// when a row in the parent table is deleted
        /// </summary>
        public ReferentialIntegrity OnDelete { get; set; } = ReferentialIntegrity.NoAction;

        /// <summary>
        /// Gets the <see cref="ReferentialIntegrity"/> for this key restraint 
        /// when a row in the parent table is updated
        /// </summary>
        public ReferentialIntegrity OnUpdate { get; set; } = ReferentialIntegrity.NoAction;

        /// <summary>
        /// Creates a single foreign key restraint between the attached Entity
        /// attribute, and the specifed parent Entity attribute
        /// </summary>
        public ForeignKeyAttribute(string attribute)
        {
            this.Attributes = new string[] { attribute };
        }

        /// <summary>
        /// Creates a foreign key constraint between the 2 attached Entity
        /// attributes, and the specifed parent Entity attributes
        /// </summary>
        public ForeignKeyAttribute(string attribute1, string attribute2)
        {
            this.Attributes = new string[] { attribute1, attribute2 };
        }

        /// <summary>
        /// Creates a foreign key constraint between the 3 attached Entity
        /// attributes, and the specifed parent Entity attributes
        /// </summary>
        public ForeignKeyAttribute(string attribute1, string attribute2, string attribute3)
        {
            this.Attributes = new string[] { attribute1, attribute2 };
        }

        /// <summary>
        /// Creates a foreign key constraint between the attached Entity
        /// attributes, and the specifed parent Entity attributes
        /// </summary>
        public ForeignKeyAttribute(params string[] attributes)
        {
            this.Attributes = attributes;
        }
    }
}
