using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Text;

namespace CrossLite.QueryBuilder
{
    /// <summary>
    /// Provides an object interface that can properly put together an Update Query string.
    /// </summary>
    /// <remarks>
    /// By using the BuildCommand() method, all parameters in the WHERE statement will be 
    /// escaped by the underlaying SQLiteCommand object, making the Execute() method SQL injection safe.
    /// </remarks>
    class UpdateQueryBuilder
    {
        #region Properties

        /// <summary>
        /// The table name to query
        /// </summary>
        public string Table { get; set; }

        /// <summary>
        /// A list of FieldValuePairs
        /// </summary>
        protected Dictionary<string, FieldValuePair> Fields = new Dictionary<string, FieldValuePair>();

        /// <summary>
        /// The Where statement for this query
        /// </summary>
        public WhereStatement WhereStatement { get; set; } = new WhereStatement();

        /// <summary>
        /// The database driver, if using the "BuildCommand" method
        /// </summary>
        protected SQLiteContext Context;

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Creates a new instance of UpdateQueryBuilder with the provided SQLite connection.
        /// </summary>
        /// <param name="context">The SQLiteContext that will be used to build and query this SQL statement</param>
        public UpdateQueryBuilder(SQLiteContext context)
        {
            this.Context = context;
        }

        /// <summary>
        /// Creates a new instance of UpdateQueryBuilder with the provided SQLite connection.
        /// </summary>
        /// <param name="table">The table name we are updating data in</param>
        /// <param name="context">The SQLiteContext that will be used to build and query this SQL statement</param>
        public UpdateQueryBuilder(string table, SQLiteContext context)
        {
            this.Table = table;
            this.Context = context;
        }

        #endregion Constructors

        #region Fields

        /// <summary>
        /// Sets a value for the specified field
        /// </summary>
        /// <param name="field">The column or field name</param>
        /// <param name="value">The new value to update</param>
        public void SetField(string field, object value)
        {
            this.SetField(field, value, ValueMode.Set);
        }

        /// <summary>
        /// Sets a value for the specified field
        /// </summary>
        /// <param name="field">The column or field name</param>
        /// <param name="value">The new value to update</param>
        /// <param name="mode">Sets how the update value will be applied to the existing field value</param>
        public void SetField(string field, object value, ValueMode mode)
        {
            if (Fields.ContainsKey(field))
                Fields[field] = new FieldValuePair(field, value, mode);
            else
                Fields.Add(field, new FieldValuePair(field, value, mode));
        }

        #endregion Fields

        #region Where's

        public WhereStatement Where(string field, Comparison @operator, object compareValue)
        {
            if (WhereStatement.InnerClauseOperator == LogicOperator.And)
                return WhereStatement.And(field, @operator, compareValue);
            else
                return WhereStatement.Or(field, @operator, compareValue);
        }

        #endregion Where's

        #region Set Methods

        /// <summary>
        /// Sets the table name to update
        /// </summary>
        /// <param name="table">The name of the table to update</param>
        public void SetTable(string table)
        {
            this.Table = table;
        }

        #endregion Set Methods

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
                throw new Exception("Cannot build a command when the Context hasn't been specified. Call SetContext first.");

            // Make sure we have a table name
            if (String.IsNullOrWhiteSpace(Table))
                throw new Exception("Table to update was not set.");

            // Make sure we have at least 1 field to update
            if (Fields.Count == 0)
                throw new Exception("No fields to update");

            // Start Query
            StringBuilder query = new StringBuilder($"UPDATE {SQLiteContext.Escape(Table)} SET ");
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();

            // Add Fields
            bool first = true;
            foreach (KeyValuePair<string, FieldValuePair> field in Fields)
            {
                // Append comma
                if (!first) query.Append(", ");
                else first = false;

                // If using a command, Convert values to Parameters
                if (buildCommand && field.Value.Value != null && field.Value.Value != DBNull.Value && !(field.Value.Value is SqlLiteral))
                {
                    // Create param for value
                    SQLiteParameter param = Context.CreateParameter();
                    param.ParameterName = "@P" + parameters.Count;
                    param.Value = field.Value.Value;

                    // Add Params to command
                    parameters.Add(param);

                    // Append Query
                    if (field.Value.Mode == ValueMode.Set)
                        query.AppendFormat("{0} = {1}", SQLiteContext.Escape(field.Key), param.ParameterName);
                    else
                        query.AppendFormat("{0} = {0} {1} {2}", SQLiteContext.Escape(field.Key), GetSign(field.Value.Mode), param.ParameterName);
                }
                else
                {
                    if (field.Value.Mode == ValueMode.Set)
                        query.AppendFormat("{0} = {1}", SQLiteContext.Escape(field.Key), SqlExpression.FormatSQLValue(field.Value.Value));
                    else
                        query.AppendFormat("{0} = {0} {1} {2}", SQLiteContext.Escape(field.Key), 
                            GetSign(field.Value.Mode), SqlExpression.FormatSQLValue(field.Value.Value));
                }
            }

            // Append Where
            if (WhereStatement.HasClause)
                query.Append(" WHERE " + WhereStatement.BuildStatement(parameters));

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
        /// Executes the command against the database. The database driver must be set!
        /// </summary>
        /// <returns></returns>
        public int Execute()
        {
            using (SQLiteCommand command = BuildCommand())
                return command.ExecuteNonQuery();
        }

        /// <summary>
        /// Returns the sign for the given value mode
        /// </summary>
        /// <param name="Mode"></param>
        /// <returns></returns>
        protected string GetSign(ValueMode Mode)
        {
            string Sign = "";
            switch (Mode)
            {
                case ValueMode.Add:
                    Sign = "+";
                    break;
                case ValueMode.Divide:
                    Sign = "/";
                    break;
                case ValueMode.Multiply:
                    Sign = "*";
                    break;
                case ValueMode.Subtract:
                    Sign = "-";
                    break;
            }
            return Sign;
        }

        /// <summary>
        /// Internal FieldValuePair object
        /// </summary>
        internal struct FieldValuePair
        {
            public string Field;
            public object Value;
            public ValueMode Mode;

            public FieldValuePair(string Field, object Value)
            {
                this.Field = Field;
                this.Value = Value;
                this.Mode = ValueMode.Set;
            }

            public FieldValuePair(string Field, object Value, ValueMode Mode)
            {
                this.Field = Field;
                this.Value = Value;
                this.Mode = Mode;
            }
        }
    }
}
