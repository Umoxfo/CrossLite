using System;

namespace CrossLite
{
    /// <summary>
    /// Represents a Foreign Key constraint on the database. Only used
    /// in CodeFirst table creation <see cref="SQLiteContext.CreateTable{TEntity}(bool)"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ForeignKeyAttribute : Attribute
    {
        /// <summary>
        /// Gets the parent Entity for this constraint
        /// </summary>
        public Type OnEntity { get; internal set; }

        /// <summary>
        /// Gets the parent Entity attribute names
        /// </summary>
        public string OnAttribute { get; internal set; }

        /// <summary>
        /// Gets the <see cref="ReferentialIntegrity"/> for this key restraint 
        /// when a row in the parent table is deleted
        /// </summary>
        public ReferentialIntegrity OnDelete { get; protected set; }

        /// <summary>
        /// Gets the <see cref="ReferentialIntegrity"/> for this key restraint 
        /// when a row in the parent table is updated
        /// </summary>
        public ReferentialIntegrity OnUpdate { get; protected set; }

        /// <summary>
        /// Creates a single foreign key restraint between the attached Entity
        /// attribute, and the specifed parent Entity attribute
        /// </summary>
        /// <param name="OnEntity">The parent Entity for this constraint</param>
        /// <param name="OnAttribute">The parent Entity attribute name</param>
        public ForeignKeyAttribute(
            Type OnEntity, 
            string OnAttribute, 
            ReferentialIntegrity OnUpdate = ReferentialIntegrity.NoAction,
            ReferentialIntegrity OnDelete = ReferentialIntegrity.NoAction)
        {
            this.OnEntity = OnEntity;
            this.OnAttribute = OnAttribute;
            this.OnUpdate = OnUpdate;
            this.OnDelete = OnDelete;
        }
    }
}
