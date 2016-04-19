using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CrossLite;
using System.Data.SQLite;
using System.IO;
using System.Diagnostics;

namespace CrossLiteTester
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // Delete old test database
            string database = Path.Combine(Application.StartupPath, "test.db");

            // Connect to the database
            var builder = new SQLiteConnectionStringBuilder() { DataSource = database };
            using (TestContext db = new TestContext(builder.ToString()))
            using (var trans = db.BeginTransaction())
            {
                // Create test table
                Stopwatch timer = Stopwatch.StartNew();

                // Drop tables
                db.DropTable<Account>();
                db.DropTable<Privilege>();
                db.DropTable<UserPrivilege>();

                // Create new tables
                db.CreateTable<Account>();
                db.CreateTable<Privilege>();
                db.CreateTable<UserPrivilege>();

                // Insert some dummy data
                Account entity = new Account() { Name = "Steve" };
                db.Users.Add(entity);

                Privilege ent = new Privilege() { Name = "Test" };
                db.Privs.Add(ent);

                UserPrivilege up = new UserPrivilege()
                {
                    PrivilegeId = ent.Id,
                    UserId = entity.Id
                };
                db.UserPrivileges.Add(up);

                foreach (UserPrivilege priv in entity.Privilages)
                {
                    var temp = priv.Privilege.Fetch();
                }

                // Check if entity inserted correctly
                if (!db.Users.Contains(entity))
                    MessageBox.Show(entity.Name + " does not exist!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                // More dummy data
                entity = new Account() { Name = "Sally" };
                db.Users.Add(entity);
                entity = db.Users.LastOrDefault();

                // Test an update
                entity.Name = "Joey";
                db.Users.Update(entity);

                // Test read
                entity = db.Users.Where(x => x.Name == "Joey").First();

                // Delete test
                db.Users.Remove(entity);
                trans.Commit();
                entity = (from x in db.Users select x).First();
                MessageBox.Show(entity.Name + " :: " + timer.ElapsedMilliseconds);
            }
        }
    }
}
