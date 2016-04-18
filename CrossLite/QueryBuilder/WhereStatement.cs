using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace CrossLite.QueryBuilder
{
    /// <summary>
    /// Represents an entire WHERE statement inside an SQL query
    /// </summary>
    class WhereStatement : List<WhereClause>
    {
        /// <summary>
        /// Gets or Sets the Logic Operator to use between Where Clauses
        /// </summary>
        public LogicOperator StatementOperator = LogicOperator.Or;

        /// <summary>
        /// Adds a new Where clause to the current Where Statement
        /// </summary>
        /// <param name="fieldName">The Column name</param>
        /// <param name="Operator">The Comparison Operator to use</param>
        /// <param name="value">The Value object</param>
        /// <returns></returns>
        public WhereClause Add(string fieldName, Comparison @Operator, object value)
        {
            WhereClause Clause = new WhereClause(fieldName, @Operator, value);
            this.Add(Clause);
            return Clause;
        }

        /// <summary>
        /// Builds the Where statement to SQL format
        /// </summary>
        /// <param name="command">The command object to use, if any. Using a command makes 
        /// this statement SQL injection safe!</param>
        /// <returns></returns>
        public string BuildStatement(DbCommand command)
        {
            StringBuilder builder = new StringBuilder();
            int counter = 0;

            // Loop through each Where clause (wrapped in parenthesis)
            foreach (WhereClause parentClause in this)
            {
                // Open Parent Clause grouping if we have more then 1 SubClause
                if (parentClause.Count > 1)
                    builder.Append("(");

                // SubClause Counter
                int subCounter = 0;

                // Append each Sub Clause
                foreach (WhereClause.SubClause clause in parentClause)
                {
                    // If we have more sub clauses in this group, append operator
                    if (++subCounter > 1)
                        builder.Append((clause.LogicOperator == LogicOperator.Or) ? " OR " : " AND ");

                    // If using a command, Convert values to Parameters for SQL safety
                    if (command != null && clause.Value != null && clause.Value != DBNull.Value && !(clause.Value is SqlLiteral))
                    {
                        if (clause.ComparisonOperator == Comparison.Between || clause.ComparisonOperator == Comparison.NotBetween)
                        {
                            // Add the between values to the command parameters
                            object[] Between = ((object[])clause.Value);
                            DbParameter Param1 = command.CreateParameter();
                            Param1.ParameterName = "@P" + command.Parameters.Count;
                            Param1.Value = Between[0].ToString();
                            DbParameter Param2 = command.CreateParameter();
                            Param2.ParameterName = "@P" + (command.Parameters.Count + 1);
                            Param2.Value = Between[1].ToString();

                            // Add Params to command
                            command.Parameters.Add(Param1);
                            command.Parameters.Add(Param2);

                            // Add statement
                           builder.Append( 
                               CreateComparisonClause(clause.FieldName, clause.ComparisonOperator, (object) new object[2]
                               {
                                    (object) new SqlLiteral(Param1.ParameterName),
                                    (object) new SqlLiteral(Param2.ParameterName)
                               })
                            );
                        }
                        else
                        {
                            // Create param for value
                            DbParameter Param = command.CreateParameter();
                            Param.ParameterName = "@P" + command.Parameters.Count;
                            Param.Value = clause.Value;

                            // Add Params to command
                            command.Parameters.Add(Param);

                            // Add statement
                            builder.Append(
                                CreateComparisonClause(
                                    clause.FieldName, 
                                    clause.ComparisonOperator, 
                                    new SqlLiteral(Param.ParameterName)
                                )
                            );
                        }
                    }
                    else
                        builder.Append(CreateComparisonClause(clause.FieldName, clause.ComparisonOperator, clause.Value));
                }

                // Close Parent Clause grouping
                if (parentClause.Count > 1)
                    builder.Append(")");

                // If we have more clauses, append operator
                if (++counter < this.Count)
                    builder.Append( (StatementOperator == LogicOperator.Or) ? " OR " : " AND " );
            }

            return builder.ToString();
        }

        /// <summary>
        /// Formats, using the correct Comparaison Operator, The clause to SQL.
        /// </summary>
        /// <param name="fieldName">The Clause Column name</param>
        /// <param name="comparisonOperator">The Comparison Operator</param>
        /// <param name="value">The Value object</param>
        /// <returns>Clause formatted to SQL</returns>
        public static string CreateComparisonClause(string fieldName, Comparison comparisonOperator, object value)
        {
            // Correct
            fieldName = SQLiteContext.Escape(fieldName);

            // Only 2 options for null values
            if (value == null || value == DBNull.Value)
            {
                switch (comparisonOperator)
                {
                    case Comparison.Equals:
                        return $"{fieldName} IS NULL";
                    case Comparison.NotEqualTo:
                        return $"NOT {fieldName} IS NULL";
                }
            }
            else
            {
                switch (comparisonOperator)
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
                        string str1 = (comparisonOperator == Comparison.NotIn) ? "NOT " : "";
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
                            ((comparisonOperator == Comparison.NotBetween) ? "NOT " : ""), 
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
