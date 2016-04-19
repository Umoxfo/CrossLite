using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CrossLite.CodeFirst
{
    public class ForeignKeyInfo
    {
        public Type ParentEntityType { get; protected set; }

        public Type ChildEntityType { get; protected set; }

        public InverseKeyAttribute InverseKey;

        public ForeignKeyAttribute ForeignKey;

        public ForeignKeyInfo(TableMapping child, Type parentType, ForeignKeyAttribute foreignKey, InverseKeyAttribute inverseKey)
        {
            this.ForeignKey = foreignKey;
            this.InverseKey = inverseKey;
            this.ParentEntityType = parentType;
            this.ChildEntityType = child.EntityType;

            // Ensure the parent and child have the specified properties
            TableMapping parent = EntityCache.GetTableMap(parentType);
            var invalid = inverseKey.Attributes.Except(parent.Columns.Keys);
            if (invalid.Count() > 0)
            {
                throw new EntityException($"Parent Entity does not contain an attribute named \"{invalid.First()}\"");
            }
            invalid = foreignKey.Attributes.Except(child.Columns.Keys);
            if (invalid.Count() > 0)
            {
                throw new EntityException($"Child Entity does not contain an attribute named \"{invalid.First()}\"");
            }
        }
    }
}
