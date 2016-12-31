using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using CrossLite.QueryBuilder;

namespace CrossLite
{
    /// <summary>
    /// A <see cref="DbSet{TEntity}"/> represents the collection
    /// of all Entities (rows of data) in the context that can be 
    /// queried from the database.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class DbSet<TEntity> : ICollection<TEntity>
        where TEntity : class
    {
        /// <summary>
        /// The database context
        /// </summary>
        protected SQLiteContext Context { get; set; }

        /// <summary>
        /// Gets the <see cref="TableMapping"/> for this TEntity type
        /// </summary>
        protected TableMapping EntityTable { get; set; }

        /// <summary>
        /// Gets the record at the selected index
        /// </summary>
        /// <param name="index"></param>
        /// <returns>
        /// Returns a <see cref="TEntity"/> at the specified index within the database, 
        /// or null if the index is out of range
        /// </returns>
        public TEntity this[int index]
        {
            get
            {
                string table = Context.QuoteIdentifier(EntityTable.TableName);
                string query = $"SELECT * FROM {table} LIMIT 1 OFFSET {index}";
                return Context.Query<TEntity>(query).FirstOrDefault();
            }
        }

        /// <summary>
        /// Returns the total number of entities in the database
        /// </summary>
        public int Count
        {
            get
            {
                string table = Context.QuoteIdentifier(EntityTable.TableName);
                string query = $"SELECT COUNT(1) FROM {table}";
                return Context.ExecuteScalar<int>(query);
            }
        }

        /// <summary>
        /// Indicates whether this ICollection is read only
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Creates a new instance of <see cref="DbSet{TEntity}"/>
        /// </summary>
        /// <param name="context">An active SQLite connection</param>
        public DbSet(SQLiteContext context)
        {
            // Since this instance will live as long as the SQLiteContext,
            // we can store the open connection instead of the connection string
            Context = context;

            // Get our Table Mapping for thie TEntity type
            EntityTable = EntityCache.GetTableMap(typeof(TEntity));
        }

        /// <summary>
        /// Inserts a new Entity into the database, and sets any Foreign key properties. If the 
        /// entity table has a single integer primary key, the primary key value will be updated 
        /// with the <see cref="SQLiteConnection.LastInsertRowId"/>.
        /// </summary>
        /// <param name="obj">The <see cref="TEntity"/> object to add to the dataset</param>
        public void Add(TEntity obj)
        {
            // For fetching the RowID
            AttributeInfo rowid = EntityTable.RowIdColumn;

            // Generate the SQL
            InsertQueryBuilder builder = new InsertQueryBuilder(EntityTable.TableName, Context);
            foreach (var attribute in EntityTable.Columns)
            {
                // Grab value
                PropertyInfo property = attribute.Value.Property;
                bool isKey = attribute.Value.PrimaryKey;

                // Check for integer primary keys
                if (isKey && EntityTable.HasRowIdAlias && EntityTable.RowIdColumn == attribute.Value)
                {
                    // A column with type INTEGER PRIMARY KEY is an alias for the 
                    // ROWID (except in WITHOUT ROWID tables). Here is check for
                    // auto assignment
                    long id;
                    var value = property.GetValue(obj);
                    if (long.TryParse(value.ToString(), out id) && id != 0)
                        builder.Set(attribute.Key, value);

                    continue;
                }

                // Add attribute to the field list
                builder.Set(attribute.Key, property.GetValue(obj));
            }

            // Execute the SQL Command
            int result = builder.Execute();

            // If the insert was successful, lets build our Entity relationships
            if (result > 0)
            {
                // If we have a Primary key that is determined database side,
                // than we can update the current object's key value here
                if (EntityTable.HasRowIdAlias)
                {
                    long rowId = Context.Connection.LastInsertRowId;
                    rowid.Property.SetValue(obj, Convert.ChangeType(rowId, rowid.Property.PropertyType));
                }

                // Build relationships after a fresh insert
                EntityTable.CreateRelationships(obj, Context);
            }
        }

        /// <summary>
        /// Inserts a range of new Entities into the database
        /// </summary>
        /// <param name="obj">The <see cref="TEntity"/> objects to add to the dataset</param>
        public void AddRange(params TEntity[] entities)
        {
            foreach (TEntity obj in entities) Add(obj);
        }

        /// <summary>
        /// Inserts a range of new Entities into the database
        /// </summary>
        /// <param name="collection">The <see cref="TEntity"/> objects to add to the dataset</param>
        public void AddRange(IEnumerable<TEntity> collection)
        {
            foreach (TEntity obj in collection) Add(obj);
        }

        /// <summary>
        /// Deletes an Entity from the database
        /// </summary>
        /// <param name="obj">The <see cref="TEntity"/> object to remove from the dataset</param>
        /// <returns>true if an entity was removed from the dataset; false otherwise.</returns>
        public bool Remove(TEntity obj)
        {
            // Start the query using a query builder
            var builder = new DeleteQueryBuilder(Context).From(EntityTable.TableName);

            // build the where statement, using primary keys only
            foreach (string keyName in EntityTable.PrimaryKeys)
            {
                PropertyInfo info = EntityTable.Columns[keyName].Property;
                builder.Where(keyName, Comparison.Equals, info.GetValue(obj));
            }

            // Execute the command
            return builder.Execute() > 0;
        }

        /// <summary>
        /// Deletes a range of new Entities into the database
        /// </summary>
        /// <param name="obj">The <see cref="TEntity"/> objects to remove from the dataset</param>
        public void RemoveRange(params TEntity[] entities)
        {
            foreach (TEntity obj in entities) Remove(obj);
        }

        /// <summary>
        /// Deletes a range of new Entities into the database
        /// </summary>
        /// <param name="collection">The <see cref="TEntity"/> objects to remove from thedataset</param>
        public void RemoveRange(IEnumerable<TEntity> collection)
        {
            foreach (TEntity obj in collection) Remove(obj);
        }

        /// <summary>
        /// Updates an Entity in the database, provided that none of the Primary
        /// keys were modified.
        /// </summary>
        /// <param name="obj">The <see cref="TEntity"/> object to update in the dataset</param>
        /// <returns>true if any records in the database were affected; false otherwise.</returns>
        public bool Update(TEntity obj)
        {
            // Generate the SQL
            UpdateQueryBuilder builder = new UpdateQueryBuilder(EntityTable.TableName, Context);
            foreach (var attribute in EntityTable.Columns)
            {
                // Keys go in the WHERE statement, not the SET statement
                if (EntityTable.PrimaryKeys.Contains(attribute.Key))
                {
                    PropertyInfo info = attribute.Value.Property;
                    builder.Where(attribute.Key, Comparison.Equals, info.GetValue(obj));
                }
                else
                {
                    object value = attribute.Value.Property.GetValue(obj);
                    builder.Set(attribute.Key, value);
                }
            }

            // Create the SQL Command
            return builder.Execute() == 1;
        }

        /// <summary>
        /// If the Entity exists in the database already, than it is updated with
        /// the new values, otherwise the Entity object is inserted into the database
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public void AddOrUpdate(TEntity obj)
        {
            if (Contains(obj))
                Update(obj);
            else
                Add(obj);
        }

        /// <summary>
        /// This method will requery an entity from the database, refreshing
        /// the values of all attributes to match that in the database.
        /// </summary>
        /// <param name="entity">The entity object to reload attributes to</param>
        /// <returns>
        /// true if the entity was successfully retrieved from the databse 
        /// and its attributes reloaded; false otherwise
        /// </returns>
        public bool Reload(ref TEntity entity)
        {
            // Begin a new Select Query
            SelectQueryBuilder query = new SelectQueryBuilder(Context);
            query.From(EntityTable.TableName).SelectAll().Take(1);

            // Grab the primary keys
            foreach (string attrName in EntityTable.PrimaryKeys)
            {
                // Add column expression
                AttributeInfo attribute = EntityTable.GetAttribute(attrName);
                query.Where(attrName, Comparison.Equals, attribute.Property.GetValue(entity));
            }

            // Create command
            using (SQLiteCommand command = query.BuildCommand())
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                // Do we have a result?
                if (reader.HasRows)
                {
                    // Read the row
                    reader.Read();
                    entity = Context.ConvertToEntity<TEntity>(EntityTable, reader);

                    // Close reader and return positive
                    reader.Close();
                    return true;
                }
                else
                {
                    reader.Close();
                    return false;
                }
            }
        }

        /// <summary>
        /// This method is used to refresh the foreign key relationship attributes
        /// on an entity.
        /// </summary>
        /// <param name="entity"></param>
        public void Refresh(TEntity entity) => EntityTable.CreateRelationships(entity, Context);

        /// <summary>
        /// Returns whether an Entity exists in the database, by comparing its 
        /// Primary/Composite Key(s).
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Contains(TEntity obj)
        {
            // Create a WHERE statement
            WhereStatement where = new WhereStatement(Context);

            // build the where statement, using primary keys
            foreach (string keyName in EntityTable.PrimaryKeys)
            {
                PropertyInfo info = EntityTable.Columns[keyName].Property;
                object val = info.GetValue(obj);

                // Add value to where statement
                where.And(keyName, Comparison.Equals, val);
            }

            return Contains(EntityTable.TableName, where);
        }

        internal bool Contains(string tableName, WhereStatement where)
        {
            // Build the SQL query
            List<SQLiteParameter> parameters;
            string sql = String.Format("SELECT EXISTS(SELECT 1 FROM {0} WHERE {1} LIMIT 1);",
                Context.QuoteIdentifier(tableName),
                where.BuildStatement(out parameters)
            );

            // Execute the command
            using (SQLiteCommand command = Context.CreateCommand(sql))
            {
                command.Parameters.AddRange(parameters.ToArray());
                return Context.ExecuteScalar<int>(command) == 1;
            }
        }

        /// <summary>
        /// Deletes all records from the database table.
        /// </summary>
        public void Clear()
        {
            // Build the SQL query
            string table = Context.QuoteIdentifier(EntityTable.TableName);
            string sql = $"DELETE FROM {table}";
            using (SQLiteCommand command = Context.CreateCommand(sql))
                command.ExecuteNonQuery();
        }

        /// <summary>
        /// Copies the entities in this DbSet to an Array, starting at a particular Array index.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional Array that is the destination of the elements copied from ICollection. 
        /// The Array must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">
        /// The zero-based index in array at which copying begins.
        /// </param>
        public void CopyTo(TEntity[] array, int arrayIndex)
        {
            // Ensure we have an array to work with
            if (array == null)
                throw new ArgumentNullException("array");

            int i = arrayIndex;
            foreach (TEntity entity in Context.Select<TEntity>())
            {
                array[i++] = entity;
            }
        }

        /// <summary>
        /// Selects all of the records in the database, and returns the Enumerator
        /// </summary>
        public IEnumerator<TEntity> GetEnumerator() => Context.Select<TEntity>().GetEnumerator();

        /// <summary>
        /// Selects all of the records in the database, and returns the Enumerator
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
