using System.Collections;
using System.Collections.Generic;

namespace CrossLite
{
    /// <summary>
    /// A <see cref="DbSet{TEntity}"/> represents a collection
    /// of Entities (Aka: rows) in the SQLite database.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class DbSet<TEntity> : IEnumerable<TEntity>
        where TEntity : class
    {
        /// <summary>
        /// The database context
        /// </summary>
        protected SQLiteContext Context { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="DbSet{TEntity}"/>
        /// </summary>
        /// <param name="context"></param>
        public DbSet(SQLiteContext context)
        {
            Context = context;
        }

        /// <summary>
        /// Inserts a new Entity into the database
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>The number of rows affected by this operation</returns>
        public int Add(TEntity obj)
        {
            // insert
            return Context.Insert(obj);
        }

        /// <summary>
        /// Deletes the Entity from the database
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>The number of rows affected by this operation</returns>
        public int Remove(TEntity obj)
        {
            // delete
            return Context.Delete(obj);
        }

        /// <summary>
        /// Updates the Entity's attributes in the database
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>The number of rows affected by this operation</returns>
        public int Update(TEntity obj)
        {
            // delete
            return Context.Update(obj);
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            return Context.Select<TEntity>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Context.Select<TEntity>().GetEnumerator();
        }
    }
}
