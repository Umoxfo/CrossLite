using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace CrossLite.QueryBuilder
{
    /// <summary>
    /// Provides an object interface that can properly put together an SQL Reader Query string.
    /// </summary>
    /// <remarks>
    /// All parameters in the WHERE and HAVING statements will be escaped by the underlaying
    /// DbCommand object when using the BuildCommand() method. The Execute*() methods are 
    /// SQL injection safe and will properly escape the values in the query.
    /// </remarks>
    /// <example>
    /// 
    /// Simple (select all):
    ///     var builder = new SelectQueryBuilder(context);
    ///     builder.From(tableName).SelectAll();
    ///     SQLiteCommand command = builder.BuildCommand();
    ///     
    /// Simple (with Where statement):
    ///     var builder = new SelectQueryBuilder(context);
    ///     builder.From(tableName)
    ///         .Select("col1", "col2", "col3")
    ///         .Where("id").Between(1, 10);
    ///     SQLiteCommand command = builder.BuildCommand();
    ///     
    /// Advanced Select:
    ///     var builder = new SelectQueryBuilder(context);
    ///     builder.From(tableName)
    ///         .Select("col1", "col2", "col3")
    ///             .As("id", "name", "is_admnin")
    ///         .OrderBy("id", Sorting.Descending)
    ///         .Where("id").GreaterThan(1)
    ///             .And("is_admin").Equals(true);
    ///     builder.Limit = 50;
    ///     builder.Offset = 10;
    ///     SQLiteCommand command = builder.BuildCommand();
    ///     
    /// </example>
    public class SelectQueryBuilder
    {
        #region Internal Properties

        protected SQLiteContext Context;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or Sets whether this Select statement will be distinct
        /// </summary>
        public bool Distinct { get; set; } =false;

        /// <summary>
        /// Gets or Sets the selected table for this query
        /// </summary>
        public string Table
        {
            get { return (SelectedItems.Count > 0) ? SelectedItems.Keys[0] : null;  }
            set { From(value); }
        }

        /// <summary>
        /// Gets a sorted list of (TableName => SelectedColumns[ColumnName => ColumnData])
        /// </summary>
        public SortedList<string, SortedList<string, ResultColumn>> SelectedItems { get; set; }

        /// <summary>
        /// The Where statement for this query
        /// </summary>
        public WhereStatement WhereStatement { get; set; }

        /// <summary>
        /// The Having statement for this query
        /// </summary>
        public WhereStatement HavingStatement { get; set; }

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

        /// <summary>
        /// Gets a list of columns this query will Group By
        /// </summary>
        public List<UnionStatement> Unions { get; set; } = new List<UnionStatement>(2);

        #endregion

        /// <summary>
        /// Creates a new instance of SelectQueryBuilder with the provided SQLite connection.
        /// </summary>
        /// <param name="context">The SQLiteContext that will be used to build and query this SQL statement</param>
        public SelectQueryBuilder(SQLiteContext context)
        {
            this.Context = context;
            this.SelectedItems = new SortedList<string, SortedList<string, ResultColumn>>();

            // Set qouting modes
            this.WhereStatement = new WhereStatement(context);
            this.HavingStatement = new WhereStatement(context);
        }

        #region Select Cols

        /// <summary>
        /// Selects all columns in the SQL Statement being built
        /// </summary>
        public SelectQueryBuilder SelectAll()
        {
            if (SelectedItems.Count == 0)
                SelectedItems.Add(Table ?? "", new SortedList<string, ResultColumn>());
            else
                SelectedItems.Values[SelectedItems.Count - 1].Clear();
            
            return this;
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
            columnName = SQLiteContext.QuoteKeyword(columnName);
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
        /// Adds the specified column selectors in the SQL Statement being built.
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

        /// <summary>
        /// Adds the specified column selectors in the SQL Statement being built.
        /// </summary>
        /// <param name="columns">The column names to select</param>
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
                throw new ArgumentNullException("Tablename cannot be null or empty!", "table");

            // Ensure created with main table index
            if (SelectedItems.Count == 0)
                SelectedItems.Add(table, new SortedList<string, ResultColumn>());
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
            AddJoin(new JoinClause(JoinType.InnerJoin, joinTable, joinColumn, @operator, onTable, onColumn));
            return this;
        }

        /// <summary>
        /// Creates a new Outer Join clause statement fot the current query object
        /// </summary>
        /// <param name="joinTable">The Joining Table name</param>
        /// <param name="joinColumn">The Joining Table Comparison Field</param>
        /// <param name="operator">the Comparison Operator used for the joining of thetwo tables</param>
        /// <param name="onTable">The table name we are joining INTO</param>
        /// <param name="onColumn">The From Table Comparison Field</param>
        public SelectQueryBuilder OuterJoin(string joinTable, string joinColumn, Comparison @operator, string onTable, string onColumn)
        {
            // Add clause to list
            AddJoin(new JoinClause(JoinType.OuterJoin, joinTable, joinColumn, @operator, onTable, onColumn));
            return this;
        }

        /// <summary>
        /// Creates a new Left Join clause statement fot the current query object
        /// </summary>
        /// <param name="joinTable">The Joining Table name</param>
        /// <param name="joinColumn">The Joining Table Comparison Field</param>
        /// <param name="operator">the Comparison Operator used for the joining of thetwo tables</param>
        /// <param name="onTable">The table name we are joining INTO</param>
        /// <param name="onColumn">The From Table Comparison Field</param>
        public SelectQueryBuilder LeftJoin(string joinTable, string joinColumn, Comparison @operator, string onTable, string onColumn)
        {
            // Add clause to list
            AddJoin(new JoinClause(JoinType.LeftJoin, joinTable, joinColumn, @operator, onTable, onColumn));
            return this;
        }

        /// <summary>
        /// Creates a new Right Join clause statement fot the current query object
        /// </summary>
        /// <param name="joinTable">The Joining Table name</param>
        /// <param name="joinColumn">The Joining Table Comparison Field</param>
        /// <param name="operator">the Comparison Operator used for the joining of thetwo tables</param>
        /// <param name="onTable">The table name we are joining INTO</param>
        /// <param name="onColumn">The From Table Comparison Field</param>
        public SelectQueryBuilder RightJoin(string joinTable, string joinColumn, Comparison @operator, string onTable, string onColumn)
        {
            // Add clause to list
            AddJoin(new JoinClause(JoinType.RightJoin, joinTable, joinColumn, @operator, onTable, onColumn));
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

        /// <summary>
        /// Creates a where clause to add to the query's where statement
        /// </summary>
        /// <param name="field">The column name</param>
        /// <returns></returns>
        public SqlExpression Where(string field)
        {
            if (WhereStatement.InnerClauseOperator == LogicOperator.And)
                return WhereStatement.And(field);
            else
                return WhereStatement.Or(field);
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

        #region Union

        /// <summary>
        /// Adds a UNION clause to the query, which is used to combine the results of two or more 
        /// SELECT statements without returning any duplicate rows.
        /// </summary>
        /// <param name="table">The table to unionize</param>
        /// <returns>Returns a new instance of <see cref="SelectQueryBuilder"/> for the union query.</returns>
        public SelectQueryBuilder Union(string table)
        {
            var statement = new UnionStatement()
            {
                Type = UnionType.Union,
                Query = new SelectQueryBuilder(Context).From(table)
            };
            Unions.Add(statement);
            return statement.Query;
        }

        /// <summary>
        /// Adds a UNION clause to the query, which is used to combine the results of two or more 
        /// SELECT statements without returning any duplicate rows.
        /// </summary>
        /// <param name="query">The query to compound in this query statement</param>
        /// <returns>Returns this instance of <see cref="SelectQueryBuilder"/></returns>
        public SelectQueryBuilder Union(SelectQueryBuilder query)
        {
            var statement = new UnionStatement()
            {
                Type = UnionType.Union,
                Query = query
            };
            Unions.Add(statement);
            return this;
        }

        /// <summary>
        /// Adds a UNION ALL clause to the query, which is used to combine the results of two or more 
        /// SELECT statements and it does not remove duplicate rows between the various SELECT statements.
        /// </summary>
        /// <param name="table">The table to unionize</param>
        /// <returns>Returns a new instance of <see cref="SelectQueryBuilder"/> for the union query.</returns>
        public SelectQueryBuilder UnionAll(string table)
        {
            var statement = new UnionStatement()
            {
                Type = UnionType.UnionAll,
                Query = new SelectQueryBuilder(Context).From(table)
            };
            Unions.Add(statement);
            return statement.Query;
        }

        /// <summary>
        /// Adds a UNION ALL clause to the query, which is used to combine the results of two or more 
        /// SELECT statements and it does not remove duplicate rows between the various SELECT statements.
        /// </summary>
        /// <param name="query">The query to compound in this query statement</param>
        /// <returns>Returns this instance of <see cref="SelectQueryBuilder"/></returns>
        public SelectQueryBuilder UnionAll(SelectQueryBuilder query)
        {
            var statement = new UnionStatement()
            {
                Type = UnionType.UnionAll,
                Query = query
            };
            Unions.Add(statement);
            return this;
        }

        /// <summary>
        /// Adds an EXCEPT operator to the query, which is used to return all rows in this SELECT statement 
        /// that are not returned by the new SELECT statement
        /// </summary>
        /// <param name="table">The table to unionize</param>
        /// <returns>Returns a new instance of <see cref="SelectQueryBuilder"/> for the union query.</returns>
        public SelectQueryBuilder Except(string table)
        {
            var statement = new UnionStatement()
            {
                Type = UnionType.Except,
                Query = new SelectQueryBuilder(Context).From(table)
            };
            Unions.Add(statement);
            return statement.Query;
        }

        /// <summary>
        /// Adds an EXCEPT operator to the query, which is used to return all rows in this SELECT statement 
        /// that are not returned by the new SELECT statement
        /// </summary>
        /// <param name="query">The query to compound in this query statement</param>
        /// <returns>Returns this instance of <see cref="SelectQueryBuilder"/></returns>
        public SelectQueryBuilder Except(SelectQueryBuilder query)
        {
            var statement = new UnionStatement()
            {
                Type = UnionType.Except,
                Query = query
            };
            Unions.Add(statement);
            return this;
        }

        /// <summary>
        /// Adds an INTERSECT operator to the query, which returns the intersection of 2 or more datasets
        /// </summary>
        /// <param name="table">The table to intersect</param>
        /// <returns>Returns a new instance of <see cref="SelectQueryBuilder"/> for the union query.</returns>
        public SelectQueryBuilder Intersect(string table)
        {
            var statement = new UnionStatement()
            {
                Type = UnionType.Intersect,
                Query = new SelectQueryBuilder(Context).From(table)
            };
            Unions.Add(statement);
            return statement.Query;
        }

        /// <summary>
        /// Adds an INTERSECT operator to the query, which returns the intersection of 2 or more datasets
        /// </summary>
        /// <param name="query">The query to compound in this query statement</param>
        /// <returns>Returns this instance of <see cref="SelectQueryBuilder"/></returns>
        public SelectQueryBuilder Intersect(SelectQueryBuilder query)
        {
            var statement = new UnionStatement()
            {
                Type = UnionType.Intersect,
                Query = query
            };
            Unions.Add(statement);
            return this;
        }

        #endregion

        /// <summary>
        /// Bypasses the specified amount of records (offset) in the result set.
        /// </summary>
        /// <param name="">The offset in the query</param>
        /// <returns></returns>
        public SelectQueryBuilder Skip(int count)
        {
            // Set internal value
            this.Offset = count;

            // Allow chaining
            return this;
        }

        /// <summary>
        /// Specifies the maximum number of records to return in the query (limit)
        /// </summary>
        /// <param name="">The number of records to grab from the result set</param>
        /// <returns></returns>
        public SelectQueryBuilder Take(int count)
        {
            // Set internal value
            this.Limit = count;

            // Allow chaining
            return this;
        }

        /// <summary>
        /// Builds the query string with the current SQL Statement, and returns
        /// the querystring. This method is NOT Sql Injection safe!
        /// </summary>
        /// <returns></returns>
        public string BuildQuery() => BuildQuery(false) as String;

        /// <summary>
        /// Builds the query string with the current SQL Statement, and
        /// returns the SQLiteCommand to be executed. All WHERE and HAVING paramenters
        /// are propery escaped, making this command SQL Injection safe.
        /// </summary>
        /// <returns></returns>
        public SQLiteCommand BuildCommand() => BuildQuery(true) as SQLiteCommand;

        /// <summary>
        /// Builds the query string or SQLiteCommand
        /// </summary>
        /// <param name="buildCommand"></param>
        /// <returns></returns>
        protected object BuildQuery(bool buildCommand)
        {
            // Make sure we have a table name
            if (SelectedItems.Count == 0 || String.IsNullOrWhiteSpace(SelectedItems.Keys[0]))
                throw new Exception("No tables were specified for this query.");

            // Start Query
            StringBuilder query = new StringBuilder("SELECT ", 256);
            query.AppendIf(Distinct, "DISTINCT ");

            // Append columns from each table
            int tableCount = SelectedItems.Count;
            foreach (var table in SelectedItems)
            {
                int tableId = 1;
                int colCount = table.Value.Count;
                tableCount--;

                // Check if the user wants to select all columns
                if (colCount == 0)
                {
                    query.AppendFormat("{0}.*", Context.QuoteAttribute($"t{tableId}"));
                    query.AppendIf(tableCount > 0, ", ");
                }
                else
                {
                    // Add each result selector to the query
                    foreach (ResultColumn column in table.Value.Values)
                    {
                        string name = column.Name;
                        string alias = column.Alias ?? column.Name;
                        bool isAggregate = name.Contains("(");

                        // Do escaping unless the result is an aggregate funtion,
                        // or the user specifies otherwise
                        if (!isAggregate && column.Escape)
                            name = Context.QuoteAttribute(name);

                        // Do NOT apply table name prefix on functions
                        if (!isAggregate)
                        {
                            query.AppendFormat("{0}.{1} AS {2}",
                                Context.QuoteAttribute($"t{tableId}"),
                                name,
                                Context.QuoteAttribute(alias)
                            );
                        }
                        else
                        {
                            query.AppendFormat("{0} AS {1}", name, Context.QuoteAttribute(alias));
                        }

                        // If we have more results to select, append Comma
                        query.AppendIf(--colCount > 0 || tableCount > 0, ", ");
                    }
                }

                // move counters
                tableId++;
            }

            // Append main Table
            query.Append($" FROM {Context.QuoteAttribute(Table)} AS {Context.QuoteAttribute("t1")}");

            // Append Joined tables
            if (Joins.Count > 0)
            {
                int tableId = 2;
                foreach (JoinClause clause in Joins)
                {
                    // Convert join type to string
                    switch (clause.JoinType)
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
                    query.Append($" {Context.QuoteAttribute(clause.JoiningTable)} AS");
                    query.Append($" {Context.QuoteAttribute($"t{tableId++}")} ON ");
                    query.Append(
                        SqlExpression.CreateExpressionString(
                            Context.QuoteAttribute($"{clause.JoiningTable}.{clause.JoiningColumn}"),
                            clause.ComparisonOperator,
                            new SqlLiteral(Context.QuoteAttribute($"{clause.FromTable}.{clause.FromColumn}"))
                        )
                    );
                }
            }

            // Append Where Statement
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();
            if (WhereStatement.HasClause)
            {
                if (buildCommand)
                    query.Append(" WHERE " + WhereStatement.BuildStatement(parameters));
                else
                    query.Append(" WHERE " + WhereStatement.BuildStatement());
            }

            // Append GroupBy
            if (GroupByColumns.Count > 0)
                query.Append(" GROUP BY " + String.Join(", ", GroupByColumns.Select(x => Context.QuoteAttribute(x))));

            // Append Having
            if (HavingStatement.HasClause)
            {
                if (GroupByColumns.Count == 0)
                    throw new Exception("Having statement was set without Group By");

                query.Append(" HAVING " + HavingStatement.BuildStatement(parameters));
            }

            // Append OrderBy
            if (OrderByStatements.Count > 0)
            {
                int count = OrderByStatements.Count;
                query.Append(" ORDER BY");
                foreach (OrderByClause clause in OrderByStatements)
                {
                    query.Append($" {Context.QuoteAttribute(clause.FieldName)}");

                    // Add sorting if not default
                    query.AppendIf(clause.SortOrder == Sorting.Descending, " DESC");

                    // Append seperator if we have more orderby statements
                    query.AppendIf(--count > 0, ",");
                }
            }

            // Append Limit
            query.AppendIf(Limit > 0, " LIMIT " + Limit);
            query.AppendIf(Offset > 0, " OFFSET " + Offset);

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
        /// Executes the built SQL statement on the SQLite database connection that was passed
        /// in the constructor. All WHERE and HAVING paramenters are escaped, making this command 
        /// SQL Injection safe.
        /// </summary>
        public T ExecuteScalar<T>() where T : IConvertible
        {
            return Context.ExecuteScalar<T>(BuildCommand());
        }

        /// <summary>
        /// Executes the built SQL statement on the SQLite database connection that was passed
        /// in the constructor. All WHERE and HAVING paramenters are escaped, making this command 
        /// SQL Injection safe.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Dictionary<string, object>> ExecuteQuery()
        {
            return Context.ExecuteReader(BuildCommand());
        }

        /// <summary>
        /// Executes the built SQL statement on the SQLite database connection that was passed
        /// in the constructor. All WHERE and HAVING paramenters are escaped, making this command 
        /// SQL Injection safe.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> ExecuteQuery<T>() where T : class
        {
            return Context.ExecuteReader<T>(BuildCommand());
        }
    }
}
