using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossLite
{
    public enum ReferentialIntegrity
    {
        /// <summary>
        /// When a parent key is modified or deleted from the database, 
        /// no special action is taken.
        /// </summary>
        NoAction,

        /// <summary>
        /// The application is prohibited from deleting (for ON DELETE RESTRICT) 
        /// or modifying (for ON UPDATE RESTRICT) a parent key when there exists 
        /// one or more child keys mapped to it.
        /// </summary>
        Restrict,

        /// <summary>
        /// When a parent key is deleted (for ON DELETE SET NULL) or modified 
        /// (for ON UPDATE SET NULL), the child key columns of all rows in the 
        /// child table that mapped to the parent key are  set to contain SQL 
        /// NULL values.
        /// </summary>
        SetNull,

        /// <summary>
        /// Similar to "SET NULL", except that each of the child key columns is 
        /// set to contain the columns default value instead of NULL.
        /// </summary>
        SetDefault,

        /// <summary>
        /// Propagates the delete or update operation on the parent key to each 
        /// dependent child key.
        /// </summary>
        Cascade
    }
}
