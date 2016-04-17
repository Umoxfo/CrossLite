using System;

namespace CrossLite
{
    /// <summary>
    /// Represents a Composite Foreign Key constraint on the database. Only used
    /// in CodeFirst table creation <see cref="SQLiteContext.CreateTable{TEntity}(bool)"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class CompositeForeignKeyAttribute : Attribute
    {
        /// <summary>
        /// Gets the parent Entity for this constraint
        /// </summary>
        public Type OnEntity { get; internal set; }

        /// <summary>
        /// Gets the parent Entity attribute names
        /// </summary>
        public string[] OnAttributes { get; internal set; }

        /// <summary>
        /// Gets the child Entity attribute names
        /// </summary>
        public string[] FromAttributes { get; internal set; }

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
        /// Creates a multi foreign key restraint on the specified child
        /// attributes, and the specifed parent Entity's attributes
        /// </summary>
        /// <param name="FromAttributes">The child Entity attribute names</param>
        /// <param name="OnEntity">The parent Entity for this constraint</param>
        /// <param name="OnAttributes">The parent Entity attribute names</param>
        public CompositeForeignKeyAttribute(
            string[] FromAttributes, 
            Type OnEntity, 
            string[] OnAttributes,
            ReferentialIntegrity OnUpdate = ReferentialIntegrity.NoAction,
            ReferentialIntegrity OnDelete = ReferentialIntegrity.NoAction)
        {
            this.OnEntity = OnEntity;
            this.OnAttributes = OnAttributes;
            this.FromAttributes = FromAttributes;
            this.OnUpdate = OnUpdate;
            this.OnDelete = OnDelete;
        }
    }
}
