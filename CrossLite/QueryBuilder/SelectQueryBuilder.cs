using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
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
    public class SelectQueryBuilder
    {
        #region Internal Properties

        protected SQLiteContext Context;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or Sets whether this Select statement will be distinct
        /// </summary>
        public bool Distinct = false;

        /// <summary>
        /// Gets or Sets the selected table for this query
        /// </summary>
        public string Table { get; set; }

        /// <summary>
        /// Gets a sorted list of (TableName => SelectedColumns[ColumnName => ColumnData])
        /// </summary>
        public SortedList<string, SortedList<string, ResultColumn>> SelectedItems { get; set; }

        /// <summary>
        /// The Where statement for this query
        /// </summary>
        public WhereStatement WhereStatement { get; set; } = new WhereStatement();

        /// <summary>
        /// The Having statement for this query
        /// </summary>
        public WhereStatement HavingStatement { get; set; } = new WhereStatement();

        /// <summary>
        /// Specifies the number of rows to return, after processing the OFFSET clause.
        /// </summary>
        public int Limit { get; set; } = 0;

        /// <summary>
        /// Specifies the number of rows to skip, before starting to return rows from the query expression.
        /// </summary>
        public int Offset { get; set; } = 0;

        /// <summary>
        /// Gets a list of columns this query will Group By
        /// </summary>
        public List<string> GroupByColumns { get; set; } = new List<string>();

        /// <summary>
        /// Gets a list of current Order By Statements on this query
        /// </summary>
        public List<OrderByClause> OrderByStatements { get; set; } = new List<OrderByClause>();

        /// <summary>
        /// Gets a list of Table Joins on this query
        /// </summary>
        public List<JoinClause> Joins { get; set; } = new List<JoinClause>();

        #endregion

        /// <summary>
        /// Creates a new instance of SelectQueryBuilder with the provided Database Driver.
        /// </summary>
        /// <param name="context">The DatabaseDriver that will be used to query this SQL statement</param>
        public SelectQueryBuilder(SQLiteContext context)
        {
            this.Context = context;
            SelectedItems = new SortedList<string, SortedList<string, ResultColumn>>();
        }

        #region Select Cols

        /// <summary>
        /// Selects all columns in the SQL Statement being built
        /// </summary>
        public void SelectAllColumns()
        {
            if (SelectedItems.Count == 0)
            {
                SelectedItems.Add(Table ?? "", new SortedList<string, ResultColumn>());
            }
        }

        /// <summary>
        /// Selects the count of rows in the SQL Statement being built
        /// </summary>
        public SelectQueryBuilder SelectCount()
        {
            return SelectColumn("COUNT(1)", "count", false);
        }

        /// <summary>
        /// Selects the distinct count of rows in the SQL Statement being built
        /// </summary>
        /// <param name="columnName">The Distinct column name</param>
        public SelectQueryBuilder SelectDistinctCount(string columnName)
        {
            columnName = SQLiteContext.Escape(columnName);
            return SelectColumn($"COUNT(DISTINCT {columnName})", "count", false);
        }

        /// <summary>
        /// Selects a specified column in the SQL Statement being built. Calling this method
        /// clears all previous selected columns
        /// </summary>
        /// <param name="column">The Column name to select</param>
        public SelectQueryBuilder SelectColumn(string column, string alias = null, bool escape = true)
        {
            // Ensure created with main table index
            if (SelectedItems.Count == 0)
                SelectedItems.Add(Table ?? "", new SortedList<string, ResultColumn>());

            // Add item to list
            SelectedItems.Values[SelectedItems.Count - 1][column] = new ResultColumn(column, alias, escape);
            return this;
        }

        /// <summary>
        /// Selects the specified columns in the SQL Statement being built. Calling this method
        /// clears all previous selected columns
        /// </summary>
        /// <param name="columns">The column names to select</param>
        public SelectQueryBuilder Select(params string[] columns)
        {
            // Ensure created with main table index
            if (SelectedItems.Count == 0)
                SelectedItems.Add(Table ?? "", new SortedList<string, ResultColumn>());

            // Add columns to the list
            var table = SelectedItems.Values[SelectedItems.Count - 1];
            foreach (string col in columns)
                table[col] = new ResultColumn(col);

            // Allow chaining
            return this;
        }

        public SelectQueryBuilder Select(IEnumerable<string> columns)
        {
            // Ensure created with main table index
            if (SelectedItems.Count == 0)
                SelectedItems.Add(Table ?? "", new SortedList<string, ResultColumn>());

            // Add columns to the list
            var table = SelectedItems.Values[SelectedItems.Count - 1];
            foreach (string col in columns)
                table[col] = new ResultColumn(col);

            // Allow chaining
            return this;
        }

        #endregion Select Cols

        #region From Table


        /// <summary>
        /// Sets the table name to be used in this SQL Statement
        /// </summary>
        /// <param name="table">The table name</param>
        public SelectQueryBuilder From(string table)
        {
            // Ensure we are not null
            if (String.IsNullOrWhiteSpace(table))
                throw new ArgumentNullException("table");

            // Set property
            Table = table;

            // Ensure created with main table index
            if (SelectedItems.Count == 0)
                SelectedItems.Add(Table, new SortedList<string, ResultColumn>());
            else
                SelectedItems.Keys[0] = table;

            return this;
        }

        #endregion From Table

        #region Alias

        /// <summary>
        /// Temporarily assigns a table column a new name for this query.
        /// Column names will be aliased by the order they were recieved.
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        public SelectQueryBuilder As(params string[] names)
        {
            // Make sure we have an item for crying out loud!
            if (SelectedItems.Count == 0)
                throw new Exception("Method call on \"AS\" not valid on a blank query! Please select some columns first!");

            // grab our table, and make sure the count is good
            var item = SelectedItems.Last();
            var tName = item.Key;
            var cols = item.Value;
            if (names.Length > cols.Count)
                throw new Exception($"Parameter count larger than selected column count on table \"{tName}\"");

            // Add the aliases
            int i = 0;
            foreach (string name in names)
                cols.Values[i++].Alias = name;

            return this;
        }

        /// <summary>
        /// Temporarily assigns a table column a new name for this query.
        /// </summary>
        /// <param name="index">The index at which the column name was added to this table.</param>
        /// <param name="name">The new alias name</param>
        /// <returns></returns>
        public SelectQueryBuilder Alias(int index, string name)
        {
            // Ensure we are not null
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("name");

            // Make sure we have an item for crying out loud!
            if (SelectedItems.Count == 0)
                throw new Exception("Method call on \"Alias\" not valid on a blank query! Please select some columns first!");

            // Set alias on item
            SelectedItems.Values.Last().Values[index].Alias = name;
            return this;
        }

        /// <summary>
        /// Tells the QueryBuilder not to escape the provided column names
        /// at the specified indexes. If no arguments are suppiled, all columns
        /// in this table will not be escaped.
        /// </summary>
        /// <param name="indexes"></param>
        /// <returns></returns>
        public SelectQueryBuilder NoEscapeOn(params int[] indexes)
        {
            // Make sure we have an item for crying out loud!
            if (SelectedItems.Count == 0)
                throw new Exception("Method call on \"NoEscape\" not valid on a blank query! Please select some columns first!");

            // grab our table, and make sure the count is good
            var item = SelectedItems.Last();
            var tName = item.Key;
            var cols = item.Value;

            // If we have specific indexes
            if (indexes.Length > 0)
            {
                // Ensure that we aren't going over the column count
                if (indexes.Max() > cols.Count)
                    throw new Exception($"Max index is larger than selected column count on table \"{tName}\"");

                foreach (int index in indexes)
                    cols.Values[index].Escape = false;
            }
            else
            {
                // No escape on all
                foreach (ResultColumn col in cols.Values)
                    col.Escape = false;
            }

            return this;
        }

        #endregion Alias

        #region Joins

        /// <summary>
        /// Adds a join clause to the current query object
        /// </summary>
        /// <param name="newJoin"></param>
        internal void AddJoin(JoinClause clause)
        {
            Joins.Add(clause);

            // Create new table mapping for clause
            SelectedItems[clause.JoiningTable] = new SortedList<string, ResultColumn>();
        }

        /// <summary>
        /// Creates a new Inner Join clause statement fot the current query object
        /// </summary>
        /// <param name="joinTable">The Joining Table name</param>
        /// <param name="joinColumn">The Joining Table Comparison Field</param>
        /// <param name="operator">the Comparison Operator used for the joining of thetwo tables</param>
        /// <param name="onTable">The table name we are joining INTO</param>
        /// <param name="onColumn">The From Table Comparison Field</param>
        public SelectQueryBuilder InnerJoin(string joinTable, string joinColumn, Comparison @operator, string onTable, string onColumn)
        {
            // Add clause to list
            var clause = new JoinClause(JoinType.InnerJoin, joinTable, joinColumn, @operator, onTable, onColumn);
            AddJoin(clause);
            return this;
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
        public WhereStatement Where(string field, Comparison @operator, object compareValue)
        {
            if (WhereStatement.InnerClauseOperator == LogicOperator.And)
                return WhereStatement.And(field, @operator, compareValue);
            else
                return WhereStatement.Or(field, @operator, compareValue);
        }

        #endregion Wheres

        #region Orderby

        /// <summary>
        /// Adds an OrderBy clause to the current query object
        /// </summary>
        /// <param name="Clause"></param>
        public void OrderBy(OrderByClause Clause) => OrderByStatements.Add(Clause);

        /// <summary>
        /// Creates and adds a new Oderby clause to the current query object
        /// </summary>
        /// <param name="FieldName"></param>
        /// <param name="Order"></param>
        public void OrderBy(string FieldName, Sorting Order)
        {
            OrderByStatements.Add(new OrderByClause(FieldName, Order));
        }


        #endregion Orderby

        #region Having

        public WhereStatement Having(string field, Comparison @operator, object compareValue)
        {
            if (HavingStatement.InnerClauseOperator == LogicOperator.And)
                return HavingStatement.And(field, @operator, compareValue);
            else
                return HavingStatement.Or(field, @operator, compareValue);
        }

        #endregion Having

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
        public SQLiteCommand BuildCommand() => BuildQuery(true) as SQLiteCommand;

        /// <summary>
        /// Builds the query string or DbCommand
        /// </summary>
        /// <param name="BuildCommand"></param>
        /// <returns></returns>
        protected object BuildQuery(bool BuildCommand)
        {
            // Make sure we have a table name
            if (SelectedItems.Count == 0 || String.IsNullOrWhiteSpace(SelectedItems.Keys[0]))
                throw new Exception("No tables were specified for this query.");

            // Start Query
            StringBuilder query = new StringBuilder("SELECT ");
            query.AppendIf(Distinct, "DISTINCT ");

            // Append columns
            int tableCount = SelectedItems.Count;
            foreach (var tables in SelectedItems)
            {
                int tableId = 1;
                int count = tables.Value.Count;
                tableCount--;

                // Check if the user wants to select all columns
                if (count == 0)
                {
                    query.AppendFormat("{0}.*", SQLiteContext.Escape($"t{tableId}"));
                    query.AppendIf(tableCount > 0, ", ");
                }
                else
                {
                    // Add each result selector to the query
                    foreach (ResultColumn column in tables.Value.Values)
                    {
                        string name = column.Name;
                        string alias = column.Alias ?? column.Name;
                        bool isAggregate = name.Contains("(");

                        // Do escaping unless the result is an aggregate funtion,
                        // or the user specifies otherwise
                        if (!isAggregate && column.Escape)
                            name = SQLiteContext.Escape(name);

                        // Do NOT apply table name prefix on functions
                        if (!isAggregate)
                        {
                            query.AppendFormat("{0}.{1} AS {2}",
                                SQLiteContext.Escape($"t{tableId}"),
                                name,
                                SQLiteContext.Escape(alias)
                            );
                        }
                        else
                        {
                            query.AppendFormat("{0} AS {1}", name, SQLiteContext.Escape(alias));
                        }

                        // If we have more results to select, append Comma
                        query.AppendIf(--count > 0 || tableCount > 0, ", ");
                    }
                }

                // move counters
                tableId++;
            }

            // Append main Table
            query.Append($" FROM {SQLiteContext.Escape(Table)} AS {SQLiteContext.Escape("t1")}");

            // Append Joined tables
            if (Joins.Count > 0)
            {
                int tableId = 2;
                foreach (JoinClause Clause in Joins)
                {
                    // Convert join type to string
                    switch (Clause.JoinType)
                    {
                        case JoinType.InnerJoin:
                            query.Append(" JOIN ");
                            break;
                        case JoinType.OuterJoin:
                            query.Append(" FULL OUTER JOIN ");
                            break;
                        case JoinType.LeftJoin:
                            query.Append(" LEFT JOIN ");
                            break;
                        case JoinType.RightJoin:
                            query.Append(" RIGHT JOIN ");
                            break;
                    }

                    // Append the join statement
                    query.Append($" {SQLiteContext.Escape(Clause.JoiningTable)} AS");
                    query.Append($" {SQLiteContext.Escape($"t{tableId++}")} ON ");
                    query.Append(
                        WhereStatement.CreateComparisonClause(
                            $"{Clause.JoiningTable}.{Clause.JoiningColumn}",
                            Clause.ComparisonOperator,
                            new SqlLiteral(SQLiteContext.Escape($"{Clause.FromTable}.{Clause.FromColumn}"))
                        )
                    );
                }
            }

            // Append Where Statement
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();
            if (WhereStatement.HasClause)
                query.Append(" WHERE " + WhereStatement.BuildStatement(Context, parameters));

            // Append GroupBy
            if (GroupByColumns.Count > 0)
                query.Append(" GROUP BY " + String.Join(", ", GroupByColumns.Select(x => SQLiteContext.Escape(x))));

            // Append Having
            if (HavingStatement.HasClause)
            {
                if (GroupByColumns.Count == 0)
                    throw new Exception("Having statement was set without Group By");

                query.Append(" HAVING " + HavingStatement.BuildStatement(Context, parameters));
            }

            // Append OrderBy
            if (OrderByStatements.Count > 0)
            {
                int count = OrderByStatements.Count;
                query.Append(" ORDER BY");
                foreach (OrderByClause Clause in OrderByStatements)
                {
                    query.Append($" {Clause.FieldName}");

                    // Add sorting if not default
                    query.AppendIf(Clause.SortOrder == Sorting.Descending, " DESC");

                    // Append seperator if we have more orderby statements
                    query.AppendIf(--count > 0, ",");
                }
            }

            // Append Limit
            query.AppendIf(Limit > 0, " LIMIT " + Limit);
            query.AppendIf(Offset > 0, " OFFSET " + Offset);

            // Create Command
            SQLiteCommand command = null;
            if (BuildCommand)
            {
                command = Context.CreateCommand(query.ToString());
                command.Parameters.AddRange(parameters.ToArray());
            }

            // Return Result
            return (BuildCommand) ? command as object : query.ToString();
        }

        /// <summary>
        /// Executes the built SQL statement on the Database connection that was passed
        /// in the contructor. All WHERE and HAVING paramenters are propery escaped, 
        /// making this command SQL Injection safe.
        /// </summary>
        public T ExecuteScalar<T>() where T : IConvertible
        {
            return Context.ExecuteScalar<T>(BuildCommand());
        }
    }
}
