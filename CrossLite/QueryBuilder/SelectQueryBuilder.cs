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
    /// SQL injection safe and will properly escape attribute values in the query.
    /// ----
    /// This is not a forward only builder, meaning you can call the methods in this class
    /// in any order, and will still get the same output.
    /// ---
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
    ///         .Where("id").GreaterThan(1)
    ///             .And("is_admin").Equals(true)
    ///         .OrderBy("id", Sorting.Descending)
    ///         .Skip(10)
    ///         .Take(5);
    ///     SQLiteCommand command = builder.BuildCommand();
    ///     
    /// Advanced Select with a Join:
    ///     var builder = new SelectQueryBuilder(context);
    ///     builder.From(tableName, "table1")
    ///         .Select("col1", "col2", "col3")
    ///         .InnerJoin("table2").As("t2").On("col22").Equals("table1", "col1")
    ///         .Select("col22", "col23") --> Call Select again to grab columns from the joined table
    ///         .Where("col1").NotEqualTo(1)
    ///             .And("col22").In(1, 3, 5, 7, 9)
    ///         .OrderBy("col1", Sorting.Descending)
    ///     string query = builder.BuildQuery();
    ///     
    /// </example>
    public class SelectQueryBuilder
    {
        #region Public Properties

        /// <summary>
        /// The SQLiteContext attached to this builder
        /// </summary>
        public SQLiteContext Context { get; protected set; }

        /// <summary>
        /// Gets or Sets whether this Select statement will be distinct
        /// </summary>
        public bool Distinct { get; set; } = false;

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
        public SortedList<string, SortedList<string, ColumnSelector>> SelectedItems { get; set; }

        /// <summary>
        /// Gets a sorted list of (TableName => Alias)
        /// </summary>
        internal Dictionary<string, string> TableAliases { get; set; }

        /// <summary>
        /// The Where statement for this query
        /// </summary>
        public SelectWhereStatement WhereStatement { get; set; }

        /// <summary>
        /// The Having statement for this query
        /// </summary>
        public SelectWhereStatement HavingStatement { get; set; }

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
            this.SelectedItems = new SortedList<string, SortedList<string, ColumnSelector>>();
            this.TableAliases = new Dictionary<string, string>();

            // Set qouting modes
            this.WhereStatement = new SelectWhereStatement(this);
            this.HavingStatement = new SelectWhereStatement(this);
        }

        #region Select Cols

        /// <summary>
        /// Selects all columns in the SQL Statement being built
        /// </summary>
        public SelectQueryBuilder SelectAll()
        {
            if (SelectedItems.Count == 0)
                SelectedItems.Add(Table ?? "", new SortedList<string, ColumnSelector>());
            else
                SelectedItems.Values[SelectedItems.Count - 1].Clear();
            
            return this;
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
                SelectedItems.Add(Table ?? "", new SortedList<string, ColumnSelector>());

            // Add item to list
            SelectedItems.Values[SelectedItems.Count - 1][column] = new ColumnSelector(column, alias, escape);
            return this;
        }

        /// <summary>
        /// Adds the specified column selectors in the SQL Statement being built.
        /// </summary>
        /// <param name="columns">The column names to select</param>
        public SelectQueryBuilder Select(params string[] columns)
        {
            // Make sure the array isnt empty...
            if (columns.Length == 0) return this;

            // Ensure created with main table index
            if (SelectedItems.Count == 0)
                SelectedItems.Add(Table ?? "", new SortedList<string, ColumnSelector>());

            // Add columns to the list
            var table = SelectedItems.Values[SelectedItems.Count - 1];
            foreach (string col in columns)
                table[col] = new ColumnSelector(col);

            // Allow chaining
            return this;
        }

        /// <summary>
        /// Adds the specified column selectors in the SQL Statement being built.
        /// </summary>
        /// <param name="columns">The column names to select</param>
        public SelectQueryBuilder Select(IEnumerable<string> columns)
        {
            // Make sure the array isnt empty...
            if (columns.Count() == 0) return this;

            // Ensure created with main table index
            if (SelectedItems.Count == 0)
                SelectedItems.Add(Table ?? "", new SortedList<string, ColumnSelector>());

            // Add columns to the list
            var table = SelectedItems.Values[SelectedItems.Count - 1];
            foreach (string col in columns)
                table[col] = new ColumnSelector(col);

            // Allow chaining
            return this;
        }

        #endregion Select Cols

        #region Aggregates

        /// <summary>
        /// Adds an aggregate selection to the current table selector
        /// </summary>
        /// <param name="column">The column name to perform the aggregate on</param>
        /// <param name="alias">The return alias of the aggregate result, if any.</param>
        /// <param name="type">The aggregate type</param>
        /// <returns></returns>
        internal SelectQueryBuilder Aggregate(string column, string alias, AggregateFunction type)
        {
            // Ensure created with main table index
            if (SelectedItems.Count == 0)
                SelectedItems.Add(Table ?? "", new SortedList<string, ColumnSelector>());

            // Ensure the column name is correct
            if (String.IsNullOrWhiteSpace(column))
            {
                // Only count can be null or empty
                if (type != AggregateFunction.Count)
                {
                    string name = Enum.GetName(typeof(AggregateFunction), type).ToUpperInvariant();
                    throw new ArgumentException("No column name specified for the aggregate '{name}'", "type");
                }
                else
                {
                    // Just wildcard the name
                    column = "*";
                }
            }
            else if (type != AggregateFunction.Count && column.Equals("*"))
            {
                string name = Enum.GetName(typeof(AggregateFunction), type).ToUpperInvariant();
                throw new ArgumentException($"Cannot use a wildcard in place of an column name for the aggregate '{name}'", "type");
            }

            // Get column key and Add item to list
            string key = String.Format(ColumnSelector.GetAggregateString(type), column);
            SelectedItems.Values[SelectedItems.Count - 1][key] = new ColumnSelector(column, alias, true)
            {
                Aggregate = type
            };
            return this;
        }

        /// <summary>
        /// The count(X) function returns a count of the number of times that X is not NULL in a group. 
        /// The count(*) function (with no arguments) returns the total number of rows in the group.
        /// </summary>
        /// <param name="columnName">The column name to perform the aggregate on</param>
        /// <param name="alias">The return alias of the aggregate result, if any.</param>
        public SelectQueryBuilder SelectCount(string columnName = "*", string alias = null)
            => Aggregate(columnName, alias, AggregateFunction.Count);

        /// <summary>
        /// The count(distinct X) function returns the number of distinct values of column X instead of the 
        /// total number of non-null values in column X.
        /// </summary>
        /// <param name="columnName">The column name to perform the aggregate on</param>
        /// <param name="alias">The return alias of the aggregate result, if any.</param>
        public SelectQueryBuilder SelectDistinctCount(string columnName, string alias = null)
            => Aggregate(columnName, alias, AggregateFunction.DistinctCount);

        /// <summary>
        /// The avg() function returns the average value of all non-NULL X within a group
        /// </summary>
        /// <param name="columnName">The column name to perform the aggregate on</param>
        /// <param name="alias">The return alias of the aggregate result, if any.</param>
        public SelectQueryBuilder SelectAverage(string columnName, string alias = null)
            => Aggregate(columnName, alias, AggregateFunction.Average);

        /// <summary>
        /// The min() aggregate function returns the minimum non-NULL value of all values in the group
        /// </summary>
        /// <param name="columnName">The column name to perform the aggregate on</param>
        /// <param name="alias">The return alias of the aggregate result, if any.</param>
        public SelectQueryBuilder SelectMin(string columnName, string alias = null)
            => Aggregate(columnName, alias, AggregateFunction.Min);

        /// <summary>
        /// The max() aggregate function returns the maximum value of all values in the group
        /// </summary>
        /// <param name="columnName">The column name to perform the aggregate on</param>
        /// <param name="alias">The return alias of the aggregate result, if any.</param>
        public SelectQueryBuilder SelectMax(string columnName, string alias = null)
            => Aggregate(columnName, alias, AggregateFunction.Max);

        /// <summary>
        /// The sum() aggregate functions return sum of all non-NULL values in the group.
        /// </summary>
        /// <param name="columnName">The column name to perform the aggregate on</param>
        /// <param name="alias">The return alias of the aggregate result, if any.</param>
        public SelectQueryBuilder SelectSum(string columnName, string alias = null)
            => Aggregate(columnName, alias, AggregateFunction.Sum);

        #endregion

        #region From Table


        /// <summary>
        /// Sets the table name to be used in this SQL Statement
        /// </summary>
        /// <param name="table">The table name</param>
        public SelectQueryBuilder From(string table, string alias = null)
        {
            // Ensure we are not null
            if (String.IsNullOrWhiteSpace(table))
                throw new ArgumentNullException("Tablename cannot be null or empty!", "table");

            // Ensure created with main table index
            if (SelectedItems.Count == 0)
                SelectedItems.Add(table, new SortedList<string, ColumnSelector>());
            else
                SelectedItems.Keys[0] = table;

            // Add alias
            TableAliases[table] = alias;
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
                foreach (ColumnSelector col in cols.Values)
                    col.Escape = false;
            }

            return this;
        }

        #endregion Alias

        #region Joins

        /// <summary>
        /// Creates a new Inner Join clause statement fot the current query object
        /// </summary>
        /// <param name="joinTable">The Joining Table name</param>
        public JoinClause InnerJoin(string joinTable)
        {
            // Add clause to list
            var clause = new JoinClause(this, JoinType.InnerJoin, joinTable);
            Joins.Add(clause);
            SelectedItems.Add(joinTable, new SortedList<string, ColumnSelector>());
            return clause;
        }

        /// <summary>
        /// Creates a new Cross Join clause statement fot the current query object
        /// </summary>
        /// <param name="joinTable">The Joining Table name</param>
        public JoinClause CrossJoin(string joinTable)
        {
            // Add clause to list
            var clause = new JoinClause(this, JoinType.CrossJoin, joinTable);
            Joins.Add(clause);
            SelectedItems.Add(joinTable, new SortedList<string, ColumnSelector>());
            return clause;
        }

        /// <summary>
        /// Creates a new Outer Join clause statement fot the current query object
        /// </summary>
        /// <param name="joinTable">The Joining Table name</param>
        public JoinClause OuterJoin(string joinTable)
        {
            // Add clause to list
            var clause = new JoinClause(this, JoinType.OuterJoin, joinTable);
            Joins.Add(clause);
            SelectedItems.Add(joinTable, new SortedList<string, ColumnSelector>());
            return clause;
        }

        /// <summary>
        /// Creates a new Left Join clause statement fot the current query object
        /// </summary>
        /// <param name="joinTable">The Joining Table name</param>
        public JoinClause LeftJoin(string joinTable)
        {
            // Add clause to list
            var clause = new JoinClause(this, JoinType.LeftJoin, joinTable);
            Joins.Add(clause);
            SelectedItems.Add(joinTable, new SortedList<string, ColumnSelector>());
            return clause;
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
        public SelectWhereStatement Where(string field, Comparison @operator, object compareValue)
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
        public SqlExpression<SelectWhereStatement> Where(string field)
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
        /// <param name="clause"></param>
        public SelectQueryBuilder OrderBy(OrderByClause clause)
        {
            OrderByStatements.Add(clause);
            return this;
        }

        /// <summary>
        /// Creates and adds a new Oderby clause to the current query object
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="order"></param>
        public SelectQueryBuilder OrderBy(string fieldName, Sorting order)
        {
            OrderByStatements.Add(new OrderByClause(fieldName, order));
            return this;
        }


        #endregion Orderby

        #region GroupBy

        /// <summary>
        /// Creates and adds a new Groupby clause to the current query object
        /// </summary>
        /// <param name="fieldName"></param>
        public SelectQueryBuilder GroupBy(string fieldName)
        {
            GroupByColumns.Add(fieldName);
            return this;
        }

        #endregion

        #region Having

        public SelectWhereStatement Having(string field, Comparison @operator, object compareValue)
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

        #region Limit / Offset

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

        #endregion

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
            // Define local variables
            int tableId = 1;
            int tableCount = SelectedItems.Count;

            // Make sure we have a table name
            if (SelectedItems.Count == 0 || String.IsNullOrWhiteSpace(SelectedItems.Keys[0]))
                throw new Exception("No tables were specified for this query.");

            // Start Query
            StringBuilder query = new StringBuilder("SELECT ", 256);
            query.AppendIf(Distinct, "DISTINCT ");

            // Append columns from each table
            foreach (var table in SelectedItems)
            {
                // Define local variables
                int colCount = table.Value.Count;
                string tableName = table.Key;
                string tableAlias = $"t{tableId++}";
                tableCount--;

                // Create alias for this table if there is none
                if (TableAliases.ContainsKey(tableName) && !String.IsNullOrWhiteSpace(TableAliases[tableName]))
                    tableAlias = TableAliases[table.Key];
                else
                    TableAliases[tableName] = tableAlias;

                // Check if the user wants to select all columns
                if (colCount == 0)
                {
                    query.AppendFormat("{0}.*", Context.QuoteIdentifier(tableAlias));
                    query.AppendIf(tableCount > 0, ", ");
                }
                else
                {
                    // Add each result selector to the query
                    foreach (ColumnSelector column in table.Value.Values)
                    {
                        // Use the internal method to append the column string to our query
                        column.AppendToQuery(query, Context, tableAlias);

                        // If we have more results to select, append Comma
                        query.AppendIf(--colCount > 0 || tableCount > 0, ", ");
                    }
                }
            }

            // === Append main Table === //
            query.Append($" FROM {Context.QuoteIdentifier(Table)} AS {Context.QuoteIdentifier(TableAliases[Table])}");

            // Append Joined tables
            if (Joins.Count > 0)
            {
                foreach (JoinClause clause in Joins)
                {
                    // Convert join type to string
                    switch (clause.JoinType)
                    {
                        default:
                        case JoinType.InnerJoin:
                            query.Append(" JOIN ");
                            break;
                        case JoinType.OuterJoin:
                            query.Append(" OUTER JOIN ");
                            break;
                        case JoinType.CrossJoin:
                            query.Append(" CROSS JOIN ");
                            break;
                        case JoinType.LeftJoin:
                            query.Append(" LEFT JOIN ");
                            break;
                    }

                    // Append the join statement
                    string alias = Context.QuoteIdentifier(TableAliases[clause.JoiningTable]);
                    query.Append($"{Context.QuoteIdentifier(clause.JoiningTable)} AS {alias}");

                    // Do we have an expression?
                    if (clause.ExpressionType == JoinExpressionType.On)
                    {
                        string fromT = TableAliases.ContainsKey(clause.FromTable) ? TableAliases[clause.FromTable] : clause.FromTable;
                        query.Append(" ON ");
                        query.Append(
                            SqlExpression<WhereStatement>.CreateExpressionString(
                                $"{alias}.{Context.QuoteIdentifier(clause.JoiningColumn)}",
                                clause.ComparisonOperator,
                                new SqlLiteral(Context.QuoteIdentifier($"{fromT}.{clause.FromColumn}"))
                            )
                        );
                    }
                    else if (clause.ExpressionType == JoinExpressionType.Using)
                    {
                        var parts = clause.JoiningColumn.Split(',');
                        query.AppendFormat(" USING({0})", String.Join(", ", parts.Select(x => Context.QuoteIdentifier(x))));
                    }
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
                query.Append(" GROUP BY " + String.Join(", ", GroupByColumns.Select(x => Context.QuoteIdentifier(x))));

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
                    query.Append($" {Context.QuoteIdentifier(clause.FieldName)}");

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
        /// Verifies that all SQL queries associated with the query builder can be
        /// successfully compiled. A <see cref="SQLiteException"/> will be raised if
        /// any errors occur.
        /// </summary>
        /// <remarks>
        /// This method builds a command and uses the already made VerifyOnly method.
        /// If you plan to also execute the query, might as well call BuildCommand()
        /// and use the VerifyOnly() method on the command itself.
        /// </remarks>
        public void VerifyQuery() => BuildCommand().VerifyOnly();

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
