namespace CrossLite.QueryBuilder
{
    public class UnionStatement
    {
        public UnionType Type { get; set; }

        public SelectQueryBuilder Query { get; set; }
    }
}
