using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
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
        protected SQLiteContext Context;

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Creates a new instance of InsertQueryBuilder with the provided Database Driver.
        /// </summary>
        /// <param name="context">The DbContext that will be used to query this SQL statement</param>
        public InsertQueryBuilder(SQLiteContext context)
        {
            this.Context = context;
        }

        /// <summary>
        /// Creates a new instance of InsertQueryBuilder with the provided Database Driver.
        /// </summary>
        /// <param name="table">The table we are inserting into</param>
        /// <param name="context">The DbContext that will be used to query this SQL statement</param>
        public InsertQueryBuilder(string table, SQLiteContext context)
        {
            this.Table = table;
            this.Context = context;
        }

        #endregion Constructors

        /// <summary>
        /// Sets a value for the specified field
        /// </summary>
        /// <param name="field">The column or field name</param>
        /// <param name="value">The value to insert</param>
        public void SetField(string field, object value)
        {
            if (Fields.ContainsKey(field))
                Fields[field] = value;
            else
                Fields.Add(field, value);
        }

        #region Set Methods

        /// <summary>
        /// Sets the table name to inesrt into
        /// </summary>
        /// <param name="table">The name of the table to insert into</param>
        public void SetTable(string table)
        {
            this.Table = table;
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
        public SQLiteCommand BuildCommand() => BuildQuery(true) as SQLiteCommand;

        /// <summary>
        /// Builds the query string or DbCommand
        /// </summary>
        /// <param name="buildCommand"></param>
        /// <returns></returns>
        protected object BuildQuery(bool buildCommand)
        {
            // Make sure we have a valid DB driver
            if (buildCommand && Context == null)
                throw new Exception("Cannot build a command when the Db Drvier hasn't been specified. Call SetContext first.");

            // Make sure we have a table name
            if (String.IsNullOrWhiteSpace(Table))
                throw new Exception("Table to insert into was not set.");

            // Make sure we have at least 1 field to update
            if (Fields.Count == 0)
                throw new Exception("No fields to insert");

            // Start Query
            StringBuilder query = new StringBuilder($"INSERT INTO {SQLiteContext.Escape(Table)} (");
            StringBuilder values = new StringBuilder();
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();
            bool first = true;

            // Add fields and values
            foreach (KeyValuePair<string, object> Item in Fields)
            {
                // Append comma
                if (!first)
                {
                    query.Append(", ");
                    values.Append(", ");
                }
                else 
                    first = false;

                // If using a command, Convert values to Parameters
                if (buildCommand && Item.Value != null && Item.Value != DBNull.Value && !(Item.Value is SqlLiteral))
                {
                    // Create param for value
                    SQLiteParameter Param = Context.CreateParam();
                    Param.ParameterName = "@P" + parameters.Count;
                    Param.Value = Item.Value;

                    // Add Params to command
                    parameters.Add(Param);

                    // Append query's
                    query.Append(SQLiteContext.Escape(Item.Key));
                    values.Append(Param.ParameterName);
                }
                else
                {
                    query.Append(Item.Key);
                    values.Append(WhereStatement.FormatSQLValue(Item.Value));
                }
            }

            // Finish the query string, and return the proper object
            query.AppendFormat(") VALUES ({0})", values);

            // Create Command
            SQLiteCommand command = null;
            if (buildCommand)
            {
                command = Context.CreateCommand(query.ToString());
                command.Parameters.AddRange(parameters.ToArray());
            }

            // Return Result
            return (buildCommand) ? command as object : query.ToString();
        }

        /// <summary>
        /// Executes the built SQL statement on the Database connection that was passed
        /// in the contructor. All WHERE paramenters are propery escaped, 
        /// making this command SQL Injection safe.
        /// </summary>
        public int Execute()
        {
            using (SQLiteCommand command = BuildCommand())
                return command.ExecuteNonQuery();
        }

        #endregion Query
    }
}
