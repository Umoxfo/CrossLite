using CrossLite;

namespace CrossLiteTester
{
    [Table("test")]
    public class Account
    {
        [Column, PrimaryKey, AutoIncrement]
        public int Id { get; protected set; }

        [Column, NotNull]
        public string Name { get; set; }
    }
}
