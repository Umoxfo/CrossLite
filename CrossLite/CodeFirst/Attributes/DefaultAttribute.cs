using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossLite.CodeFirst
{
    /// <summary>
    /// Represents a default value for an Attribute. Only used in CodeFirst 
    /// table creation: <see cref="SQLiteContext.CreateTable{TEntity}(bool)"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DefaultAttribute : Attribute
    {
        /// <summary>
        /// Gets or Sets the default value, if any
        /// </summary>
        public object Value { get; set; }

        public DefaultAttribute(object Value)
        {
            this.Value = Value;
        }
    }
}
