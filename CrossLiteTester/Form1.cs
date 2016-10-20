using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CrossLite;
using CrossLite.CodeFirst;
using CrossLite.QueryBuilder;

namespace CrossLiteTester
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // Delete old test database
            string database = Path.Combine(Application.StartupPath, "test.db");
            TestContext.PerformEscapeOnQuery = false;

            // Connect to the database
            var builder = new SQLiteConnectionStringBuilder() { DataSource = database, ForeignKeys = true };
            using (TestContext db = new TestContext(builder.ToString()))
            using (var trans = db.BeginTransaction())
            {
                // Simple query test
                var query = new SelectQueryBuilder(db);
                query.From("table1").Select("col1", "col2").Where("col1").Equals("Test").And("col2").GreaterThan(6).Or("col3").NotEqualTo(3);
                var q = query.BuildQuery();

                // Create test table
                Stopwatch timer = Stopwatch.StartNew();

                // Drop tables
                db.DropTable<UserPrivilege>();
                db.DropTable<Privilege>();
                db.DropTable<Account>();

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

                // Test fetching for Fkeys
                foreach (UserPrivilege priv in entity.Privilages)
                {
                    var temp = priv.Privilege.Fetch();
                }

                // Check if entity inserted correctly
                if (!db.Users.Contains(entity))
                    MessageBox.Show(entity.Name + " does not exist!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                // More dummy data
                try
                {
                    entity = new Account() { Name = "Sally" };
                    db.Users.Add(entity);
                    var results = db.Users.Select(x => x).Where(x => x.Id == 1).ToString();

                    // Query builder testing
                    query = new SelectQueryBuilder(db);
                    query.From("test").SelectAll().Where("Id").Between(1, 2);
                    var res = query.ExecuteQuery<Account>().ToList();
                }
                catch (Exception e)
                {
                    var exp = e;
                }

                /*
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
                */
            }
        }
    }
}
