using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossLite
{
    internal static class EntityCache
    {
        /// <summary>
        /// Gets a list of Entity => table mappings
        /// </summary>
        internal static Dictionary<Type, TableMapping> Mappings { get; set; }

        static EntityCache()
        {
            Mappings = new Dictionary<Type, TableMapping>();
        }

        /// <summary>
        /// Gets or Creates a new <see cref="TableMapping"/> for the provided
        /// Entity Type provided.
        /// </summary>
        /// <param name="objType"></param>
        /// <returns></returns>
        public static TableMapping GetTableMap(Type objType)
        {
            // Grab our type for mapping objects
            if (!Mappings.ContainsKey(objType))
                Mappings[objType] = new TableMapping(objType);

            return Mappings[objType];
        }
    }
}
