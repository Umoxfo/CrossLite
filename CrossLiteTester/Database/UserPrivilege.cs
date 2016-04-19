using CrossLite;
using CrossLite.CodeFirst;

namespace CrossLiteTester
{
    public class UserPrivilege
    {
        [Column("pid"), PrimaryKey]
        public int PrivilegeId { get; set; }

        [Column("uid"), PrimaryKey]
        public int UserId { get; set; }

        [Column("has_privilege")]
        public bool HasPrivilege { get; set; }

        /// <summary>
        /// Using "Fetch()" on this lazy loading class will retrieve
        /// the Account object that this UserPriv references
        /// </summary>
        [InverseKey("Id")]
        [ForeignKey("uid", OnDelete = ReferentialIntegrity.Cascade)]
        public virtual ForeignKey<Account> Account { get; set; }

        /// <summary>
        /// Using "Fetch()" on this lazy loading class will retrieve
        /// the Privilege object that this UserPriv references
        /// </summary>
        [InverseKey("id")]
        [ForeignKey("pid", OnDelete = ReferentialIntegrity.Cascade)]
        public virtual ForeignKey<Privilege> Privilege { get; set; }
    }
}
