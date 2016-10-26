using System;
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
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            // Delete old test database
            string database = Path.Combine(Application.StartupPath, "test.db");

            // Connect to the database
            var builder = new SQLiteConnectionStringBuilder() { DataSource = database, ForeignKeys = true };
            using (TestContext db = new TestContext(builder.ToString()))
            using (var trans = db.BeginTransaction())
            {
                int i = 1;

                // Run test 1
                RunQueryBuilderTest(db);

                Console.WriteLine();
                Console.Write("Droping Tables...");

                // Drop tables
                db.DropTable<UserPrivilege>();
                db.DropTable<Privilege>();
                db.DropTable<Account>();

                Console.WriteLine("Success!");
                Console.WriteLine();
                Console.Write("Adding Tables to Database...");

                // Create new tables
                db.CreateTable<Account>();
                db.CreateTable<Privilege>();
                db.CreateTable<UserPrivilege>();

                Console.WriteLine("Success!");
                Console.WriteLine();
                Console.Write("Adding dummy data...");

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

                Console.WriteLine("Success!");
                Console.WriteLine();
                Console.Write("Testing readers...");

                // Test fetching for Fkeys
                foreach (UserPrivilege priv in entity.Privilages)
                {
                    var temp = priv.Privilege.Fetch();
                }

                // Check if entity inserted correctly
                if (!db.Users.Contains(entity))
                    Console.WriteLine("Failed!!! " + entity.Name + " does not exist!");

                // More dummy data
                try
                {
                    entity = new Account() { Name = "Sally" };
                    db.Users.Add(entity);
                    var result1 = db.Users.Select(x => x).Where(x => x.Id == 2).ToList();

                    // Query builder testing
                    var query = new SelectQueryBuilder(db);
                    query.From("test").SelectAll().Where("Id").Between(1, 2);
                    var result2 = query.ExecuteQuery<Account>().ToList();

                    Console.WriteLine($"Success!");
                    Console.WriteLine();
                    Console.WriteLine("Account ID #1 is: " + result2[0].Name);
                    Console.WriteLine("Account ID #2 is: " + result1[0].Name);
                    Console.WriteLine();
                    Console.Write("Fetching data count...");

                    query = new SelectQueryBuilder(db);
                    int num = query.From("test").SelectCount().ExecuteScalar<int>();
                    Console.WriteLine("Success!");
                    Console.WriteLine($"There are {num} Records!");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed!!!");
                    Console.WriteLine(e.Message);
                    var exp = e;
                }

                // Test an update
                entity.Name = "Joey";
                db.Users.Update(entity);

                // Test read
                entity = db.Users.Where(x => x.Name == "Joey").First();

                // Delete test
                db.Users.Remove(entity);
                trans.Commit();
                entity = (from x in db.Users select x).First();

                // Pause
                Console.Read();
            }
        }

        private static void RunQueryBuilderTest(CrossLite.SQLiteContext context)
        {
            // Quote keywords only with accents
            context.IdentifierQuoteKind = IdentifierQuoteKind.Accents;
            context.IdentifierQuoteMode = IdentifierQuoteMode.KeywordsOnly;

            // Start logging times
            Stopwatch timer = Stopwatch.StartNew();

            // Simple query test, using a reserved word for quote testing
            var query = new SelectQueryBuilder(context);
            query.From("table1")
                .Select("col1", "col2", "plan")
                // Inner join a table
                .InnerJoin("table2").As("t2").On("col22").Equals("table1", "col1")
                .Select("col21", "col22")
                // Cross Join another!
                .CrossJoin("table3").As("t3").Using("col1")
                .SelectCount()
                // Finally, a where clause
                .Where("col1").Equals(AccountType.Admin).And("col22").GreaterThan(6).Or("plan").NotEqualTo(3);
            var queryString = query.BuildQuery();

            // Log query builder time (14ms for me on an i7-950)
            long time = timer.ElapsedMilliseconds;

            Console.WriteLine("Query Builder Test:");
            Console.WriteLine();
            Console.WriteLine(queryString);
            Console.WriteLine($"Query generated in {time}ms");
        }
    }
}
