using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossLite
{
    /// <summary>
    /// Represents a SQLite database table. Only used in CodeFirst table 
    /// creation <see cref="SQLiteContext.CreateTable{TEntity}(bool)"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        /// <summary>
        /// If specified, this is the table name this Entity
        /// represents
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or Sets whether the "WITHOUT ROWID" command is used
        /// when creating a table using Code First 
        /// (see <see cref="SQLiteContext.CreateTable{TEntity}(bool)"/>)
        /// </summary>
        public bool WithoutRowID { get; set; }

        /// <summary>
        /// Creates a new SQLite Entity to table relationship
        /// </summary>
        /// <param name="Name">If not null, this is the table name that this 
        /// Entity will represent</param>
        public TableAttribute(string Name = null, bool WithoutRowID = false)
        {
            this.Name = Name;
        }
    }
}
