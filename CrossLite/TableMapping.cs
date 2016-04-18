using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace CrossLite
{
    /// <summary>
    /// Represents an Attribute => Entity mapping of a database table
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
        /// Gets a collection of Column to Property mappings
        /// </summary>
        public IReadOnlyDictionary<string, AttributeInfo> Columns { get; protected set; }

        /// <summary>
        /// Gets a collection of Composite Foreign keys on this table
        /// </summary>
        public IReadOnlyCollection<CompositeForeignKeyAttribute> CompositeForeignKeys { get; protected set; }

        /// <summary>
        /// Gets a collection of Unique constraints on this table
        /// </summary>
        public IReadOnlyCollection<CompositeUniqueAttribute> CompositeUnique { get; protected set; }

        /// <summary>
        /// Indicates whether this Table has a single Primary Key
        /// </summary>
        public bool HasPrimaryKey { get; protected set; }

        /// <summary>
        /// Gets a collection of keys on this table
        /// </summary>
        public string[] CompositeKeys
        {
            get
            {
                return (from x in Columns where x.Value.PrimaryKey select x.Key).ToArray();
            }
        }

        /// <summary>
        /// Gets a collection of foreign keys on this table
        /// </summary>
        public AttributeInfo[] ForeignKeys
        {
            get
            {
                return (from x in Columns where x.Value.ForeignKey != null select x.Value).ToArray();
            }
        }

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

            // Get table related instructions
            var tableAttr = (TableAttribute)entityType.GetCustomAttribute(typeof(TableAttribute));
            if (tableAttr != null)
            {
                this.TableName = tableAttr.Name;
                this.WithoutRowID = tableAttr.WithoutRowID;
            }

            // Check for composite foreign Keys and unique composites
            CompositeForeignKeys = entityType.GetCustomAttributes<CompositeForeignKeyAttribute>().ToList().AsReadOnly();    
            CompositeUnique = entityType.GetCustomAttributes<CompositeUniqueAttribute>().ToList().AsReadOnly();

            // Temporary variables
            Dictionary<string, AttributeInfo> cols = new Dictionary<string, AttributeInfo>();
            bool hasAutoIncrement = false;

            // Get a list of properties from the Entity that represents an Attribute
            var props = entityType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(prop => Attribute.IsDefined(prop, typeof(ColumnAttribute)));

            // Loop through each attribute, and generate an attribute map
            foreach (PropertyInfo property in props)
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
                        info.ForeignKey = (ForeignKeyAttribute)attr;
                    }
                    else if (attrType == typeof(PrimaryKeyAttribute))
                    {
                        info.PrimaryKey = true;
                    }
                    else if (attrType == typeof(DefaultAttribute))
                    {
                        info.DefaultValue = ((DefaultAttribute)attr).Value;
                    }
                    else if (attrType == typeof(NotNullAttribute))
                    {
                        info.HasNotNullableAttribute = true;
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

            // Set internals
            Columns = new ReadOnlyDictionary<string, AttributeInfo>(cols);
            HasPrimaryKey = this.CompositeKeys.Length == 1;
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
    }
}
