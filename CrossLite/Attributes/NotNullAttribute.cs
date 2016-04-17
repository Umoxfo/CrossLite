using System;

namespace CrossLite
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// NOT NULL is enforced on every column of the PRIMARY KEY 
    /// in a WITHOUT ROWID table.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public class NotNullAttribute : Attribute
    {
    }
}
