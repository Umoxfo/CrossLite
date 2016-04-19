using System;

namespace CrossLite.CodeFirst
{
    /// <summary>
    /// Represents an Auto Increment attribute. Only used in CodeFirst table 
    /// creation <see cref="SQLiteContext.CreateTable{TEntity}(bool)"/>
    /// </summary>
    /// <remarks>
    /// AUTOINCREMENT is to prevent the reuse of ROWIDs from previously deleted rows.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public class AutoIncrementAttribute : Attribute
    {
        public AutoIncrementAttribute()
        {

        }
    }
}
