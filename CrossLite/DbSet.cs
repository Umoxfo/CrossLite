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
    public class DbSet<TEntity> : IEnumerable<TEntity>
        where TEntity : class
    {
        /// <summary>
        /// The database context
        /// </summary>
        protected SQLiteContext Context { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="DbSet{TEntity}"/>
        /// </summary>
        /// <param name="context"></param>
        public DbSet(SQLiteContext context)
        {
            Context = context;
        }

        /// <summary>
        /// Inserts a new Entity into the database
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>The number of rows affected by this operation</returns>
        public int Add(TEntity obj)
        {
            // Get our Table Mapping
            Type objType = typeof(TEntity);
            TableMapping table = EntityCache.GetTableMap(objType);
            string[] keys = table.CompositeKeys; // Linq query; Run enumerator once

            // For fetching the RowID
            bool useRowId = false;
            AttributeInfo pk = null;

            // Generate the SQL
            InsertQueryBuilder builder = new InsertQueryBuilder(table.TableName, Context);
            foreach (var attribute in table.Columns)
            {
                // Grab value
                PropertyInfo info = attribute.Value.Property;
                object value = info.GetValue(obj);

                // Check for auto increments!
                if (keys.Contains(attribute.Key))
                {
                    // Skip single primary keys that are Integers and do not have a value, 
                    // This will cause the key to perform an Auto Increment
                    if (attribute.Value.AutoIncrement || (table.HasPrimaryKey && info.PropertyType.IsNumericType()))
                    {
                        useRowId = true;
                        pk = attribute.Value;
                        continue;
                    }
                }

                // Add attribute to the field list
                builder.SetField(attribute.Key, value);
            }

            // Execute the SQL Command
            int result = builder.Execute();

            // If we have a Primary key that is determined database side,
            // than we can update the current object's key value here
            if (result > 0 && useRowId)
            {
                long rowId = Context.Connection.LastInsertRowId;
                pk.Property.SetValue(obj, Convert.ChangeType(rowId, pk.Property.PropertyType));

                // Build relationships
                table.CreateRelationships(objType, obj, Context);
            }

            // Finally, return the result
            return result;
        }

        /// <summary>
        /// Deletes an Entity from the database
        /// </summary>
        /// <param name="obj">The entity to remove from the database</param>
        /// <returns>The number of rows affected by this operation</returns>
        public int Remove(TEntity obj)
        {
            // Get our Table Mapping
            Type objType = typeof(TEntity);
            TableMapping table = EntityCache.GetTableMap(objType);
            WhereStatement statement = new WhereStatement();

            // build the where statement, using primary keys
            foreach (string keyName in table.CompositeKeys)
            {
                PropertyInfo info = table.Columns[keyName].Property;
                statement.And(keyName, Comparison.Equals, info.GetValue(obj));
            }

            // Build the SQL query
            List<SQLiteParameter> parameters;
            string sql = String.Format("DELETE FROM {0} WHERE {1}",
                SQLiteContext.Escape(table.TableName),
                statement.BuildStatement(Context, out parameters)
            );

            // Execute the command
            using (SQLiteCommand command = Context.CreateCommand(sql))
            {
                command.Parameters.AddRange(parameters.ToArray());
                return command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Updates an Entity in the database, provided none of the Primary
        /// keys were modified.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>The number of rows affected by this operation</returns>
        public int Update(TEntity obj)
        {
            // Get our Table Mapping
            Type objType = typeof(TEntity);
            TableMapping table = EntityCache.GetTableMap(objType);

            // Generate the SQL
            UpdateQueryBuilder builder = new UpdateQueryBuilder(table.TableName, Context);
            foreach (var attribute in table.Columns)
            {
                // Check for keys
                if (table.CompositeKeys.Contains(attribute.Key))
                    continue;

                object value = attribute.Value.Property.GetValue(obj);
                builder.SetField(attribute.Key, value);
            }

            // build the where statement, using primary keys
            foreach (string keyName in table.CompositeKeys)
            {
                PropertyInfo info = table.Columns[keyName].Property;
                builder.Where(keyName, Comparison.Equals, info.GetValue(obj));
            }

            // Create the SQL Command
            return builder.Execute();
        }

        /// <summary>
        /// If the Entity exists in the database already, than it is updated with
        /// the new values, otherwise the Entity object is inserted into the database
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int AddOrUpdate(TEntity obj)
        {
            if (Contains(obj))
                return Update(obj);
            else
                return Add(obj);
        }

        /// <summary>
        /// Returns whether an Entity exists in the database, by comparing its 
        /// Primary/Composite Key(s).
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Contains(TEntity obj)
        {
            // Get our Table Mapping
            Type objType = typeof(TEntity);
            TableMapping table = EntityCache.GetTableMap(objType);
            WhereStatement stmt = new WhereStatement();

            // build the where statement, using primary keys
            foreach (string keyName in table.CompositeKeys)
            {
                PropertyInfo info = table.Columns[keyName].Property;
                object val = info.GetValue(obj);

                // Add value to where statement
                stmt.And(keyName, Comparison.Equals, val);
            }

            // Build the SQL query
            List<SQLiteParameter> parameters;
            string sql = String.Format("SELECT EXISTS(SELECT 1 FROM {0} WHERE {1} LIMIT 1);",
                SQLiteContext.Escape(table.TableName),
                stmt.BuildStatement(Context, out parameters)
            );

            // Execute the command
            using (SQLiteCommand command = Context.CreateCommand(sql))
            {
                command.Parameters.AddRange(parameters.ToArray());
                return Context.ExecuteScalar<int>(command) == 1;
            }
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            return Context.Select<TEntity>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Context.Select<TEntity>().GetEnumerator();
        }
    }
}
