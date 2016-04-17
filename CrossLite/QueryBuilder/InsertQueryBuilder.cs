using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace CrossLite.QueryBuilder
{
    /// <summary>
    /// Provides an object interface that can properly put together an Insert Query string.
    /// </summary>
    /// <remarks>
    /// All parameters in the WHERE and HAVING statements will be escaped by the underlaying
    /// DbCommand object, making the Execute*() methods SQL injection safe.
    /// </remarks>
    class InsertQueryBuilder
    {
        #region Properties

        /// <summary>
        /// The table name to query
        /// </summary>
        public string Table;

        /// <summary>
        /// A list of FieldValuePairs
        /// </summary>
        protected Dictionary<string, object> Fields = new Dictionary<string, object>();

        /// <summary>
        /// The database driver, if using the "BuildCommand" method
        /// </summary>
        protected SQLiteContext Driver;

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Creates a new instance of InsertQueryBuilder with the provided Database Driver.
        /// </summary>
        /// <param name="Driver">The DatabaseDriver that will be used to query this SQL statement</param>
        public InsertQueryBuilder(SQLiteContext Driver)
        {
            this.Driver = Driver;
        }

        /// <summary>
        /// Creates a new instance of InsertQueryBuilder with the provided Database Driver.
        /// </summary>
        /// <param name="Table">The table we are inserting into</param>
        /// <param name="Driver">The DatabaseDriver that will be used to query this SQL statement</param>
        public InsertQueryBuilder(string Table, SQLiteContext Driver)
        {
            this.Table = Table;
            this.Driver = Driver;
        }

        #endregion Constructors

        /// <summary>
        /// Sets a value for the specified field
        /// </summary>
        /// <param name="Field">The column or field name</param>
        /// <param name="Value">The value to insert</param>
        public void SetField(string Field, object Value)
        {
            if (Fields.ContainsKey(Field))
                Fields[Field] = Value;
            else
                Fields.Add(Field, Value);
        }

        #region Set Methods

        /// <summary>
        /// Sets the table name to inesrt into
        /// </summary>
        /// <param name="Table">The name of the table to insert into</param>
        public void SetTable(string Table)
        {
            this.Table = Table;
        }

        #endregion Set Methods

        #region Query

        /// <summary>
        /// Builds the query string with the current SQL Statement, and returns
        /// the querystring. This method is NOT Sql Injection safe!
        /// </summary>
        /// <returns></returns>
        public string BuildQuery() => BuildQuery(false) as String;

        /// <summary>
        /// Builds the query string with the current SQL Statement, and
        /// returns the DbCommand to be executed. All WHERE paramenters
        /// are propery escaped, making this command SQL Injection safe.
        /// </summary>
        /// <returns></returns>
        public DbCommand BuildCommand() => BuildQuery(true) as DbCommand;

        /// <summary>
        /// Builds the query string or DbCommand
        /// </summary>
        /// <param name="BuildCommand"></param>
        /// <returns></returns>
        protected object BuildQuery(bool BuildCommand)
        {
            // Make sure we have a valid DB driver
            if (BuildCommand && Driver == null)
                throw new Exception("Cannot build a command when the Db Drvier hasn't been specified. Call SetDbDriver first.");

            // Make sure we have a table name
            if (String.IsNullOrWhiteSpace(Table))
                throw new Exception("Table to insert into was not set.");

            // Make sure we have at least 1 field to update
            if (Fields.Count == 0)
                throw new Exception("No fields to insert");

            // Create Command
            DbCommand Command = (BuildCommand) ? Driver.CreateCommand(null) : null;

            // Start Query
            StringBuilder Query = new StringBuilder("INSERT INTO " + Table + " (");
            StringBuilder Values = new StringBuilder();
            bool First = true;

            // Add fields and values
            foreach (KeyValuePair<string, object> Item in Fields)
            {
                // Append comma
                if (!First)
                {
                    Query.Append(", ");
                    Values.Append(", ");
                }
                else 
                    First = false;

                // If using a command, Convert values to Parameters
                if (BuildCommand && Item.Value != null && Item.Value != DBNull.Value && !(Item.Value is SqlLiteral))
                {
                    // Create param for value
                    DbParameter Param = Command.CreateParameter();
                    Param.ParameterName = "@P" + Command.Parameters.Count;
                    Param.Value = Item.Value;

                    // Add Params to command
                    Command.Parameters.Add(Param);

                    // Append query's
                    Query.Append(Item.Key);
                    Values.Append(Param.ParameterName);
                }
                else
                {
                    Query.Append(Item.Key);
                    Values.Append(WhereStatement.FormatSQLValue(Item.Value));
                }
            }

            // Finish the query string, and return the proper object
            Query.Append(") VALUES (" + Values.ToString() + ")");

            // Set the command text
            if (BuildCommand) Command.CommandText = Query.ToString();
            return (BuildCommand) ? Command as object : Query.ToString();
        }

        /// <summary>
        /// Executes the built SQL statement on the Database connection that was passed
        /// in the contructor. All WHERE paramenters are propery escaped, 
        /// making this command SQL Injection safe.
        /// </summary>
        public int Execute()
        {
            using (DbCommand Command = BuildCommand())
                return Command.ExecuteNonQuery();
        }

        #endregion Query
    }
}
