using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossLite;

namespace CrossLiteTester
{
    public class TestContext : SQLiteContext
    {
        /// <summary>
        /// The "Users" table in our database
        /// </summary>
        public DbSet<Account> Users { get; set; }

        public TestContext(string conn) : base(conn)
        {
            // Connect to the SQLite database file
            base.Connect();

            // Setup our Database Sets
            this.Users = new DbSet<Account>(this);
        }
    }
}
