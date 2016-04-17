using CrossLite;

namespace CrossLiteTester
{
    public class UserPrivilege
    {
        [Column("pid"), PrimaryKey]
        [ForeignKey(typeof(Privilege), "id", OnDelete: ReferentialIntegrity.Cascade)]
        public int PrivilegeId { get; set; }

        [Column("uid"), PrimaryKey]
        [ForeignKey(typeof(Account), "Id", OnDelete: ReferentialIntegrity.Cascade)]
        public int UserId { get; set; }

        [Column("has_privilege"), Default(0)]
        public bool HasPrivilege { get; set; }
    }
}
