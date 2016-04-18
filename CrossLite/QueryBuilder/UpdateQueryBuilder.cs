using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace CrossLite.QueryBuilder
{
    /// <summary>
    /// Provides an object interface that can properly put together an Update Query string.
    /// </summary>
    /// <remarks>
    /// All parameters in the WHERE statement will be escaped by the underlaying
    /// DbCommand object, making the Execute*() methods SQL injection safe.
    /// </remarks>
    class UpdateQueryBuilder
    {
        #region Properties

        /// <summary>
        /// The table name to query
        /// </summary>
        public string Table;

        /// <summary>
        /// A list of FieldValuePairs
        /// </summary>
        protected Dictionary<string, FieldValuePair> Fields = new Dictionary<string, FieldValuePair>();

        /// <summary>
        /// Query's where statement
        /// </summary>
        protected WhereStatement WhereStatement = new WhereStatement();

        /// <summary>
        /// The database driver, if using the "BuildCommand" method
        /// </summary>
        protected SQLiteContext Context;

        /// <summary>
        /// Column name escape delimiter
        /// </summary>
        internal static char EscapeChar { get; set; } = '`';

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Creates a new instance of UpdateQueryBuilder with the provided Database Driver.
        /// </summary>
        /// <param name="context">The DbContext that will be used to query this SQL statement</param>
        public UpdateQueryBuilder(SQLiteContext context)
        {
            this.Context = context;
        }

        /// <summary>
        /// Creates a new instance of UpdateQueryBuilder with the provided Database Driver.
        /// </summary>
        /// <param name="table">The table we are updating</param>
        /// <param name="context">The DbContext that will be used to query this SQL statement</param>
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

        public WhereClause AddWhere(string field, Comparison @operator, object compareValue)
        {
            WhereClause Clause = new WhereClause(field, @operator, compareValue);
            this.WhereStatement.Add(Clause);
            return Clause;
        }

        public void AddWhere(WhereClause clause)
        {
            this.WhereStatement.Add(clause);
        }

        /// <summary>
        /// Sets the Logic Operator for the WHERE statement
        /// </summary>
        /// <param name="Operator"></param>
        public void SetWhereOperator(LogicOperator @Operator)
        {
            this.WhereStatement.StatementOperator = @Operator;
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

        /// <summary>
        /// Sets the database context for this query
        /// </summary>
        /// <param name="context"></param>
        public void SetContext(SQLiteContext context)
        {
            this.Context = context;
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
        public DbCommand BuildCommand() => BuildQuery(true) as DbCommand;

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

            // Create Command
            DbCommand command = (buildCommand) ? Context.CreateCommand(null) : null;

            // Start Query
            StringBuilder query = new StringBuilder($"UPDATE {SQLiteContext.Escape(Table)} SET ");

            // Add Fields
            bool First = true;
            foreach (KeyValuePair<string, FieldValuePair> Pair in Fields)
            {
                // Append comma
                if (!First) query.Append(", ");
                else First = false;

                // If using a command, Convert values to Parameters
                if (buildCommand && Pair.Value.Value != null && Pair.Value.Value != DBNull.Value && !(Pair.Value.Value is SqlLiteral))
                {
                    // Create param for value
                    DbParameter Param = command.CreateParameter();
                    Param.ParameterName = "@P" + command.Parameters.Count;
                    Param.Value = Pair.Value.Value;

                    // Add Params to command
                    command.Parameters.Add(Param);

                    // Append Query
                    if (Pair.Value.Mode == ValueMode.Set)
                        query.AppendFormat("{0} = {1}", SQLiteContext.Escape(Pair.Key), Param.ParameterName);
                    else
                        query.AppendFormat("{0} = {0} {1} {2}", SQLiteContext.Escape(Pair.Key), GetSign(Pair.Value.Mode), Param.ParameterName);
                }
                else
                {
                    if (Pair.Value.Mode == ValueMode.Set)
                        query.AppendFormat("{0} = {1}", SQLiteContext.Escape(Pair.Key), WhereStatement.FormatSQLValue(Pair.Value.Value));
                    else
                        query.AppendFormat("{0} = {0} {1} {2}", SQLiteContext.Escape(Pair.Key), 
                            GetSign(Pair.Value.Mode), WhereStatement.FormatSQLValue(Pair.Value.Value));
                }
            }

            // Append Where
            if (this.WhereStatement.Count != 0)
                query.Append(" WHERE " + this.WhereStatement.BuildStatement(command));

            // Set the command text
            if (buildCommand) command.CommandText = query.ToString();
            return (buildCommand) ? command as object : query.ToString();
        }

        /// <summary>
        /// Executes the command against the database. The database driver must be set!
        /// </summary>
        /// <returns></returns>
        public int Execute()
        {
            using (DbCommand Command = BuildCommand())
                return Command.ExecuteNonQuery();
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
