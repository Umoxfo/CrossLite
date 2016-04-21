using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using CrossLite.CodeFirst;

namespace CrossLite
{
    /// <summary>
    /// Represents an Attribute to Entity mapping of a database table
    /// </summary>
    public class TableMapping
    {
        /// <summary>
        /// Gets the Entity object type for this table
        /// </summary>
        public Type EntityType { get; protected set; }

        /// <summary>
        /// Gets the table name this Entity represents
        /// </summary>
        public string TableName { get; protected set; }

        /// <summary>
        /// Gets or Sets whether the "WITHOUT ROWID" command is used
        /// when creating a table using Code First 
        /// (see <see cref="SQLiteContext.CreateTable{TEntity}(bool)"/>)
        /// </summary>
        public bool WithoutRowID { get; protected set; } = false;

        /// <summary>
        /// Indicates whether this Table has a single Primary Key
        /// </summary>
        public bool HasPrimaryKey { get; protected set; }

        /// <summary>
        /// Gets a collection of keys on this table
        /// </summary>
        public IReadOnlyCollection<string> PrimaryKeys { get; protected set; }

        /// <summary>
        /// Gets a collection of Column to Property mappings
        /// </summary>
        public IReadOnlyDictionary<string, AttributeInfo> Columns { get; protected set; }

        /// <summary>
        /// Gets a collection of Foreign keys on this table, where this Entity is a
        /// child (Many) to a parent Entity (One)
        /// </summary>
        public IReadOnlyCollection<ForeignKeyConstraint> ForeignKeys { get; protected set; }

        /// <summary>
        /// Gets a collection of Unique constraints on this table
        /// </summary>
        public IReadOnlyCollection<CompositeUniqueAttribute> UniqueConstraints { get; protected set; }

        /// <summary>
        /// Contains a list of Foreign key relationships, where this Entity is a
        /// child (Many) to a parent Entity (One)
        /// </summary>
        internal Dictionary<Type, PropertyInfo> ParentRelationships { get; set; }

        /// <summary>
        /// Contains a list of Foreign key relationships, where this Entity is a
        /// parent Entity (one) to many child Entities (many)
        /// </summary>
        internal Dictionary<Type, PropertyInfo> ChildRelationships { get; set; }

        /// <summary>
        /// Gets the name of the Auto Increment attribute, or NULL
        /// </summary>
        public string AutoIncrementAttribute
        {
            get
            {
                return (from x in Columns where x.Value.AutoIncrement select x.Key).FirstOrDefault();
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="TableMapping"/>
        /// </summary>
        /// <param name="entityType"></param>
        public TableMapping(Type entityType)
        {
            // Set critical properties
            this.EntityType = entityType;
            this.TableName = entityType.Name;
            this.ParentRelationships = new Dictionary<Type, PropertyInfo>();
            this.ChildRelationships = new Dictionary<Type, PropertyInfo>();

            // Get table related instructions
            var tableAttr = (TableAttribute)entityType.GetCustomAttribute(typeof(TableAttribute));
            if (tableAttr != null)
            {
                this.TableName = tableAttr.Name;
                this.WithoutRowID = tableAttr.WithoutRowID;
            }

            // Temporary variables
            Dictionary<string, AttributeInfo> cols = new Dictionary<string, AttributeInfo>();
            List<AttributeInfo> primaryKeys = new List<AttributeInfo>();
            bool hasAutoIncrement = false;

            // Get a list of properties from the Entity that represents an Attribute
            var props = entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            // Loop through each attribute, and generate an attribute map
            foreach (PropertyInfo property in props)
            {
                // Grab type
                Type type = property.PropertyType;

                // Column attribute?
                if (Attribute.IsDefined(property, typeof(ColumnAttribute)))
                {
                    // Create our attribute info class
                    AttributeInfo info = new AttributeInfo();
                    info.Property = property;

                    // Now itterate through each attribute
                    foreach (Attribute attr in property.GetCustomAttributes())
                    {
                        Type attrType = attr.GetType();
                        if (attrType == typeof(ColumnAttribute))
                        {
                            // Get our attribute name
                            ColumnAttribute colAttr = (ColumnAttribute)attr;
                            info.Name = colAttr.Name ?? property.Name;
                        }
                        else if (attrType == typeof(ForeignKeyAttribute))
                        {
                            throw new EntityException($"Invalid foreign key attribute on {entityType.Name}.{property.Name}");
                        }
                        else if (attrType == typeof(PrimaryKeyAttribute))
                        {
                            info.PrimaryKey = true;
                            primaryKeys.Add(info);
                        }
                        else if (attrType == typeof(DefaultAttribute))
                        {
                            info.DefaultValue = ((DefaultAttribute)attr).Value;
                        }
                        else if (attrType == typeof(RequiredAttribute))
                        {
                            info.HasRequiredAttribute = true;
                        }
                        else if (attrType == typeof(UniqueAttribute))
                        {
                            info.Unique = true;
                        }
                        else if (attrType == typeof(CollationAttribute))
                        {
                            info.Collation = ((CollationAttribute)attr).Collation;
                        }
                        else if (attrType == typeof(AutoIncrementAttribute))
                        {
                            // Cannot have more than 1
                            if (hasAutoIncrement)
                                throw new EntityException($"Entity `{EntityType.Name}` cannot contain multiple AutoIncrement attributes.");

                            // set value
                            info.AutoIncrement = true;
                            hasAutoIncrement = true;
                        }
                    }

                    // Add to column list
                    cols[info.Name] = info;
                }
                // Check for foreign key collections
                else if (type.IsGenericType && property.GetGetMethod().IsVirtual)
                {
                    Type def = type.GetGenericTypeDefinition();
                    type = type.GenericTypeArguments[0];
                    if (def == typeof(IEnumerable<>))
                    {
                        // IEnumerable means this is a parent entity
                        ChildRelationships.Add(type, property);
                    }
                    else if (def == typeof(ForeignKey<>))
                    {
                        // If no foreign key attribute is defined, tell the dev
                        if (!Attribute.IsDefined(property, typeof(ForeignKeyAttribute)))
                            throw new EntityException("Properties of type ForeignKey<T> must contain the ForeignKey attribute");

                        // ForeignKey<T> means this is a child entity
                        ParentRelationships.Add(type, property);
                    }
                }
            }

            // Check for unique composites  
            UniqueConstraints = entityType.GetCustomAttributes<CompositeUniqueAttribute>().ToList().AsReadOnly();

            // Set internals
            Columns = new ReadOnlyDictionary<string, AttributeInfo>(cols);
            PrimaryKeys = primaryKeys.Select(x => x.Name).ToList().AsReadOnly();
            HasPrimaryKey = primaryKeys.Count == 1;
            List<ForeignKeyConstraint> foreignKeys = new List<ForeignKeyConstraint>();

            // ------------------------------------
            // Always check foreign keys after setting the Columns property!
            // 
            // We must always check foreign keys after loading all of the entities properties,
            // because the properties may not be ordered correctly in the class itself. This
            // would cause errors when checking for column matches between the parent
            // and child entities when creating the ForeignKeyInfo class.
            // ------------------------------------

            // Loop through each attribute, and generate an attribute map
            foreach (PropertyInfo property in ParentRelationships.Values)
            {
                var fkey = (ForeignKeyAttribute)property.GetCustomAttribute(typeof(ForeignKeyAttribute));
                var inverse = (InverseKeyAttribute)property.GetCustomAttribute(typeof(InverseKeyAttribute));

                // Create ForeignKeyInfo
                ForeignKeyConstraint info = new ForeignKeyConstraint(this, 
                    property.PropertyType.GetGenericArguments()[0], 
                    fkey, 
                    inverse ?? new InverseKeyAttribute(fkey.Attributes)
                );

                foreignKeys.Add(info);
            }

            // Finally, set our class ForeignKey property
            ForeignKeys = foreignKeys.AsReadOnly();
        }

        /// <summary>
        /// Fetches the <see cref="AttributeInfo"/> for the specified attribute name
        /// </summary>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        public AttributeInfo GetAttribute(string attributeName)
        {
            if (!Columns.ContainsKey(attributeName))
                throw new Exception("Entity type \"" + EntityType.Name + "\" does not contain a definition for \"" + attributeName + "\"");

            return Columns[attributeName];
        }

        /// <summary>
        /// Populates the foreign key related properties on an Entity
        /// </summary>
        /// <param name="entityType">
        /// Not really needed, but since this information is available when this 
        /// method is usually called, might as well save CPU cycles.
        /// </param>
        /// <param name="entity">The entity we are populating attributes on</param>
        /// <param name="context">An open SQLite connection where this Entity can be stored/fetched from</param>
        internal void CreateRelationships(Type entityType, object entity, SQLiteContext context)
        {
            // We must have a type match
            if (entityType != EntityType)
                throw new ArgumentException("Invalid Entity type passed", "entityType");

            // Fill ForeignKey<T> properties
            foreach (var parent in this.ParentRelationships)
            {
                Type fkT = typeof(ForeignKey<>).MakeGenericType(parent.Key);
                var fk = Activator.CreateInstance(fkT, entity, context);
                parent.Value.SetValue(entity, fk);
            }

            // Fill IEnumerable<T> properties
            foreach (var parent in this.ChildRelationships)
            {
                Type ckT = typeof(ChildDbSet<,>).MakeGenericType(entityType, parent.Key);
                var ck = Activator.CreateInstance(ckT, entity, context);
                parent.Value.SetValue(entity, ck);
            }
        }
    }
}
