using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using CrossLite.CodeFirst;

namespace CrossLite
{
    /// <summary>
    /// This class represents a SQLite connection in ORM terms.
    /// 
    /// ORM is a technique to map database objects to Object Oriented Programming 
    /// Objects to let the developer focus on programming in an Object 
    /// Oriented manner.
    /// </summary>
    public class SQLiteContext : IDisposable
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
        /// <see cref="EscapeCharacters"/>
        /// </summary>
        protected static char[] EscapeChars = new char[2] { '`', '`' };

        /// <summary>
        /// Gets or sets the starting and ending delimiters to use when specifying SQL 
        /// database objects, such as tables or columns, whose names contain characters 
        /// such as spaces or reserved tokens
        /// </summary>
        public static char[] EscapeCharacters
        {
            get { return EscapeChars; }
            set
            {
                // Must be 2
                if (value.Length != 2)
                    throw new ArgumentException("Delimiter length must be 2 characters!", "EscapeCharacters");

                EscapeChars = value;
            }
        }

        /// <summary>
        /// Contains the conenction string used to open this connection
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// Creates a new connection to an SQLite Database
        /// </summary>
        /// <param name="connectionString">The Connection string to connect to this database</param>
        public SQLiteContext(string connectionString)
        {
            ConnectionString = connectionString;
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
        public T ExecuteScalar<T>(string Sql, params object[] Items) where T : IConvertible
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
        public T ExecuteScalar<T>(DbCommand Command) where T : IConvertible
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

        #region Query Methods

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
        public IEnumerable<T> Query<T>(string Sql, params object[] Params) where T : class
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
        public IEnumerable<T> Query<T>(string Sql, IEnumerable<SQLiteParameter> Params) where T : class
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
                            yield return ConvertToEntity<T>(table, reader);
                        }
                    }

                    // Cleanup
                    reader.Close();
                }
            }
        }

        /// <summary>
        /// Peforms a SELECT query on the Entity Type, and returns the Enumerator
        /// for the Result set.
        /// </summary>
        /// <typeparam name="TEntity">The Entity Type</typeparam>
        /// <returns></returns>
        internal IEnumerable<TEntity> Select<TEntity>() where TEntity : class
        {
            // Get our Table Mapping
            Type objType = typeof(TEntity);
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
                        yield return ConvertToEntity<TEntity>(table, reader);
                }

                // Cleanup
                reader.Close();
            }
        }

        #endregion Query Methods

        #region Code First

        /// <summary>
        /// By passing an Entity type, this method will use the Attribute's
        /// attached to each of the entities properties to generate an 
        /// SQL command, that will create a table on the database.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="flags">Additional flags for SQL generation</param>
        public void CreateTable<TEntity>(TableCreationOptions flags = TableCreationOptions.None) 
            where TEntity : class
        {
            // Get our table mapping
            Type entityType = typeof(TEntity);
            TableMapping table = EntityCache.GetTableMap(entityType);

            // Column defined foreign keys
            List<AttributeInfo> withFKs = new List<AttributeInfo>();

            // -----------------------------------------
            // Begin the SQL generation
            // -----------------------------------------
            StringBuilder sql = new StringBuilder("CREATE ");
            sql.AppendIf(flags.HasFlag(TableCreationOptions.Temporary), "TEMP ");
            sql.Append("TABLE ");
            sql.AppendIf(flags.HasFlag(TableCreationOptions.IfNotExists), "IF NOT EXISTS ");
            sql.AppendLine($"{Escape(table.TableName)} (");

            // -----------------------------------------
            // Append attributes
            // -----------------------------------------
            foreach (var colData in table.Columns)
            {
                // Get attribute data
                AttributeInfo info = colData.Value;
                Type propertyType = info.Property.PropertyType;
                SQLiteDataType pSqlType = GetSQLiteType(propertyType);

                // Start appending column definition SQL
                sql.Append($"\t{Escape(colData.Key)} {pSqlType}");

                // Primary Key and Unique column definition
                if (info.AutoIncrement || (table.HasPrimaryKey && info.PrimaryKey))
                {
                    sql.AppendIf(table.HasPrimaryKey && info.PrimaryKey, $" PRIMARY KEY");
                    sql.AppendIf(info.AutoIncrement && pSqlType == SQLiteDataType.INTEGER, " AUTOINCREMENT");
                }
                else if (info.Unique)
                {
                    // Unique column definition
                    sql.Append(" UNIQUE");
                }

                // Collation
                sql.AppendIf(
                    info.Collation != Collation.Default && pSqlType == SQLiteDataType.TEXT, 
                    " COLLATE " + info.Collation.ToString().ToUpperInvariant()
                );

                // Nullable definition
                bool canBeNull = !propertyType.IsValueType || (Nullable.GetUnderlyingType(propertyType) != null);
                if (info.HasRequiredAttribute || (!info.PrimaryKey && !canBeNull))
                    sql.Append(" NOT NULL");

                // Default value
                if (info.DefaultValue != null)
                {
                    // Do we need to quote this?
                    SQLiteDataType type = GetSQLiteType(info.DefaultValue.GetType());
                    if (type == SQLiteDataType.INTEGER || type == SQLiteDataType.REAL)
                        sql.Append($" DEFAULT {info.DefaultValue}");
                    else
                        sql.Append($" DEFAULT \"{info.DefaultValue}\"");
                }

                // Add last comma
                sql.AppendLine(",");

                // For later use
                if (info.ForeignKey != null)
                    withFKs.Add(info);
            }

            // -----------------------------------------
            // Composite Keys
            // -----------------------------------------
            string[] keys = table.CompositeKeys; // Linq query; Perform once here
            if (!table.HasPrimaryKey && keys.Length > 0)
            {
                sql.Append($"\tPRIMARY KEY(");
                sql.Append(String.Join(", ", keys.Select(x => Escape(x))));
                sql.AppendLine("),");
            }

            // -----------------------------------------
            // Composite Unique Constraints
            // -----------------------------------------
            foreach (var cu in table.UniqueConstraints)
            {
                sql.Append($"\tUNIQUE(");
                sql.Append(String.Join(", ", cu.Attributes.Select(x => Escape(x))));
                sql.AppendLine("),");
            }

            // -----------------------------------------
            // Foreign Keys
            // -----------------------------------------
            foreach (ForeignKeyInfo info in table.ForeignKeys)
            {
                // Primary table attributes
                ForeignKeyAttribute fk = info.ForeignKey;
                string attrs1 = String.Join(", ", fk.Attributes.Select(x => Escape(x)));
                string attrs2 = String.Join(", ", info.InverseKey.Attributes.Select(x => Escape(x)));

                // Build sql command
                TableMapping map = EntityCache.GetTableMap(info.ParentEntityType);
                sql.Append($"\tFOREIGN KEY({Escape(attrs1)}) REFERENCES {Escape(map.TableName)}({attrs2})");

                // Add integrety options
                sql.AppendIf(fk.OnUpdate != ReferentialIntegrity.NoAction, $" ON UPDATE {ToSQLite(fk.OnUpdate)}");
                sql.AppendIf(fk.OnDelete != ReferentialIntegrity.NoAction, $" ON DELETE {ToSQLite(fk.OnDelete)}");

                // Finish the line
                sql.AppendLine(",");
            }

            // -----------------------------------------
            // SQL wrap up
            // -----------------------------------------
            string sqlLine = String.Concat(
                sql.ToString().TrimEnd(new char[] { '\r', '\n', ',' }), 
                Environment.NewLine, 
                ")"
            );

            // Without row id?
            if (table.WithoutRowID)
                sqlLine += " WITHOUT ROWID;";

            // -----------------------------------------
            // Execute the command on the database
            // -----------------------------------------
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
            string sql = $"DROP TABLE IF EXISTS {Escape(table.TableName)}";
            using (SQLiteCommand command = this.CreateCommand(sql))
            {
                command.ExecuteNonQuery();
            }
        }

        #endregion Code First

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
        internal TEntity ConvertToEntity<TEntity>(TableMapping table, SQLiteDataReader reader)
        {
            // Use reflection to map the column name to the object Property
            TEntity entity = (TEntity)Activator.CreateInstance(table.EntityType);
            for (int i = 0; i < reader.FieldCount; ++i)
            {
                string attrName = reader.GetName(i);
                PropertyInfo property = table.GetAttribute(attrName).Property;

                // SQLite doesn't support nearly as many primitive types as
                // C# does, so we must translate
                switch (Type.GetTypeCode(property.PropertyType))
                {
                    case TypeCode.Byte:
                        property.SetValue(entity, reader.GetByte(i));
                        break;
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

            // Foreign keys!
            table.CreateRelationships(typeof(TEntity), entity, this);

            // Add object
            return entity;
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

        /// <summary>
        /// Wraps the string with Indentifer Delimiters, to prevent keyword
        /// errors in SQL statements.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Escape(string value)
        {
            return String.Concat(EscapeChars[0], value.Trim(EscapeChars), EscapeChars[1]);
        }

        #endregion Helper Methods
    }
}
