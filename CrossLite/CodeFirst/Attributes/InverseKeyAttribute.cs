using System;

namespace CrossLite.CodeFirst
{
    /// <summary>
    /// This Attribute is used on a Foreign key when the Parent
    /// and Child's attribute names do not match. Only used in CodeFirst table 
    /// creation: <see cref="SQLiteContext.CreateTable{TEntity}(bool)"/>
    /// </summary>
    public class InverseKeyAttribute : Attribute
    {
        public string[] Attributes { get; protected set; }

        /// <summary>
        /// Creates a new instance of <see cref="InverseKeyAttribute"/>
        /// </summary>
        /// <param name="attributes">The Parent Entity column name(s) in the parent table.</param>
        public InverseKeyAttribute(params string[]  attributes)
        {
            this.Attributes = attributes;
        }
    }
}
