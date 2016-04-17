using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossLite
{
    /// <summary>
    /// Represents an Entity property to attribute relationship. Only used
    /// in CodeFirst table creation <see cref="SQLiteContext.CreateTable{TEntity}(bool)"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        /// <summary>
        /// Gets the attribute (column) name in the database
        /// </summary>
        public string Name { get; set; }

        public ColumnAttribute(string Name = null)
        {
            this.Name = Name;
        }
    }
}
