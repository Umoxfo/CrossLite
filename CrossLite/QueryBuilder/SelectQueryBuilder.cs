using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace CrossLite.QueryBuilder
{
    /// <summary>
    /// Provides an object interface that can properly put together a Reader Query string.
    /// </summary>
    /// <remarks>
    /// All parameters in the WHERE and HAVING statements will be escaped by the underlaying
    /// DbCommand object, making the Execute*() methods SQL injection safe.
    /// </remarks>
    class SelectQueryBuilder
    {
        #region Internal Properties

        protected List<string> selectedColumns = new List<string>();
        protected List<string> selectedTables = new List<string>();
        protected List<OrderByClause> OrderByStatements = new List<OrderByClause>();
        protected List<JoinClause> Joins = new List<JoinClause>();
        protected List<string> GroupByColumns = new List<string>();
        protected int[] LimitRecords = null;
        protected SQLiteContext Driver;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or Sets whether this Select statement will be distinct
        /// </summary>
        public bool Distinct = false;

        /// <summary>
        /// The selected columns for this query. We convert to an array,
        /// which un-references the original list, and prevents modifications
        /// </summary>
        public string[] SelectedColumns => (selectedColumns.Count > 0) ? selectedColumns.ToArray() : new string[1] { "*" };

        /// <summary>
        /// The selected tables for this query. We convert to an array,
        /// which un-references the original list, and prevents modifications
        /// </summary>
        public string[] SelectedTables => this.selectedTables.ToArray();

        /// <summary>
        /// The Where statement for this query
        /// </summary>
        public WhereStatement WhereStatement = new WhereStatement();

        /// <summary>
        /// The Having statement for this query
        /// </summary>
        public WhereStatement HavingStatement = new WhereStatement();

        #endregion

        /// <summary>
        /// Creates a new instance of SelectQueryBuilder with the provided Database Driver.
        /// </summary>
        /// <param name="Driver">The DatabaseDriver that will be used to query this SQL statement</param>
        public SelectQueryBuilder(SQLiteContext Driver)
        {
            this.Driver = Driver;
        }

        /// <summary>
        /// Sets the database driver
        /// </summary>
        /// <param name="Driver">The database driver for this SQL statement</param>
        public void SetDbDriver(SQLiteContext Driver) => this.Driver = Driver;

        #region Select Cols

        /// <summary>
        /// Selects all columns in the SQL Statement being built
        /// </summary>
        public void SelectAllColumns() => this.selectedColumns.Clear();

        /// <summary>
        /// Selects the count of rows in the SQL Statement being built
        /// </summary>
        public void SelectCount() => this.SelectColumn("COUNT(1) as count");

        /// <summary>
        /// Selects the distinct count of rows in the SQL Statement being built
        /// </summary>
        /// <param name="ColumnName">The Distinct column name</param>
        public void SelectDistinctCount(string ColumnName)
        {
            this.SelectColumn($"COUNT(DISTINCT {ColumnName}) as count");
        }

        /// <summary>
        /// Selects a specified column in the SQL Statement being built. Calling this method
        /// clears all previous selected columns
        /// </summary>
        /// <param name="column">The Column name to select</param>
        public void SelectColumn(string column)
        {
            this.selectedColumns.Clear();
            this.selectedColumns.Add(column);
        }

        /// <summary>
        /// Selects the specified columns in the SQL Statement being built. Calling this method
        /// clears all previous selected columns
        /// </summary>
        /// <param name="columns">The column names to select</param>
        public void SelectColumns(params string[] columns)
        {
            this.selectedColumns.Clear();
            foreach (string str in columns)
                this.selectedColumns.Add(str);
        }

        #endregion Select Cols

        #region Select From


        /// <summary>
        /// Sets the table name to be used in this SQL Statement
        /// </summary>
        /// <param name="table">The table name</param>
        public void SelectFromTable(string table)
        {
            this.selectedTables.Clear();
            this.selectedTables.Add(table);
        }

        /// <summary>
        /// Sets the table names to be used in this SQL Statement
        /// </summary>
        /// <param name="tables">Each param passed is another table name</param>
        public void SelectFromTables(params string[] tables)
        {
            this.selectedTables.Clear();
            foreach (string str in tables)
                this.selectedTables.Add(str);
        }

        #endregion Select From

        #region Joins

        /// <summary>
        /// Adds a join clause to the current query object
        /// </summary>
        /// <param name="newJoin"></param>
        public void AddJoin(JoinClause newJoin) => this.Joins.Add(newJoin);

        /// <summary>
        /// Creates a new Join clause statement fot the current query object
        /// </summary>
        /// <param name="join">Specifies the Type of Join statement this is.</param>
        /// <param name="toTableName">The Joining Table name</param>
        /// <param name="toColumnName">The Joining Table Comparison Field</param>
        /// <param name="operator">the Comparison Operator used for the joining of thetwo tables</param>
        /// <param name="fromTableName">The table name we are joining INTO</param>
        /// <param name="fromColumnName">The From Table Comparison Field</param>
        public void AddJoin(JoinType join, string toTableName, string toColumnName, Comparison @operator, string fromTableName, string fromColumnName)
        {
            this.Joins.Add(new JoinClause(join, toTableName, toColumnName, @operator, fromTableName, fromColumnName));
        }

        #endregion Joins

        #region Wheres

        /// <summary>
        /// Creates a where clause to add to the query's where statement
        /// </summary>
        /// <param name="field">The column name</param>
        /// <param name="operator">The Comaparison Operator to use</param>
        /// <param name="compareValue">The value, for the column name and comparison operator</param>
        /// <returns></returns>
        public WhereClause AddWhere(string field, Comparison @operator, object compareValue)
        {
            WhereClause Clause = new WhereClause(field, @operator, compareValue);
            this.WhereStatement.Add(Clause);
            return Clause;
        }

        /// <summary>
        /// Adds a where clause to the current query statement
        /// </summary>
        /// <param name="Clause"></param>
        public void AddWhere(WhereClause Clause) => this.WhereStatement.Add(Clause);

        /// <summary>
        /// Sets the Logic Operator for the WHERE statement
        /// </summary>
        /// <param name="Operator"></param>
        public void SetWhereOperator(LogicOperator @Operator)
        {
            this.WhereStatement.StatementOperator = @Operator;
        }

        #endregion Wheres

        #region Orderby

        /// <summary>
        /// Adds an OrderBy clause to the current query object
        /// </summary>
        /// <param name="Clause"></param>
        public void AddOrderBy(OrderByClause Clause) => OrderByStatements.Add(Clause);

        /// <summary>
        /// Creates and adds a new Oderby clause to the current query object
        /// </summary>
        /// <param name="FieldName"></param>
        /// <param name="Order"></param>
        public void AddOrderBy(string FieldName, Sorting Order)
        {
            OrderByStatements.Add(new OrderByClause(FieldName, Order));
        }


        #endregion Orderby

        #region Having

        public WhereClause AddHaving(string field, Comparison @operator, object compareValue)
        {
            WhereClause Clause = new WhereClause(field, @operator, compareValue);
            this.HavingStatement.Add(Clause);
            return Clause;
        }

        public void AddHaving(WhereClause Clause)
        {
            this.HavingStatement.Add(Clause);
        }

        /// <summary>
        /// Sets the Logic Operator for the WHERE statement
        /// </summary>
        /// <param name="Operator"></param>
        public void SetHavingOperator(LogicOperator @Operator)
        {
            this.HavingStatement.StatementOperator = @Operator;
        }

        #endregion Having

        /// <summary>
        /// Limit is used to limit your query results to those that fall within a specified range
        /// </summary>
        /// <param name="Records">The number if rows to be returned in the result set</param>
        public void Limit(int Records) =>  this.LimitRecords = new[] { Records };

        /// <summary>
        /// Limit is used to limit your query results to those that fall within a specified range
        /// </summary>
        /// <param name="Records">The number if rows to be returned in the result set</param>
        /// <param name="Start">The starting point or record (remember the first record is 0)</param>
        public void Limit(int Records, int Start) => this.LimitRecords = new[] { Records, Start };

        /// <summary>
        /// Builds the query string with the current SQL Statement, and returns
        /// the querystring. This method is NOT Sql Injection safe!
        /// </summary>
        /// <returns></returns>
        public string BuildQuery() => BuildQuery(false) as String;

        /// <summary>
        /// Builds the query string with the current SQL Statement, and
        /// returns the DbCommand to be executed. All WHERE and HAVING paramenters
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
            if (selectedTables.Count == 0)
                throw new Exception("No tables were specified for this query.");

            // Create Command
            DbCommand Command = (BuildCommand) ? Driver.CreateCommand(null) : null;

            // Start Query
            StringBuilder Query = new StringBuilder("SELECT ");
            Query.AppendIf(Distinct, "DISTINCT ");

            // Append columns
            Query.Append(String.Join(", ", SelectedColumns));

            // Append Tables
            Query.Append(" FROM " + String.Join(", ", SelectedTables));

            // Append Joins
            if (Joins.Count > 0)
            {
                foreach (JoinClause Clause in Joins)
                {
                    // Convert join type to string
                    switch (Clause.JoinType)
                    {
                        case JoinType.InnerJoin:
                            Query.Append(" JOIN ");
                            break;
                        case JoinType.OuterJoin:
                            Query.Append(" FULL OUTER JOIN ");
                            break;
                        case JoinType.LeftJoin:
                            Query.Append(" LEFT JOIN ");
                            break;
                        case JoinType.RightJoin:
                            Query.Append(" RIGHT JOIN ");
                            break;
                    }

                    // Append the join statement
                    Query.Append($"{Clause.JoiningTable} ON ");
                    Query.Append(
                        WhereStatement.CreateComparisonClause(
                            $"{Clause.JoiningTable}.{Clause.JoiningColumn}",
                            Clause.ComparisonOperator,
                            new SqlLiteral($"{Clause.FromTable}.{Clause.FromColumn}")
                        )
                    );
                }
            }


            // Append Where
            if (this.WhereStatement.Count != 0)
                Query.Append(" WHERE " + WhereStatement.BuildStatement(Command));

            // Append GroupBy
            if (GroupByColumns.Count > 0)
                Query.Append(" GROUP BY " + String.Join(", ", GroupByColumns));

            // Append Having
            if (HavingStatement.Count > 0)
            {
                if (GroupByColumns.Count == 0)
                    throw new Exception("Having statement was set without Group By");

                Query.Append(" HAVING " + this.HavingStatement.BuildStatement(Command));
            }

            // Append OrderBy
            if (OrderByStatements.Count > 0)
            {
                int count = OrderByStatements.Count;
                Query.Append(" ORDER BY");
                foreach (OrderByClause Clause in OrderByStatements)
                {
                    Query.Append($" {Clause.FieldName}");

                    // Add sorting if not default
                    Query.AppendIf(Clause.SortOrder == Sorting.Descending, " DESC");

                    // Append seperator if we have more orderby statements
                    Query.AppendIf(--count > 0, ",");
                }
            }

            // Append Limit
            if (LimitRecords is Array)
            {
                if (LimitRecords.Length == 1)
                    Query.Append(" LIMIT " + LimitRecords[0].ToString());
                else
                    Query.Append($" LIMIT {LimitRecords[1]}, {LimitRecords[0]}");
            }

            // Set the command text
            if (BuildCommand)
                Command.CommandText = Query.ToString();

            // Return Result
            return (BuildCommand) ? Command as object : Query.ToString();
        }

        /// <summary>
        /// Executes the built SQL statement on the Database connection that was passed
        /// in the contructor. All WHERE and HAVING paramenters are propery escaped, 
        /// making this command SQL Injection safe.
        /// </summary>
        public T ExecuteScalar<T>()
        {
            return Driver.ExecuteScalar<T>(BuildCommand());
        }
    }
}
