using System;

namespace CrossLite.CodeFirst
{
    /// <summary>
    /// Indicates that Property/Attribute Requires a value and
    /// cannot be NULL during Entity insertion into the database
    /// </summary>
    /// <remarks>
    /// This is automatically enforced on every column of the 
    /// PRIMARY KEY in a WITHOUT ROWID table.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public class RequiredAttribute : Attribute
    {
    }
}
