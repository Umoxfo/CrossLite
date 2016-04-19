using System;
using System.Data.SQLite;
using System.Linq;
using CrossLite.QueryBuilder;

namespace CrossLite.CodeFirst
{
    /// <summary>
    /// This object is used to Lazy load Parent Entity objects on 
    /// Foreign Key relationships from the SQLite database
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class ForeignKey<TEntity> where TEntity : class
    {
        protected object ChildEntity { get; set; }

        protected string ConnectionString { get; set; }

        protected WhereStatement Statement { get; set; } = new WhereStatement();

        /// <summary>
        /// Creates a new Instance of <see cref="ForeignKey{TEntity}"/>
        /// </summary>
        /// <param name="entity">The Child Entity object</param>
        /// <param name="context">An open SQLiteContext</param>
        public ForeignKey(object entity, SQLiteContext context)
        {
            // Set properties
            ChildEntity = entity;
            ConnectionString = context.ConnectionString;

            // Define entity types
            Type childType = entity.GetType();
            Type parentType = typeof(TEntity);

            // Grab mapping and foreign info from child entity
            TableMapping childTable = EntityCache.GetTableMap(childType);
            ForeignKeyInfo fkinfo = childTable.ForeignKeys.Where(x => x.ParentEntityType == parentType).FirstOrDefault();

            // Make sure the user set their code up correctly
            if (fkinfo == null)
            {
                throw new EntityException(
                    $"Entity \"{childType.Name}\" does not contain a ForeignKey attribute for {parentType.Name}"
                );
            }

            // Fill up the WhereStatement with joining keys specific to this Child
            // entities instance
            for (int i = 0; i < fkinfo.ForeignKey.Attributes.Length; i++)
            {
                // Grab attribute names
                string childColName = fkinfo.ForeignKey.Attributes[i];
                string parentColName = fkinfo.InverseKey.Attributes[i];

                // Get the value of the child attribute on this Entity instance
                AttributeInfo info = childTable.GetAttribute(childColName);
                object val = info.Property.GetValue(entity);

                // Add the key => value to the where statement
                Statement.And(parentColName, Comparison.Equals, val);
            }
        }

        /// <summary>
        /// Gets the current Parent Entity value from the database,
        /// that this Child Entity instance is bound to.
        /// </summary>
        /// <returns></returns>
        public TEntity Fetch()
        {
            // Get our Table Mapping
            Type objType = typeof(TEntity);
            TableMapping table = EntityCache.GetTableMap(objType);

            // Connect to the SQLite database, and prepare the query
            using (SQLiteContext context = new SQLiteContext(ConnectionString))
            {
                // Open the connection
                context.Connect();

                // Build the SQL query
                SelectQueryBuilder builder = new SelectQueryBuilder(context);
                builder.From(table.TableName).Select(table.Columns.Keys);
                builder.WhereStatement = Statement;

                // Execute the Data Reader
                using (SQLiteCommand command = builder.BuildCommand())
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    // If we have rows, add them to the list
                    if (reader.HasRows)
                    {
                        // Return each row
                        reader.Read();
                        return context.ConvertToEntity<TEntity>(table, reader);
                    }
                    else
                        return null;
                }
            }
        }
    }
}
