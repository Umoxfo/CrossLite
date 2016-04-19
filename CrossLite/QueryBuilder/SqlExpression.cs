using System;

namespace CrossLite.QueryBuilder
{
    public class SqlExpression
    {
        /// <summary>
        /// The column name for this clause
        /// </summary>
        public string FieldName;

        /// <summary>
        /// The Comaparison Operator to use
        /// </summary>
        public Comparison ComparisonOperator;

        /// <summary>
        /// The Value object
        /// </summary>
        public object Value;

        public SqlExpression(string fieldName, Comparison @operator, object value)
        {
            // Between values must be an array, with 2 values
            if (@operator == Comparison.Between || @operator == Comparison.NotBetween)
            {
                if (!(Value is Array) || ((Array)Value).Length != 2)
                    throw new ArgumentException("The value of a Between clause must be an array, with 2 values.");
            }

            // Cant use NULL values for most operators
            if ((Value == null || Value == DBNull.Value) && (@operator != Comparison.Equals && @operator != Comparison.NotEqualTo))
            {
                throw new Exception("Cannot use comparison operator " + ((object)@operator).ToString() + " for NULL values.");
            }

            this.FieldName = fieldName;
            this.ComparisonOperator = @operator;
            this.Value = value;
        }
    }
}