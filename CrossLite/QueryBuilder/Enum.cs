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
        InnerJoin,
        OuterJoin,
        LeftJoin,
        RightJoin,
    }

    public enum UnionType
    {
        Union,
        UnionAll,
        Except,
        Intersect
    }
}
