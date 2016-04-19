using System;

namespace CrossLite.CodeFirst
{
    /// <summary>
    /// Represents a Primary or Composite Key constraint on the database. 
    /// Only used in CodeFirst table creation 
    /// <see cref="SQLiteContext.CreateTable{TEntity}(bool)"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryKeyAttribute : Attribute
    {
        public PrimaryKeyAttribute()
        {

        }
    }
}
