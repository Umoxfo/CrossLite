namespace CrossLite.QueryBuilder
{
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
}
