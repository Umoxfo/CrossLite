using System;
using System.Collections.Generic;

namespace CrossLite.QueryBuilder
{
    /// <summary>
    /// Represents a group of Where Clauses that will grouped together and 
    /// wrapped in parenthesis
    /// </summary>
    class WhereClause : List<WhereClause.SubClause>, ICloneable
    {
        /// <summary>
        /// The parent field name for this clause
        /// </summary>
        protected string FieldName;

        /// <summary>
        /// Constructor. Adds the initial Where Sub Clause
        /// </summary>
        /// <param name="FieldName">The column name</param>
        /// <param name="Operator">The Comaparison Operator to use</param>
        /// <param name="Value">The value, for the column name and comparison operator</param>
        public WhereClause(string FieldName, Comparison @Operator, object Value)
        {
            this.FieldName = FieldName;
            this.Add(new SubClause(null, this.FieldName, @Operator, Value));
        }

        /// <summary>
        /// Adds a new clause to the current where clause.
        /// </summary>
        /// <param name="Logic">The Logic operator, that seperates this from the previos clause</param>
        /// <param name="FieldName">The column name</param>
        /// <param name="Operator">The Comaparison Operator to use</param>
        /// <param name="Value">The value, for the column name and comparison operator</param>
        public void AddClause(LogicOperator Logic, string FieldName, Comparison @Operator, object Value)
        {
            this.Add(new SubClause(Logic, FieldName, @Operator, Value));
        }

        /// <summary>
        /// Adds a new clause to the current where clause. The parent field name will be used
        /// </summary>
        /// <param name="Logic"></param>
        /// <param name="Operator">The Comaparison Operator to use</param>
        /// <param name="Value">The value, for the column name and comparison operator</param>
        public void AddClause(LogicOperator Logic, Comparison @Operator, object Value)
        {
            this.Add(new SubClause(Logic, this.FieldName, @Operator, Value));
        }

        /// <summary>
        /// Returns a clone of this clause
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return this.MemberwiseClone();
        }
        
        /// <summary>
        /// An actual clause statement that converts to a Key = Value string
        /// </summary>
        internal struct SubClause : ICloneable
        {
            /// <summary>
            /// The column name for this clause
            /// </summary>
            public string FieldName;

            /// <summary>
            /// The Logic operator, that seperates this from the previos clause
            /// </summary>
            public LogicOperator LogicOperator;

            /// <summary>
            /// The Comaparison Operator to use
            /// </summary>
            public Comparison ComparisonOperator;

            /// <summary>
            /// The Value object
            /// </summary>
            public object Value;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="Logic">The Logic operator, that seperates this from the previos clause</param>
            /// <param name="FieldName">The column name</param>
            /// <param name="Operator">The Comaparison Operator to use</param>
            /// <param name="Value">The value, for the column name and comparison operator</param>
            public SubClause(LogicOperator? Logic, string FieldName, Comparison @Operator, object Value)
            {
                // Between values must be an array, with 2 values
                if (@Operator == Comparison.Between || @Operator == Comparison.NotBetween)
                {
                    if (!(Value is Array) || ((Array)Value).Length != 2)
                        throw new ArgumentException("The value of a Between clause must be an array, with 2 values.");
                }

                // Cant use NULL values for most operators
                else if ((Value == null || Value == DBNull.Value) && (@Operator != Comparison.Equals && @Operator != Comparison.NotEqualTo))
                    throw new Exception("Cannot use comparison operator " + ((object)@Operator).ToString() + " for NULL values.");

                // Set class vars
                this.LogicOperator = Logic ?? LogicOperator.And;
                this.FieldName = FieldName;
                this.ComparisonOperator = @Operator;
                this.Value = Value;
            }

            public object Clone()
            {
                return this.MemberwiseClone();
            }
        }
    }
}
