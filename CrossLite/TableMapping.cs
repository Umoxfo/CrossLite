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
        /// Gets a collection of keys on this table
        /// </summary>
        public IReadOnlyCollection<string> PrimaryKeys { get; protected set; }

        /// <summary>
        /// Get or sets the RowId column
        /// </summary>
        public AttributeInfo RowIdColumn { get; protected set; }

        /// <summary>
        /// Indicates whether this Table has a single Integer Primary Key
        /// </summary>
        public bool HasRowIdAlias => (!WithoutRowID && RowIdColumn != null);

        /// <summary>
        /// Gets or Sets whether the "WITHOUT ROWID" command is used
        /// when creating a table using Code First 
        /// (see <see cref="SQLiteContext.CreateTable{TEntity}(bool)"/>)
        /// </summary>
        public bool WithoutRowID { get; protected set; } = false;

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
        /// child (Many) to a parent Entity (One). [Property => Generic Type]
        /// </summary>
        /// <remarks>
        /// Contains both Lazy loaded properties and Eager loaded properties.
        /// </remarks>
        internal Dictionary<PropertyInfo, Type> ParentRelationships { get; set; }

        /// <summary>
        /// Contains a list of Foreign key relationships, where this Entity is a
        /// parent Entity (one) to many child Entities (many). 
        /// [Property => Child Generic Type]
        /// </summary>
        internal Dictionary<PropertyInfo, Type> ChildRelationships { get; set; }

        /// <summary>
        /// Gets the name of the Auto Increment attribute, or NULL
        /// </summary>
        public AttributeInfo AutoIncrementAttribute { get; internal set; }

        /// <summary>
        /// Creates a new instance of <see cref="TableMapping"/>
        /// </summary>
        /// <param name="entityType"></param>
        public TableMapping(Type entityType)
        {
            // Set critical properties
            this.EntityType = entityType;
            this.TableName = entityType.Name;
            this.ParentRelationships = new Dictionary<PropertyInfo, Type>();
            this.ChildRelationships = new Dictionary<PropertyInfo, Type>();

            // Get table related instructions
            var tableAttr = (TableAttribute)entityType.GetCustomAttribute(typeof(TableAttribute));
            if (tableAttr != null)
            {
                this.TableName = tableAttr.Name ?? entityType.Name;
                this.WithoutRowID = tableAttr.WithoutRowID;
            }

            // Temporary variables
            Dictionary<string, AttributeInfo> cols = new Dictionary<string, AttributeInfo>();
            List<AttributeInfo> primaryKeys = new List<AttributeInfo>();

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
                            // Check for RowID Alias column (INTEGER PRIMARY KEY)
                            if (info.Property.PropertyType.IsInteger())
                            {
                                if (primaryKeys.Count == 0)
                                    RowIdColumn = info;
                                else
                                    RowIdColumn = null;
                            }

                            // Add primary key to the list
                            info.PrimaryKey = true;
                            primaryKeys.Add(info);
                        }
                        else if (attrType == typeof(DefaultAttribute))
                        {
                            info.DefaultValue = (DefaultAttribute)attr;
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
                            // Cannot have more than 1 auto increment column
                            if (AutoIncrementAttribute != null)
                                throw new EntityException($"Entity `{EntityType.Name}` cannot contain multiple AutoIncrement attributes.");

                            // set values
                            AutoIncrementAttribute = info;
                            info.AutoIncrement = true;
                        }
                    }

                    // Add to column list
                    cols[info.Name] = info;
                }
                // Check for foreign key collections
                else if (property.GetGetMethod(true).IsVirtual)
                {
                    // All generics are Lazy-Loaded
                    if (type.IsGenericType)
                    {
                        Type def = type.GetGenericTypeDefinition();
                        type = type.GenericTypeArguments[0];
                        if (def == typeof(IEnumerable<>))
                        {
                            // IEnumerable means this is a parent entity
                            ChildRelationships.Add(property, type);
                        }
                        else if (def == typeof(ForeignKey<>))
                        {
                            // If no foreign key attribute is defined, tell the dev
                            if (!Attribute.IsDefined(property, typeof(ForeignKeyAttribute)))
                                throw new EntityException("Properties of type ForeignKey<T> must contain the ForeignKey attribute");

                            // ForeignKey<T> means this is a child entity
                            ParentRelationships.Add(property, type);
                        }
                    }
                    else if (Attribute.IsDefined(property, typeof(ForeignKeyAttribute)))
                    {
                        // Eager Loaded Property!
                        ParentRelationships.Add(property, type);
                    }
                }
            }

            // Check for unique composites  
            UniqueConstraints = entityType.GetCustomAttributes<CompositeUniqueAttribute>().ToList().AsReadOnly();

            // Set internals
            Columns = new ReadOnlyDictionary<string, AttributeInfo>(cols);
            PrimaryKeys = primaryKeys.Select(x => x.Name).ToList().AsReadOnly();
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
            foreach (PropertyInfo property in ParentRelationships.Keys)
            {
                var fkey = (ForeignKeyAttribute)property.GetCustomAttribute(typeof(ForeignKeyAttribute));
                var inverse = (InverseKeyAttribute)property.GetCustomAttribute(typeof(InverseKeyAttribute));

                // Grab generic type
                Type parentType = (property.PropertyType.IsGenericType)
                    ? property.PropertyType.GetGenericArguments()[0]
                    : property.PropertyType;

                // Create ForeignKeyInfo
                InverseKeyAttribute inv = inverse ?? new InverseKeyAttribute(fkey.Attributes);
                ForeignKeyConstraint info = new ForeignKeyConstraint(this, property.Name, parentType, fkey, inv);  
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
        /// <param name="entity">The entity we are populating the attributes on</param>
        /// <param name="context">An open SQLite connection where this Entity can be stored/fetched from</param>
        internal void CreateRelationships(object entity, SQLiteContext context)
        {
            // Fill Child ForeignKey<T> properties (Contains Parent Entities)
            foreach (var parent in this.ParentRelationships)
            {
                Type fkT = typeof(ForeignKey<>).MakeGenericType(parent.Value);
                dynamic fk = Activator.CreateInstance(fkT, entity, parent.Key, context);
                if (parent.Key.PropertyType.IsGenericType)
                    parent.Key.SetValue(entity, fk);
                else
                    parent.Key.SetValue(entity, fk.Fetch());
            }

            // Fill Parent Entity IEnumerable<T> properties (Contains Child Entities)
            foreach (var child in this.ChildRelationships)
            {
                Type ckT = typeof(ChildDbSet<,>).MakeGenericType(EntityType, child.Value);
                var ck = Activator.CreateInstance(ckT, entity, context);
                child.Key.SetValue(entity, ck);
            }
        }
    }
}
