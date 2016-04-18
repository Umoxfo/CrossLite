using CrossLite;

namespace CrossLiteTester
{
    [Table("test")]
    public class Account
    {
        [Column, PrimaryKey]
        public int Id { get; set; }

        [Column, NotNull, Collation(Collation.NoCase)]
        public string Name { get; set; }
    }
}
