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
    /// A <see cref="DbSet{TEntity}"/> represents a collection
    /// of Entities (Aka: rows) in the SQLite database.
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
            string[] keys = table.CompositeKeys;

            // Generate the SQL
            InsertQueryBuilder builder = new InsertQueryBuilder(table.TableName, Context);
            foreach (var attribute in table.Columns)
            {
                // Grab value
                object value = attribute.Value.Property.GetValue(obj);

                // Check for auto increments!
                if (keys.Contains(attribute.Key))
                {
                    // Skip inserting Auto Increment fields!
                    if (attribute.Value.AutoIncrement == true)
                        continue;

                    // Skip single primary keys that are Integers and do not have a value, 
                    // This will cause the key to perform an Auto Increment
                    if (table.HasPrimaryKey && attribute.Value.Property.PropertyType.IsNumericType() && value == null)
                        continue;
                }

                // Add attribute to the field list
                builder.SetField(attribute.Key, value);
            }

            // Create the SQL Command
            return builder.Execute();
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
            WhereStatement statement = null;
            WhereClause where = null;

            // build the where statement, using primary keys
            foreach (string keyName in table.CompositeKeys)
            {
                PropertyInfo info = table.Columns[keyName].Property;
                if (statement == null)
                {
                    statement = new WhereStatement();
                    where = statement.Add(keyName, Comparison.Equals, info.GetValue(obj));
                }
                else
                    where.AddClause(LogicOperator.And, keyName, Comparison.Equals, info.GetValue(obj));
            }

            // Build the SQL query and perform the deletion
            string sql = $"DELETE FROM `{table.TableName}`";
            using (SQLiteCommand command = Context.CreateCommand(sql))
            {
                // Append the where statement
                if (statement.Count > 0)
                    command.CommandText += $" WHERE {statement.BuildStatement(command)}";

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
            builder.SetWhereOperator(LogicOperator.And);
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
                builder.AddWhere(keyName, Comparison.Equals, info.GetValue(obj));
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
            WhereStatement statement = new WhereStatement();
            WhereClause clause = null;

            // build the where statement, using primary keys
            foreach (string keyName in table.CompositeKeys)
            {
                PropertyInfo info = table.Columns[keyName].Property;
                object val = info.GetValue(obj);

                // Add value to where statement
                if (clause == null)
                    clause = statement.Add(keyName, Comparison.Equals, val);
                else
                    clause.AddClause(LogicOperator.And, keyName, Comparison.Equals, val);
            }

            // Build the SQL query and perform the deletion
            string sql = $"SELECT EXISTS(SELECT 1 FROM `{table.TableName}`";
            using (SQLiteCommand command = Context.CreateCommand(sql))
            {
                // Append the where statement
                command.CommandText += $" WHERE {statement.BuildStatement(command)} LIMIT 1);";
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
