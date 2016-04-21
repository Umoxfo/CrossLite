using System;

namespace CrossLite.CodeFirst
{
    /// <summary>
    /// Only used in CodeFirst table creation: <see cref="SQLiteContext.CreateTable{TEntity}(bool)"/>.
    /// 
    /// The COLLATE clause of the column definition is used to define 
    /// alternative collating functions for a column.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CollationAttribute : Attribute
    {
        public Collation Collation { get; protected set; }

        public CollationAttribute(Collation collation)
        {
            this.Collation = collation;
        }
    }

    /// <summary>
    /// When SQLite compares two strings, it uses a collating sequence or 
    /// collating function (two words for the same thing) to determine 
    /// which string is greater or if the two strings are equal. SQLite has 
    /// three built-in collating functions: BINARY, NOCASE, and RTRIM.
    /// </summary>
    public enum Collation
    {
        Default,

        /// <summary>
        /// The same as binary, except the 26 upper case characters of ASCII are 
        /// folded to their lower case equivalents before the comparison is 
        /// performed
        /// </summary>
        NoCase,

        /// <summary>
        /// Compares string data using memcmp(), regardless of text encoding.
        /// </summary>
        Binary,

        /// <summary>
        /// The same as binary, except that trailing space characters are ignored.
        /// </summary>
        RTrim
    }
}
