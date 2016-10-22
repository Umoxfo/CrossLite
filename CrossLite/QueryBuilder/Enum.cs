namespace CrossLite.QueryBuilder
{
    public enum ValueMode
    {
        Set,
        Add,
        Subtract,
        Divide,
        Multiply
    }

    public enum Comparison
    {
        Equals,
        NotEqualTo,
        LessThan,
        GreaterThan,
        LessOrEquals,
        GreaterOrEquals,
        Like,
        NotLike,
        In,
        NotIn,
        Between,
        NotBetween
    }

    public enum LogicOperator
    {
        And, 
        Or
    }

    public enum Sorting
    {
        Ascending,
        Descending,
    }

    public enum JoinType
    {
        /// <summary>
        /// A INNER JOIN creates a new result table by combining column values of two tables 
        /// (table1 and table2) based upon the join-predicate.
        /// </summary>
        InnerJoin,

        /// <summary>
        /// 
        /// </summary>
        OuterJoin,

        /// <summary>
        /// A CROSS JOIN matches every row of the first table with every row of the second table.
        /// </summary>
        CrossJoin,

        /// <summary>
        /// The Left Join or Left Outer Join operation takes two relations, A and B, and returns the inner join 
        /// of A and B along with the unmatched rows of A.
        /// </summary>
        LeftJoin,
    }

    internal enum JoinExpressionType
    {
        None,
        On,
        Using
    }

    public enum UnionType
    {
        Union,
        UnionAll,
        Except,
        Intersect
    }
}
