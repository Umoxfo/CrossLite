using CrossLite;
using CrossLite.CodeFirst;

namespace CrossLiteTester
{
    public class Privilege
    {
        [PrimaryKey]
        [Column(Name: "id")]
        public int Id { get; set; }

        [Column("name"), Required, Unique]
        public string Name { get; set; }   
    }
}
