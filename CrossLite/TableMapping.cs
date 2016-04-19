using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using CrossLite.CodeFirst;

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
        public IReadOnlyCollection<ForeignKeyInfo> ForeignKeys { get; protected set; }

        /// <summary>
        /// Gets a collection of Unique constraints on this table
        /// </summary>
        public IReadOnlyCollection<CompositeUniqueAttribute> UniqueConstraints { get; protected set; }

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
        /// 
        /// </summary>
        internal Dictionary<Type, PropertyInfo> Parents { get; set; }

        /// <summary>
        /// 
        /// </summary>
        internal Dictionary<Type, PropertyInfo> Childs { get; set; }

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
            this.Parents = new Dictionary<Type, PropertyInfo>();
            this.Childs = new Dictionary<Type, PropertyInfo>();

            // Get table related instructions
            var tableAttr = (TableAttribute)entityType.GetCustomAttribute(typeof(TableAttribute));
            if (tableAttr != null)
            {
                this.TableName = tableAttr.Name;
                this.WithoutRowID = tableAttr.WithoutRowID;
            }

            // Temporary variables
            Dictionary<string, AttributeInfo> cols = new Dictionary<string, AttributeInfo>();
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
                else if (property.GetGetMethod().IsVirtual && type.IsGenericType)
                {
                    Type def = type.GetGenericTypeDefinition();
                    type = type.GenericTypeArguments[0];
                    if (def == typeof(IEnumerable<>))
                    {
                        Childs.Add(type, property);
                    }
                    else if (def == typeof(ForeignKey<>))
                    {
                        Parents.Add(type, property);
                    }
                }
            }

            // Check for unique composites  
            UniqueConstraints = entityType.GetCustomAttributes<CompositeUniqueAttribute>().ToList().AsReadOnly();

            // Set internals
            Columns = new ReadOnlyDictionary<string, AttributeInfo>(cols);
            HasPrimaryKey = this.CompositeKeys.Length == 1;

            // ------------------------------------
            // Always perform this last!
            // ------------------------------------

            // Get a list of properties that are foreign keys
            List<ForeignKeyInfo> fks = new List<ForeignKeyInfo>();
            props = entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(prop => Attribute.IsDefined(prop, typeof(ForeignKeyAttribute))).ToArray();

            // Loop through each attribute, and generate an attribute map
            foreach (PropertyInfo property in props)
            {
                var fkey = (ForeignKeyAttribute)property.GetCustomAttribute(typeof(ForeignKeyAttribute));
                var inverse = (InverseKeyAttribute)property.GetCustomAttribute(typeof(InverseKeyAttribute));

                if (property.PropertyType.BaseType != typeof(ForeignKey<>).BaseType)
                    continue;

                // Create ForeignKeyInfo
                ForeignKeyInfo info = new ForeignKeyInfo(this, 
                    property.PropertyType.GetGenericArguments()[0], 
                    fkey, 
                    inverse ?? new InverseKeyAttribute(fkey.Attributes)
                );

                fks.Add(info);
            }

            ForeignKeys = fks.AsReadOnly();
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
        /// 
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="entity"></param>
        /// <param name="context"></param>
        internal void CreateRelationships(Type entityType, object entity, SQLiteContext context)
        {
            if (entityType != EntityType)
                throw new ArgumentException("Invalid Entity type passed", "entityType");

            foreach (var parent in this.Parents)
            {
                Type fkT = typeof(ForeignKey<>).MakeGenericType(parent.Key);
                var fk = Activator.CreateInstance(fkT, entity, context);
                parent.Value.SetValue(entity, fk);
            }

            foreach (var parent in this.Childs)
            {
                Type ckT = typeof(ChildDbSet<,>).MakeGenericType(entityType, parent.Key);
                var ck = Activator.CreateInstance(ckT, entity, context);
                parent.Value.SetValue(entity, ck);
            }
        }
    }
}
