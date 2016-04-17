using System;

namespace CrossLite
{
    /// <summary>
    /// Represents an Auto Increment attribute. Only used in CodeFirst table 
    /// creation <see cref="SQLiteContext.CreateTable{TEntity}(bool)"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AutoIncrementAttribute : Attribute
    {
        public AutoIncrementAttribute()
        {

        }
    }
}
