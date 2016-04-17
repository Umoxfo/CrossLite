using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CrossLite.QueryBuilder;

namespace CrossLite
{
    /// <summary>
    /// This class represents a SQLite connection in ORM terms.
    /// 
    /// ORM is a technique to map database objects to Object Oriented Programming 
    /// Objects to let the developer focus on programming in an Object 
    /// Oriented manner.
    /// </summary>
    public abstract class SQLiteContext : IDisposable
    {
        /// <summary>
        /// The database connection
        /// </summary>
        public SQLiteConnection Connection { get; protected set; }

        /// <summary>
        /// Indicates whether the disposed method was called
        /// </summary>
        protected bool IsDisposed = false;

        /// <summary>
        /// Creates a new connection to an SQLite Database
        /// </summary>
        /// <param name="connectionString">The Connection string to connect to this database</param>
        public SQLiteContext(string connectionString)
        {
            Connection = new SQLiteConnection(connectionString);
        }

        /// <summary>
        /// Creates a new connection to an SQLite Database
        /// </summary>
        /// <param name="builder">The Connection string to connect to this database</param>
        public SQLiteContext(SQLiteConnectionStringBuilder builder)
        {
            Connection = new SQLiteConnection(builder.ToString());
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~SQLiteContext()
        {
            Dispose();
        }

        /// <summary>
        /// Disposes the DB connection
        /// </summary>
        public void Dispose()
        {
            if (Connection != null && !IsDisposed)
            {
                try
                {
                    Connection.Close();
                    Connection.Dispose();
                }
                catch (ObjectDisposedException) { }

                IsDisposed = true;
            }
        }

        #region Connection Management

        /// <summary>
        /// Opens the database connection
        /// </summary>
        public void Connect()
        {
            if (Connection.State != ConnectionState.Open)
            {
                try
                {
                    Connection.Open();
                }
                catch (Exception e)
                {
                    throw new DbConnectException("Unable to etablish database connection", e);
                }
            }
        }

        /// <summary>
        /// Closes the connection to the database
        /// </summary>
        public void Close()
        {
            try
            {
                if (Connection.State != ConnectionState.Closed)
                    Connection.Close();
            }
            catch (ObjectDisposedException) { }
        }

        #endregion Connection Management

        #region Execute Methods

        /// <summary>
        /// Executes a statement on the database (Update, Delete, Insert)
        /// </summary>
        /// <param name="Sql">The SQL statement to be executes</param>
        /// <returns>Returns the number of rows affected by the statement</returns>
        public int Execute(string Sql)
        {
            // Create the SQL Command
            using (SQLiteCommand Command = this.CreateCommand(Sql))
                return Command.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes a statement on the database (Update, Delete, Insert)
        /// </summary>
        /// <param name="Sql">The SQL statement to be executed</param>
        /// <param name="Params">A list of Sqlparameters</param>
        /// <returns>Returns the number of rows affected by the statement</returns>
        public int Execute(string Sql, List<DbParameter> Params)
        {
            // Create the SQL Command
            using (DbCommand Command = this.CreateCommand(Sql))
            {
                // Add params
                foreach (DbParameter Param in Params)
                    Command.Parameters.Add(Param);

                // Execute command, and dispose of the command
                return Command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Executes a statement on the database (Update, Delete, Insert)
        /// </summary>
        /// <param name="Sql">The SQL statement to be executed</param>
        /// <param name="Items">Additional parameters are parameter values for the query.
        /// The first parameter replaces @P0, second @P1 etc etc.
        /// </param>
        /// <returns>Returns the number of rows affected by the statement</returns>
        public int Execute(string Sql, params object[] Items)
        {
            // Create the SQL Command
            using (DbCommand Command = this.CreateCommand(Sql))
            {
                // Add params
                for (int i = 0; i < Items.Length; i++)
                {
                    DbParameter Param = this.CreateParam();
                    Param.ParameterName = "@P" + i;
                    Param.Value = Items[i];
                    Command.Parameters.Add(Param);
                }

                // Execute command, and dispose of the command
                return Command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result 
        /// set returned by the query. Additional columns or rows are ignored.
        /// </summary>
        /// <param name="Sql">The SQL statement to be executed</param>
        /// <returns></returns>
        public object ExecuteScalar(string Sql)
        {
            // Create the SQL Command
            using (DbCommand Command = this.CreateCommand(Sql))
                return Command.ExecuteScalar();
        }

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result 
        /// set returned by the query. Additional columns or rows are ignored.
        /// </summary>
        /// <param name="Sql">The SQL statement to be executed</param>
        /// <param name="Params">A list of Sqlparameters</param>
        /// <returns></returns>
        public object ExecuteScalar(string Sql, IEnumerable<DbParameter> Params)
        {
            // Create the SQL Command
            using (DbCommand Command = this.CreateCommand(Sql))
            {
                // Add params
                foreach (DbParameter Param in Params)
                    Command.Parameters.Add(Param);

                // Execute command, and dispose of the command
                return Command.ExecuteScalar();
            }
        }

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result 
        /// set returned by the query. Additional columns or rows are ignored.
        /// </summary>
        /// <param name="Sql">The SQL statement to be executed</param>
        /// <param name="Items"></param>
        /// <returns></returns>
        public object ExecuteScalar(string Sql, params object[] Items)
        {
            // Create the SQL Command
            using (DbCommand Command = this.CreateCommand(Sql))
            {
                // Add params
                for (int i = 0; i < Items.Length; i++)
                {
                    DbParameter Param = this.CreateParam();
                    Param.ParameterName = "@P" + i;
                    Param.Value = Items[i];
                    Command.Parameters.Add(Param);
                }

                // Execute command, and dispose of the command
                return Command.ExecuteScalar();
            }
        }

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result 
        /// set returned by the query. Additional columns or rows are ignored.
        /// </summary>
        /// <param name="Sql">The SQL statement to be executed</param>
        public T ExecuteScalar<T>(string Sql, params object[] Items)
        {
            // Create the SQL Command
            using (DbCommand Command = this.CreateCommand(Sql))
            {
                // Add params
                for (int i = 0; i < Items.Length; i++)
                {
                    DbParameter Param = this.CreateParam();
                    Param.ParameterName = "@P" + i;
                    Param.Value = Items[i];
                    Command.Parameters.Add(Param);
                }

                // Execute command, and dispose of the command
                object Value = Command.ExecuteScalar();
                return (T)Convert.ChangeType(Value, typeof(T), CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result 
        /// set returned by the query. Additional columns or rows are ignored.
        /// </summary>
        /// <param name="Command">The SQL Command to run on this database</param>
        public T ExecuteScalar<T>(DbCommand Command)
        {
            // Create the SQL Command
            using (Command)
            {
                // Execute command, and dispose of the command
                object Value = Command.ExecuteScalar();
                return (T)Convert.ChangeType(Value, typeof(T), CultureInfo.InvariantCulture);
            }
        }

        #endregion Execute Methods

        #region Read

        /// <summary>
        /// Queries the database, and returns a result set
        /// </summary>
        /// <param name="Sql">The SQL Statement to run on the database</param>
        /// <param name="Params">Additional parameters are parameter values for the query.
        /// The first parameter replaces @P0, second @P1 etc etc.
        /// </param>
        /// <returns></returns>
        public IEnumerable<Dictionary<string, object>> Query(string Sql, params object[] Params)
        {
            var paramItems = new List<SQLiteParameter>(Params.Length);
            for (int i = 0; i < Params.Length; i++)
            {
                SQLiteParameter Param = this.CreateParam();
                Param.ParameterName = "@P" + i;
                Param.Value = Params[i];
                paramItems.Add(Param);
            }

            return this.Query(Sql, paramItems);
        }

        /// <summary>
        /// Queries the database, and returns a result set
        /// </summary>
        /// <param name="Sql">The SQL Statement to run on the database</param>
        /// <param name="Params">A list of sql params to add to the command</param>
        /// <returns></returns>
        public IEnumerable<Dictionary<string, object>> Query(string Sql, IEnumerable<SQLiteParameter> Params)
        {
            // Create our Rows result
            var rows = new List<Dictionary<string, object>>();

            // Create the SQL Command
            using (SQLiteCommand command = this.CreateCommand(Sql))
            {
                // Add params
                foreach (SQLiteParameter Param in Params)
                    command.Parameters.Add(Param);

                // Execute the query
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    // If we have rows, add them to the list
                    if (reader.HasRows)
                    {
                        // Add each row to the rows list
                        while (reader.Read())
                        {
                            Dictionary<string, object> row = new Dictionary<string, object>(reader.FieldCount);
                            for (int i = 0; i < reader.FieldCount; ++i)
                                row.Add(reader.GetName(i), reader.GetValue(i));

                            yield return row;
                        }
                    }

                    // Cleanup
                    reader.Close();
                }
            }
        }

        /// <summary>
        /// Queries the database, and returns a result set
        /// </summary>
        /// <param name="Sql">The SQL Statement to run on the database</param>
        /// <param name="Params">Additional parameters are parameter values for the query.
        /// The first parameter replaces @P0, second @P1 etc etc.
        /// </param>
        /// <returns></returns>
        public IEnumerable<T> Query<T>(string Sql, params object[] Params)
        {
            var paramItems = new List<SQLiteParameter>(Params.Length);
            for (int i = 0; i < Params.Length; i++)
            {
                SQLiteParameter Param = this.CreateParam();
                Param.ParameterName = "@P" + i;
                Param.Value = Params[i];
                paramItems.Add(Param);
            }

            return this.Query<T>(Sql, paramItems);
        }

        /// <summary>
        /// Queries the database, and returns a result set
        /// </summary>
        /// <param name="Sql">The SQL Statement to run on the database</param>
        /// <param name="Params">A list of sql params to add to the command</param>
        /// <returns></returns>
        public IEnumerable<T> Query<T>(string Sql, IEnumerable<SQLiteParameter> Params)
        {
            // Get our Table Mapping
            Type objType = typeof(T);
            TableMapping table = EntityCache.GetTableMap(objType);

            // Create the SQL Command
            using (SQLiteCommand command = this.CreateCommand(Sql))
            {
                // Add params
                foreach (SQLiteParameter param in Params)
                    command.Parameters.Add(param);

                // Execute the query
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    // If we have rows, add them to the list
                    if (reader.HasRows)
                    {
                        // Add each row to the rows list
                        while (reader.Read())
                        {
                            // Add object
                            yield return (T)ConvertToEntity(table, reader);
                        }
                    }

                    // Cleanup
                    reader.Close();
                }
            }
        }

        /// <summary>
        /// Peforms a SELECT query on the Entity Type, and returns the results
        /// </summary>
        /// <typeparam name="T">The Entity Type</typeparam>
        /// <returns></returns>
        internal IEnumerable<T> Select<T>()
        {
            // Get our Table Mapping
            Type objType = typeof(T);
            TableMapping table = EntityCache.GetTableMap(objType);
            string sql = $"SELECT * FROM `{table.TableName}`;";

            // Create the SQL Command
            using (SQLiteCommand command = this.CreateCommand(sql))
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                // If we have rows, add them to the list
                if (reader.HasRows)
                {
                    // Return each row
                    while (reader.Read())
                        yield return (T)ConvertToEntity(table, reader);
                }

                // Cleanup
                reader.Close();
            }
        }

        #endregion Read

        #region Create

        /// <summary>
        /// Inserts a new Entity into the database
        /// </summary>
        /// <typeparam name="T">The Entity Type</typeparam>
        /// <param name="obj"></param>
        /// <returns>The number of rows affected by this operation</returns>
        internal int Insert<TEntity>(TEntity obj) where TEntity : class
        {
            // Get our Table Mapping
            Type objType = typeof(TEntity);
            TableMapping table = EntityCache.GetTableMap(objType);
            string[] keys = table.CompositeKeys;

            // Generate the SQL
            InsertQueryBuilder builder = new InsertQueryBuilder(table.TableName, this);
            foreach (var attribute in table.Columns)
            {
                // Grab value
                object value = attribute.Value.Property.GetValue(obj);

                // Check for auto increments!
                if (keys.Contains(attribute.Key))
                {
                    // Skip inserting Auto Increment fields!
                    if (attribute.Value.AutoIncrement == true)
                        continue;

                    // Skip single primary keys that are Integers and do not have a value, 
                    // This will cause the key to perform an Auto Increment
                    if (table.HasPrimaryKey && IsNumericType(attribute.Value.Property.PropertyType) && value == null)
                        continue;
                }

                // Add attribute to the field list
                builder.SetField(attribute.Key, value);
            }

            // Create the SQL Command
            return builder.Execute();
        }

        #endregion Create

        #region Update

        /// <summary>
        /// Updates an Entity in the database, provided none of the Primary
        /// keys were modified.
        /// </summary>
        /// <typeparam name="T">The Entity Type</typeparam>
        /// <param name="obj"></param>
        /// <returns>The number of rows affected by this operation</returns>
        internal int Update<TEntity>(TEntity obj) where TEntity : class
        {
            // Get our Table Mapping
            Type objType = typeof(TEntity);
            TableMapping table = EntityCache.GetTableMap(objType);

            // Generate the SQL
            UpdateQueryBuilder builder = new UpdateQueryBuilder(table.TableName, this);
            builder.SetWhereOperator(LogicOperator.And);
            foreach (var attribute in table.Columns)
            {
                // Check for keys
                if (table.CompositeKeys.Contains(attribute.Key))
                    continue;

                object value = attribute.Value.Property.GetValue(obj);
                builder.SetField(attribute.Key, value);
            }

            // build the where statement, using primary keys
            foreach (string keyName in table.CompositeKeys)
            {
                PropertyInfo info = table.Columns[keyName].Property;
                builder.AddWhere(keyName, Comparison.Equals, info.GetValue(obj));
            }

            // Create the SQL Command
            return builder.Execute();
        }

        #endregion Update

        #region Delete

        /// <summary>
        /// Deletes an Entity from the database
        /// </summary>
        /// <typeparam name="T">The Entity Type</typeparam>
        /// <param name="obj">The entity to remove from the database</param>
        /// <returns>The number of rows affected by this operation</returns>
        internal int Delete<TEntity>(TEntity obj) where TEntity : class
        {
            // Get our Table Mapping
            Type objType = typeof(TEntity);
            TableMapping table = EntityCache.GetTableMap(objType);
            WhereStatement statement = null;
            WhereClause where = null;

            // build the where statement, using primary keys
            foreach (string keyName in table.CompositeKeys)
            {
                PropertyInfo info = table.Columns[keyName].Property;
                if (statement == null)
                {
                    statement = new WhereStatement();
                    where = statement.Add(keyName, Comparison.Equals, info.GetValue(obj));
                }
                else
                    where.AddClause(LogicOperator.And, keyName, Comparison.Equals, info.GetValue(obj));
            }

            // Build the SQL query and perform the deletion
            string sql = $"DELETE FROM `{table.TableName}`";
            using (SQLiteCommand command = this.CreateCommand(sql))
            {
                // Append the where statement
                if (statement.Count > 0)
                    command.CommandText += $" WHERE {statement.BuildStatement(command)}";

                return command.ExecuteNonQuery();
            }
        }

        #endregion Delete

        #region Database Operations

        /// <summary>
        /// By passing an Entity type, this method will use the Attribute's
        /// attached to each of the entities properties to generate an 
        /// SQL command, that wil create a table on the database.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="ifNotExists">Indicates whether to skip the table creation 
        /// if the table already exists on the database.</param>
        public void CreateTable<TEntity>(bool ifNotExists = false) where TEntity : class
        {
            // Get our table mapping
            Type entityType = typeof(TEntity);
            TableMapping table = EntityCache.GetTableMap(entityType);

            // For later use
            List<AttributeInfo> withFKs = new List<AttributeInfo>();

            // Begin the SQL generation
            StringBuilder sql = new StringBuilder("CREATE TABLE ");
            sql.AppendIf(ifNotExists, "IF NOT EXISTS ");
            sql.AppendLine($"`{table.TableName}` (");

            // Append attributes
            foreach (var colData in table.Columns)
            {
                // Get attribute data
                AttributeInfo info = colData.Value;
                Type propertyType = info.Property.PropertyType;

                // For later use
                if (info.ForeignKey != null)
                    withFKs.Add(info);

                // Start building our SQL
                sql.Append($"\t`{colData.Key}` {GetSQLiteType(info.Property.PropertyType)}");

                // Primary Key and Unique column definition
                sql.AppendIf(table.HasPrimaryKey && info.PrimaryKey, $" PRIMARY KEY");
                sql.AppendIf(info.Unique, " UNIQUE");

                // Auto Increment
                sql.AppendIf(info.AutoIncrement, " AUTOINCREMENT");

                // Nullable definition
                bool canBeNull = !propertyType.IsValueType || (Nullable.GetUnderlyingType(propertyType) != null);
                if (info.HasNotNullableAttribute || (!info.PrimaryKey && !canBeNull))
                    sql.Append(" NOT NULL");

                // Default value
                if (info.DefaultValue != null)
                {
                    SQLiteDataType type = GetSQLiteType(info.DefaultValue.GetType());

                    // No quotes
                    if (type == SQLiteDataType.INTEGER || type == SQLiteDataType.REAL)
                        sql.Append($" DEFAULT {info.DefaultValue}");
                    else
                        sql.Append($" DEFAULT \"{info.DefaultValue}\"");
                }

                // Add last comma
                sql.AppendLine(",");
            }

            // Add Composite Keys
            string[] keys = table.CompositeKeys;
            if (!table.HasPrimaryKey && keys.Length > 0)
            {
                sql.Append($"\tPRIMARY KEY(");
                sql.Append(String.Join(", ", keys));
                sql.AppendLine("),");
            }

            // Add Composite Unique's
            foreach (var cu in table.CompositeUnique)
            {
                sql.Append($"\tUNIQUE(");
                sql.Append(String.Join(", ", cu.Attributes));
                sql.AppendLine("),");
            }

            // Foreign Keys
            foreach (AttributeInfo info in withFKs)
            {
                // Primary table attributes
                ForeignKeyAttribute fk = info.ForeignKey;
                string attr = String.Join(", ", fk.OnAttribute);

                // Build sql command
                TableMapping map = EntityCache.GetTableMap(fk.OnEntity);
                sql.Append($"\tFOREIGN KEY({info.Name}) REFERENCES {map.TableName}({attr})");

                // Add integrety options
                sql.AppendIf(fk.OnUpdate != ReferentialIntegrity.NoAction, $" ON UPDATE {ToSQLite(fk.OnUpdate)}");
                sql.AppendIf(fk.OnDelete != ReferentialIntegrity.NoAction, $" ON DELETE {ToSQLite(fk.OnDelete)}");

                // Finish the line
                sql.AppendLine(",");
            }

            // Composite Foreign Keys
            foreach (var fk in table.CompositeForeignKeys)
            {
                TableMapping map = EntityCache.GetTableMap(fk.OnEntity);
                sql.AppendFormat("\tFOREIGN KEY({0}) REFERENCES {1}({2})",
                    String.Join(", ", fk.FromAttributes),
                    map.TableName,
                    String.Join(", ", fk.OnAttributes)
                );

                // Add integrety options
                sql.AppendIf(fk.OnUpdate != ReferentialIntegrity.NoAction, $" ON UPDATE {ToSQLite(fk.OnUpdate)}");
                sql.AppendIf(fk.OnDelete != ReferentialIntegrity.NoAction, $" ON DELETE {ToSQLite(fk.OnDelete)}");

                // Finish the line
                sql.AppendLine(",");
            }

            // Convert to string
            string sqlLine = String.Concat(
                sql.ToString().TrimEnd(new char[] { '\r', '\n', ',' }), 
                Environment.NewLine, 
                ")"
            );

            // Without row id?
            if (table.WithoutRowID)
                sqlLine += " WITHOUT ROWID;";

            // Now execute the command on the database
            using (SQLiteCommand command = this.CreateCommand(sqlLine))
            {
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Drops the specified table Entity from the database if it exists
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        public void DropTable<TEntity>() where TEntity : class
        {
            // Get our table mapping
            Type entityType = typeof(TEntity);
            TableMapping table = EntityCache.GetTableMap(entityType);

            // Build the SQL query and perform the deletion
            string sql = $"DROP TABLE IF EXISTS `{table.TableName}`";
            using (SQLiteCommand command = this.CreateCommand(sql))
            {
                command.ExecuteNonQuery();
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a new command to be executed on the database
        /// </summary>
        /// <param name="queryString">The query string this command will use</param>
        public SQLiteCommand CreateCommand(string queryString) => new SQLiteCommand(queryString, Connection);

        /// <summary>
        /// Creates a DbParameter using the current Database engine's Parameter object
        /// </summary>
        /// <returns></returns>
        public SQLiteParameter CreateParam() => new SQLiteParameter();

        /// <summary>
        /// Begins a new database transaction
        /// </summary>
        /// <returns></returns>
        public DbTransaction BeginTransaction() => Connection.BeginTransaction();
        /// <summary>
        /// Converts attributes from an <see cref="SQLiteDataReader"/> to an Entity
        /// </summary>
        /// <param name="table">The <see cref="TableMapping"/> for this Entity</param>
        /// <param name="reader">The current, open DataReader object</param>
        /// <returns></returns>
        protected virtual object ConvertToEntity(TableMapping table, SQLiteDataReader reader)
        {
            // Use reflection to map the column name to the object Property
            object entity = Activator.CreateInstance(table.EntityType);
            for (int i = 0; i < reader.FieldCount; ++i)
            {
                string attrName = reader.GetName(i);
                PropertyInfo property = table.GetAttribute(attrName).Property;

                // SQLite doesn't support nearly as many primitive types as
                // C# does, so we must translate
                switch (Type.GetTypeCode(property.PropertyType))
                {
                    case TypeCode.Int16:
                        property.SetValue(entity, reader.GetInt16(i));
                        break;
                    case TypeCode.Int32:
                        property.SetValue(entity, reader.GetInt32(i));
                        break;
                    case TypeCode.Int64:
                        property.SetValue(entity, reader.GetInt64(i));
                        break;
                    case TypeCode.Boolean:
                        property.SetValue(entity, reader.GetBoolean(i));
                        break;
                    case TypeCode.Decimal:
                        property.SetValue(entity, reader.GetDecimal(i));
                        break;
                    case TypeCode.Double:
                        property.SetValue(entity, reader.GetDouble(i));
                        break;
                    case TypeCode.Char:
                        property.SetValue(entity, reader.GetChar(i));
                        break;
                    default:
                        property.SetValue(entity, reader.GetValue(i));
                        break;
                }
            }

            // Add object
            return entity;
        }

        /// <summary>
        /// Determines if a type is numeric. Nullable numeric types are considered numeric.
        /// </summary>
        /// <remarks>
        /// Boolean is not considered numeric.
        /// </remarks>
        protected static bool IsNumericType(Type type)
        {
            if (type == null)
                return false;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Converts a C# data type to a textual SQLite data type
        /// </summary>
        /// <param name="propertyType"></param>
        /// <returns></returns>
        internal SQLiteDataType GetSQLiteType(Type propertyType)
        {
            switch (Type.GetTypeCode(propertyType))
            {
                case TypeCode.Boolean:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Byte:
                case TypeCode.Char:
                    return SQLiteDataType.INTEGER;
                case TypeCode.String:
                    return SQLiteDataType.TEXT;
                case TypeCode.Object:
                    return SQLiteDataType.BLOB;
                case TypeCode.Decimal:
                case TypeCode.DateTime:
                    return SQLiteDataType.NUMERIC;
                case TypeCode.Double:
                    return SQLiteDataType.REAL;
                default:
                    throw new NotSupportedException("Invalid object type conversion.");
            }
        }

        /// <summary>
        /// Converts a <see cref="ReferentialIntegrity"/> item to its SQLite
        /// string equivelant
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        internal static string ToSQLite(ReferentialIntegrity action)
        {
            switch (action)
            {
                case ReferentialIntegrity.Cascade: return "CASCADE";
                case ReferentialIntegrity.Restrict: return "RESTRICT";
                case ReferentialIntegrity.SetDefault: return "SET DEFAULT";
                case ReferentialIntegrity.SetNull: return "SET NULL";
                default: return "NO ACTION";
            }
        }

        #endregion Helper Methods
    }
}
