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
        protected SQLiteContext Driver;

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Creates a new instance of UpdateQueryBuilder with the provided Database Driver.
        /// </summary>
        /// <param name="Driver">The DatabaseDriver that will be used to query this SQL statement</param>
        public UpdateQueryBuilder(SQLiteContext Driver)
        {
            this.Driver = Driver;
        }

        /// <summary>
        /// Creates a new instance of UpdateQueryBuilder with the provided Database Driver.
        /// </summary>
        /// <param name="Table">The table we are updating</param>
        /// <param name="Driver">The DatabaseDriver that will be used to query this SQL statement</param>
        public UpdateQueryBuilder(string Table, SQLiteContext Driver)
        {
            this.Table = Table;
            this.Driver = Driver;
        }

        #endregion Constructors

        #region Fields

        /// <summary>
        /// Sets a value for the specified field
        /// </summary>
        /// <param name="Field">The column or field name</param>
        /// <param name="Value">The new value to update</param>
        public void SetField(string Field, object Value)
        {
            this.SetField(Field, Value, ValueMode.Set);
        }

        /// <summary>
        /// Sets a value for the specified field
        /// </summary>
        /// <param name="Field">The column or field name</param>
        /// <param name="Value">The new value to update</param>
        /// <param name="Mode">Sets how the update value will be applied to the existing field value</param>
        public void SetField(string Field, object Value, ValueMode Mode)
        {
            if (Fields.ContainsKey(Field))
                Fields[Field] = new FieldValuePair(Field, Value, Mode);
            else
                Fields.Add(Field, new FieldValuePair(Field, Value, Mode));
        }

        #endregion Fields

        #region Where's

        public WhereClause AddWhere(string field, Comparison @operator, object compareValue)
        {
            WhereClause Clause = new WhereClause(field, @operator, compareValue);
            this.WhereStatement.Add(Clause);
            return Clause;
        }

        public void AddWhere(WhereClause Clause)
        {
            this.WhereStatement.Add(Clause);
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
        /// <param name="Table">The name of the table to update</param>
        public void SetTable(string Table)
        {
            this.Table = Table;
        }

        /// <summary>
        /// Sets the database driver
        /// </summary>
        /// <param name="Driver"></param>
        public void SetDbDriver(SQLiteContext Driver)
        {
            this.Driver = Driver;
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
        /// <param name="BuildCommand"></param>
        /// <returns></returns>
        protected object BuildQuery(bool BuildCommand)
        {
            // Make sure we have a valid DB driver
            if (BuildCommand && Driver == null)
                throw new Exception("Cannot build a command when the Db Drvier hasn't been specified. Call SetDbDriver first.");

            // Make sure we have a table name
            if (String.IsNullOrWhiteSpace(Table))
                throw new Exception("Table to update was not set.");

            // Make sure we have at least 1 field to update
            if (Fields.Count == 0)
                throw new Exception("No fields to update");

            // Create Command
            DbCommand Command = (BuildCommand) ? Driver.CreateCommand(null) : null;

            // Start Query
            StringBuilder Query = new StringBuilder($"UPDATE {Table} SET ");

            // Add Fields
            bool First = true;
            foreach (KeyValuePair<string, FieldValuePair> Pair in Fields)
            {
                // Append comma
                if (!First) Query.Append(", ");
                else First = false;

                // If using a command, Convert values to Parameters
                if (BuildCommand && Pair.Value.Value != null && Pair.Value.Value != DBNull.Value && !(Pair.Value.Value is SqlLiteral))
                {
                    // Create param for value
                    DbParameter Param = Command.CreateParameter();
                    Param.ParameterName = "@P" + Command.Parameters.Count;
                    Param.Value = Pair.Value.Value;

                    // Add Params to command
                    Command.Parameters.Add(Param);

                    // Append Query
                    if (Pair.Value.Mode == ValueMode.Set)
                        Query.Append(Pair.Key + " = " + Param.ParameterName);
                    else
                        Query.Append(String.Format("{0} = `{0}` {1} {2}", Pair.Key, GetSign(Pair.Value.Mode), Param.ParameterName));
                }
                else
                {
                    if (Pair.Value.Mode == ValueMode.Set)
                        Query.Append(Pair.Key + " = " + WhereStatement.FormatSQLValue(Pair.Value.Value));
                    else
                        Query.Append(String.Format("{0} = `{0}` {1} {2}", Pair.Key, GetSign(Pair.Value.Mode), WhereStatement.FormatSQLValue(Pair.Value.Value)));
                }
            }

            // Append Where
            if (this.WhereStatement.Count != 0)
                Query.Append(" WHERE " + this.WhereStatement.BuildStatement(Command));

            // Set the command text
            if (BuildCommand) Command.CommandText = Query.ToString();
            return (BuildCommand) ? Command as object : Query.ToString();
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
