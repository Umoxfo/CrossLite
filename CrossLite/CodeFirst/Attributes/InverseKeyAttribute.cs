using System;

namespace CrossLite.CodeFirst
{
    public class InverseKeyAttribute : Attribute
    {
        public string[] Attributes { get; protected set; }

        public InverseKeyAttribute(params string[] attributes)
        {
            this.Attributes = attributes;
        }
    }
}
