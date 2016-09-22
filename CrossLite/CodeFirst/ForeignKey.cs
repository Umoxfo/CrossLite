using System;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
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

        protected ForeignKeyConstraint Constraint { get; set; }

        protected TableMapping ChildTable { get; set; }

        protected WhereStatement Statement { get; set; } 

        /// <summary>
        /// Creates a new Instance of <see cref="ForeignKey{TEntity}"/>
        /// </summary>
        /// <param name="childEntity">The Child Entity object WITH the Foreign Key restraint</param>
        /// <param name="childPropertry">The property from the child property, that hosts the foreign key object</param>
        /// <param name="context">An open SQLiteContext that hosts these entities</param>
        public ForeignKey(object childEntity, PropertyInfo childPropertry, SQLiteContext context)
        {
            // Set properties
            ChildEntity = childEntity;
            ConnectionString = context.ConnectionString;

            // Define entity types
            Type childType = ChildEntity.GetType();
            Type parentType = typeof(TEntity);

            // Grab mapping and foreign info from child entity
            ChildTable = EntityCache.GetTableMap(childType);
            Constraint = ChildTable.ForeignKeys.Where(x => x.ChildPropertyName == childPropertry.Name).FirstOrDefault();

            // Make sure the user set their code up correctly
            if (Constraint == null)
            {
                throw new EntityException(
                    $"Entity \"{childType.Name}\" does not contain a ForeignKey attribute for {parentType.Name}"
                );
            }

            // Refresh where statment
            Refresh();
        }

        /// <summary>
        /// Refreshes the internal SQL statement to reflect any property value changes
        /// within the child entity. This method should be called whenever a foreign key
        /// value changes.
        /// </summary>
        public void Refresh()
        {
            // Create new WHERE Statement
            Statement = new WhereStatement();

            // Fill up the WhereStatement with joining keys specific to this Child
            // entities instance
            for (int i = 0; i < Constraint.ForeignKey.Attributes.Length; i++)
            {
                // Grab attribute names
                string childColName = Constraint.ForeignKey.Attributes[i];
                string parentColName = Constraint.InverseKey.Attributes[i];

                // Get the value of the child attribute on this Entity instance
                AttributeInfo info = ChildTable.GetAttribute(childColName);
                object val = info.Property.GetValue(ChildEntity);

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
