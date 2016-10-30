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
        /// The Parent Entity instance that the Child Entities are bound to
        /// </summary>
        protected TParentEntity Entity { get; set; }

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

        /// <summary>
        /// Lazy loads the child entities of a foreign key constraint
        /// </summary>
        public IEnumerator<TChildEntity> GetEnumerator()
        {
            // Grab table mappings
            TableMapping parentTable = EntityCache.GetTableMap(typeof(TParentEntity));
            TableMapping childTable = EntityCache.GetTableMap(typeof(TChildEntity));

            // Create the SQL Command
            using (SQLiteContext context = new SQLiteContext(ConnectionString))
            {
                // Open the connection
                context.Connect();
                
                // Begin a new Select Query
                SelectQueryBuilder query = new SelectQueryBuilder(context);
                query.From(childTable.TableName).SelectAll();

                // Grab the foreign key constraints
                var fkinfos = childTable.ForeignKeys.Where(x => x.ParentEntityType == parentTable.EntityType);
                foreach (ForeignKeyConstraint fkinfo in fkinfos)
                {
                    // Append each key => value to the query
                    for (int i = 0; i < fkinfo.ForeignKey.Attributes.Length; i++)
                    {
                        string attrName = fkinfo.ForeignKey.Attributes[i];
                        string parentColName = fkinfo.InverseKey.Attributes[i];

                        // Add column expression
                        AttributeInfo attribute = parentTable.GetAttribute(parentColName);
                        query.Where(attrName, Comparison.Equals, attribute.Property.GetValue(Entity));
                    }

                    // Create a new clause, to seperate by an OR
                    query.WhereStatement.CreateNewClause();
                }

                // Create command
                using (SQLiteCommand command = query.BuildCommand())
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    // If we have rows, return each row
                    while (reader.Read())
                        yield return context.ConvertToEntity<TChildEntity>(childTable, reader);

                    // Cleanup
                    reader.Close();
                }
            }
        }

        /// <summary>
        /// Lazy loads the child entities of a foreign key constraint
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
