using CrossLite;

namespace CrossLiteTester
{
    public class Privilege
    {
        [PrimaryKey]
        [Column(Name: "id")]
        public int Id { get; set; }

        [Column("name"), NotNull, Unique]
        public string Name { get; set; }   
    }
}
