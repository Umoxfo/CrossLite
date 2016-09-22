using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace CrossLite.QueryBuilder
{
    /// <summary>
    /// Represents a WHERE statement inside an SQL query
    /// </summary>
    public class WhereStatement
    {
        /// <summary>
        /// Gets the current Clause group in this Statement
        /// </summary>
        public WhereClause CurrentClause { get; protected set; }

        /// <summary>
        /// Gets a list of all Where Clauses in this statement
        /// </summary>
        public List<WhereClause> Clauses { get; protected set; }

        /// <summary>
        /// Gets or Sets the Logic Operator to use inside Clauses.
        /// Default is AND
        /// </summary>
        public LogicOperator InnerClauseOperator { get; set; } = LogicOperator.And;

        /// <summary>
        /// Gets or Sets the Logic Operator to use between Where Clauses
        /// </summary>
        public LogicOperator OutterClauseOperator { get; set; } = LogicOperator.Or;

        /// <summary>
        /// Indicates whether this WhereStatement has any clauses, or if its empty.
        /// </summary>
        public bool HasClause => Clauses.Any(x => x.Expressions.Count > 0);

        /// <summary>
        /// Creates a new instance of <see cref="WhereStatement"/>
        /// </summary>
        public WhereStatement()
        {
            CurrentClause = new WhereClause();
            Clauses = new List<WhereClause>() { CurrentClause };
        }

        /// <summary>
        /// Ends the current active clause, and creates a new one.
        /// </summary>
        public void CreateNewClause()
        {
            // Create new Group
            if (CurrentClause.Expressions.Count > 0)
            {
                CurrentClause = new WhereClause();
                Clauses.Add(CurrentClause);
            }
        }

        /// <summary>
        /// Appends a new expression evaluation to the current Statement
        /// </summary>
        /// <param name="fieldName">The attribute name we are performing the evaluation on</param>
        /// <param name="operator">The Comparison we are performing on this attribute</param>
        /// <param name="value">The value at which we require in this evaluation</param>
        /// <param name="literal">If true, than the value will not be escaped and quoted during the query</param>
        /// <returns>Returns this object to allow method chaining</returns>
        public WhereStatement And(string fieldName, Comparison @operator, object value, bool literal = false)
        {
            // Create new Group
            if (InnerClauseOperator == LogicOperator.Or && HasClause)
                this.CreateNewClause();

            // Convert value
            if (literal)
                CurrentClause.Add(fieldName, @operator, new SqlLiteral(value.ToString()));
            else
                CurrentClause.Add(fieldName, @operator, value);

            // Allow chaining
            return this;
        }

        /// <summary>
        /// Appends a new expression evaluation to the current Statement
        /// </summary>
        /// <param name="fieldName">The attribute name we are performing the evaluation on</param>
        /// <param name="operator">The Comparison we are performing on this attribute</param>
        /// <param name="value">The value at which we require in this evaluation</param>
        /// <param name="literal">If true, than the value will not be escaped and quoted during the query</param>
        /// <returns>Returns this object to allow method chaining</returns>
        public WhereStatement Or(string fieldName, Comparison @operator, object value, bool literal = false)
        {
            // Create new Group
            if (InnerClauseOperator == LogicOperator.And && HasClause)
                this.CreateNewClause();

            // Add expression
            CurrentClause.Add(fieldName, @operator, value);
            return this;
        }

        /// <summary>
        /// Builds the current set of Clauses and returns the output as a string.
        /// </summary>
        /// <param name="context">An open SQLiteContext to create parameters from</param>
        /// <param name="parameters">A list of current query parameters</param>
        /// <returns></returns>
        public string BuildStatement(SQLiteContext context, out List<SQLiteParameter> parameters)
        {
            parameters = new List<SQLiteParameter>();
            return BuildStatement(context, parameters);
        }

        /// <summary>
        /// Builds the current set of Clauses and returns the output as a string.
        /// </summary>
        /// <param name="context">An open SQLiteContext to create parameters from</param>
        /// <param name="parameters">A list of current query parameters</param>
        /// <returns></returns>
        public string BuildStatement(SQLiteContext context, List<SQLiteParameter> parameters)
        {
            StringBuilder builder = new StringBuilder();
            int paramsCounter = parameters.Count;
            int counter = 0;

            // Ignore empty expressions
            Clauses.RemoveAll(x => x.Expressions.Count == 0);

            // Loop through each Where clause (wrapped in parenthesis)
            foreach (WhereClause clause in Clauses)
            {
                // Open Parent Clause grouping if we have more then 1 SubClause
                int subCounter = 0;
                builder.AppendIf(clause.Expressions.Count > 1, "(");

                // Append each Sub Clause
                foreach (SqlExpression expression in clause.Expressions)
                {
                    // If we have more sub clauses in this group, append operator
                    builder.AppendIf(++subCounter > 1, (InnerClauseOperator == LogicOperator.Or) ? " OR " : " AND ");

                    // If using a command, Convert values to Parameters for SQL safety
                    if (expression.Value != null && expression.Value != DBNull.Value && !(expression.Value is SqlLiteral))
                    {
                        // --------------------------------------
                        // BETWEEN and NOT BETWEEN
                        //--------------------------------------
                        if (expression.ComparisonOperator == Comparison.Between || expression.ComparisonOperator == Comparison.NotBetween)
                        {
                            // Add the between values to the command parameters
                            object[] between = ((object[])expression.Value);

                            SQLiteParameter param1 = context.CreateParameter();
                            param1.ParameterName = "@P" + parameters.Count;
                            param1.Value = between[0].ToString();

                            SQLiteParameter param2 = context.CreateParameter();
                            param2.ParameterName = "@P" + (parameters.Count + 1);
                            param2.Value = between[1].ToString();

                            // Add Params to command
                            parameters.Add(param1);
                            parameters.Add(param2);

                            // Add statement
                            builder.Append(
                                CreateComparisonClause(
                                    expression.FieldName, 
                                    expression.ComparisonOperator, 
                                    (object)new object[2]
                                    {
                                        (object) new SqlLiteral(param1.ParameterName),
                                        (object) new SqlLiteral(param2.ParameterName)
                                    }
                                )
                             );
                        }

                        // --------------------------------------
                        // All Other Clauses
                        //--------------------------------------
                        else
                        {
                            // Create param for value
                            SQLiteParameter param = context.CreateParameter();
                            param.ParameterName = "@P" + parameters.Count;
                            param.Value = expression.Value;

                            // Add Params to command
                            parameters.Add(param);

                            // Add statement
                            builder.Append(
                                CreateComparisonClause(
                                    expression.FieldName,
                                    expression.ComparisonOperator,
                                    new SqlLiteral(param.ParameterName)
                                )
                            );
                        }
                    }
                    else // Null values
                    {
                        builder.Append(CreateComparisonClause(expression.FieldName, expression.ComparisonOperator, expression.Value));
                    }
                }

                // Close Parent Clause grouping
                builder.AppendIf(clause.Expressions.Count > 1, ")");

                // If we have more clauses, append operator
                builder.AppendIf(++counter < Clauses.Count, (OutterClauseOperator == LogicOperator.Or) ? " OR " : " AND ");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Formats, using the correct Comparaison Operator, The clause to SQL. The fieldName
        /// will be escaped automatically.
        /// </summary>
        /// <param name="fieldName">The Clause Column name</param>
        /// <param name="comparison">The Comparison Operator</param>
        /// <param name="value">The Value object</param>
        /// <returns>Clause formatted to SQL</returns>
        public static string CreateComparisonClause(string fieldName, Comparison comparison, object value)
        {
            // Correct
            fieldName = SQLiteContext.Escape(fieldName);

            // Only 2 options for null values
            if (value == null || value == DBNull.Value)
            {
                switch (comparison)
                {
                    case Comparison.Equals:
                        return $"{fieldName} IS NULL";
                    case Comparison.NotEqualTo:
                        return $"NOT {fieldName} IS NULL";
                }
            }
            else
            {
                switch (comparison)
                {
                    case Comparison.Equals:
                        return $"{fieldName} = {FormatSQLValue(value)}";
                    case Comparison.NotEqualTo:
                        return $"{fieldName} <> {FormatSQLValue(value)}";
                    case Comparison.Like:
                        return $"{fieldName} LIKE {FormatSQLValue(value)}";
                    case Comparison.NotLike:
                        return $"NOT {fieldName} LIKE {FormatSQLValue(value)}";
                    case Comparison.GreaterThan:
                        return $"{fieldName} > {FormatSQLValue(value)}";
                    case Comparison.GreaterOrEquals:
                        return $"{fieldName} >= {FormatSQLValue(value)}";
                    case Comparison.LessThan:
                        return $"{fieldName} < {FormatSQLValue(value)}";
                    case Comparison.LessOrEquals:
                        return $"{fieldName} <= {FormatSQLValue(value)}";
                    case Comparison.In:
                    case Comparison.NotIn:
                        string str1 = (comparison == Comparison.NotIn) ? "NOT " : "";
                        if (value is Array)
                        {
                            Array array = (Array)value;
                            string str2 = str1 + fieldName + " IN (";
                            foreach (object someValue in array)
                                str2 = str2 + FormatSQLValue(someValue) + ",";
                            return str2.TrimEnd(new char[] { ',' }) + ")";
                        }
                        else if (value is string)
                            return str1 + fieldName + " IN (" + value.ToString() + ")";
                        else
                            return str1 + fieldName + " IN (" + FormatSQLValue(value) + ")";
                    case Comparison.Between:
                    case Comparison.NotBetween:
                        object[] objArray = (object[])value;
                        return String.Format(
                            "{0}{1} BETWEEN {2} AND {3}",
                            ((comparison == Comparison.NotBetween) ? "NOT " : ""),
                            fieldName,
                            FormatSQLValue(objArray[0]),
                            FormatSQLValue(objArray[1])
                        );
                }
            }

            return "";
        }

        /// <summary>
        /// Formats and escapes a Value object, to the proper SQL format.
        /// </summary>
        /// <param name="someValue"></param>
        /// <returns></returns>
        public static string FormatSQLValue(object someValue)
        {
            if (someValue == null)
                return "NULL";

            switch (someValue.GetType().Name)
            {
                case "String": return $"'{((string)someValue).Replace("'", "''")}'";
                case "DateTime": return $"'{((DateTime)someValue).ToString("yyyy/MM/dd HH:mm:ss")}'";
                case "DBNull": return "NULL";
                case "Boolean": return (bool)someValue ? "1" : "0";
                case "Guid": return $"'{((Guid)someValue).ToString()}'";
                case "SqlLiteral": return ((SqlLiteral)someValue).Value;
                case "SelectQueryBuilder":
                    throw new ArgumentException("Using SelectQueryBuilder in another Querybuilder statement is unsupported!", "someValue");
                default: return someValue.ToString();
            }
        }
    }
}
