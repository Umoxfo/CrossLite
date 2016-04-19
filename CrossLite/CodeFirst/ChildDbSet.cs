using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using CrossLite.QueryBuilder;

namespace CrossLite.CodeFirst
{
    /// <summary>
    /// This object is used to Lazy load Child Entities that are
    /// bound to the Parent Entity via a Foreign Key relationship.
    /// </summary>
    /// <typeparam name="TParentEntity"></typeparam>
    /// <typeparam name="TChildEntity"></typeparam>
    public class ChildDbSet<TParentEntity, TChildEntity> : IEnumerable<TChildEntity>
        where TParentEntity : class
        where TChildEntity : class
    {
        /// <summary>
        /// The SQLite connection string from this Entity
        /// </summary>
        protected string ConnectionString { get; set; }

        /// <summary>
        /// The Parent Entity that the Child Entities are bound to
        /// </summary>
        protected TParentEntity Entity { get; set; }

        /// <summary>
        /// The cached SELECT statement, if it has been run once already.
        /// </summary>
        protected SelectQueryBuilder SelectQuery { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="ChildDbSet{TParentEntity, TChildEntity}"/>
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="context"></param>
        public ChildDbSet(TParentEntity entity, SQLiteContext context)
        {
            Entity = entity;
            ConnectionString = context.ConnectionString;
        }

        public IEnumerator<TChildEntity> GetEnumerator()
        {
            // Get our Table Mapping
            Type objType = typeof(TChildEntity);
            TableMapping table = EntityCache.GetTableMap(objType);

            // Create the SQL Command
            using (SQLiteContext context = new SQLiteContext(ConnectionString))
            {
                // Open the connection
                context.Connect();

                // --------------------------------------
                // We can cache this query since Keys will never change
                // --------------------------------------
                if (SelectQuery == null)
                {
                    // Begin a new Select Query
                    SelectQuery = new SelectQueryBuilder(context);
                    SelectQuery.SelectFromTable(table.TableName);
                    SelectQuery.SelectColumns(table.Columns.Select(x => SQLiteContext.Escape(x.Key)));

                    // Define types
                    Type childType = typeof(TChildEntity);
                    Type parentType = typeof(TParentEntity);

                    // Grab mapping from parent
                    TableMapping childTable = EntityCache.GetTableMap(childType);
                    TableMapping parentTable = EntityCache.GetTableMap(parentType);

                    // Grab the foreign key info
                    ForeignKeyInfo fkinfo = childTable.ForeignKeys
                        .Where(x => x.ParentEntityType == parentType)
                        .FirstOrDefault();

                    // Append each key => value to the query
                    for (int i = 0; i < fkinfo.ForeignKey.Attributes.Length; i++)
                    {
                        string attrName = fkinfo.ForeignKey.Attributes[i];
                        string parentColName = fkinfo.InverseKey.Attributes[i];

                        AttributeInfo info = parentTable.GetAttribute(parentColName);
                        object val = info.Property.GetValue(Entity);

                        // Add column expression
                        SelectQuery.Where(attrName, Comparison.Equals, val);
                    }
                }

                // Create command
                using (SQLiteCommand command = SelectQuery.BuildCommand())
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    // If we have rows, add them to the list
                    if (reader.HasRows)
                    {
                        // Return each row
                        while (reader.Read())
                            yield return context.ConvertToEntity<TChildEntity>(table, reader);
                    }

                    // Cleanup
                    reader.Close();
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
