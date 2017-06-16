using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Globalization;
using System.Reflection;
using System.Text;
using CrossLite.CodeFirst;
using CrossLite.QueryBuilder;

namespace CrossLite
{
    /// <summary>
    /// This class represents an SQLite connection with ORM query methods.
    /// 
    /// ORM is a technique to map database objects to Object Oriented Programming 
    /// Objects to let the developer focus on programming in an Object 
    /// Oriented manner.
    /// </summary>
    public class SQLiteContext : IDisposable
    {
        /// <summary>
        /// Gets or sets the default <see cref="CrossLite.IdentifierQuoteMode"/> for queries. New instances of
        /// <see cref="SQLiteContext"/> with automatically dfefault to this value.
        /// </summary>
        public static IdentifierQuoteMode DefaultIdentifierQuoteMode { get; set; } = IdentifierQuoteMode.None;

        /// <summary>
        /// Gets or sets the default <see cref="CrossLite.IdentifierQuoteKind"/> for queries. New instances of
        /// <see cref="SQLiteContext"/> with automatically dfefault to this value.
        /// </summary>
        public static IdentifierQuoteKind DefaultIdentifierQuoteKind { get; set; } = IdentifierQuoteKind.Default;

        /// <summary>
        /// The database connection
        /// </summary>
        public SQLiteConnection Connection { get; protected set; }

        /// <summary>
        /// Indicates whether the disposed method was called
        /// </summary>
        protected bool IsDisposed = false;

        /// <summary>
        /// Gets or sets the <see cref="CrossLite.IdentifierQuoteMode"/> this instance will use for queries
        /// </summary>
        public IdentifierQuoteMode IdentifierQuoteMode { get; set; } = DefaultIdentifierQuoteMode;

        /// <summary>
        /// Gets or sets the <see cref="CrossLite.IdentifierQuoteKind"/> this instance will use for queries
        /// </summary>
        public IdentifierQuoteKind IdentifierQuoteKind { get; set; } = DefaultIdentifierQuoteKind;

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
            ConnectionString = builder.ToString();
            Connection = new SQLiteConnection(ConnectionString);
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
                    // Close and dispose of the internal connection
                    Connection.Close();
                    Connection.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // We dont do anything here
                }
                finally
                {
                    // Always set this to true!
                    IsDisposed = true;
                }
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
        /// <param name="sql">The SQL statement to be executes</param>
        /// <returns>Returns the number of rows affected by the statement</returns>
        public int Execute(string sql)
        {
            // Create the SQL Command
            using (SQLiteCommand Command = this.CreateCommand(sql))
                return Command.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes a statement on the database (Update, Delete, Insert)
        /// </summary>
        /// <param name="sql">The SQL statement to be executed</param>
        /// <param name="parameters">A list of Sqlparameters</param>
        /// <returns>Returns the number of rows affected by the statement</returns>
        public int Execute(string sql, List<DbParameter> parameters)
        {
            // Create the SQL Command
            using (DbCommand Command = this.CreateCommand(sql))
            {
                // Add params
                foreach (DbParameter Param in parameters)
                    Command.Parameters.Add(Param);

                // Execute command, and dispose of the command
                return Command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Executes a statement on the database (Update, Delete, Insert)
        /// </summary>
        /// <param name="sql">The SQL statement to be executed</param>
        /// <param name="items">Additional parameters are parameter values for the query.
        /// The first parameter replaces @P0, second @P1 etc etc.
        /// </param>
        /// <returns>Returns the number of rows affected by the statement</returns>
        public int Execute(string sql, params object[] items)
        {
            // Create the SQL Command
            using (DbCommand Command = this.CreateCommand(sql))
            {
                // Add params
                for (int i = 0; i < items.Length; i++)
                {
                    DbParameter Param = this.CreateParameter();
                    Param.ParameterName = "@P" + i;
                    Param.Value = items[i];
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
        /// <param name="sql">The SQL statement to be executed</param>
        /// <returns></returns>
        public object ExecuteScalar(string sql)
        {
            // Create the SQL Command
            using (DbCommand Command = this.CreateCommand(sql))
                return Command.ExecuteScalar();
        }

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result 
        /// set returned by the query. Additional columns or rows are ignored.
        /// </summary>
        /// <param name="sql">The SQL statement to be executed</param>
        /// <param name="parameters">A list of Sqlparameters</param>
        /// <returns></returns>
        public object ExecuteScalar(string sql, IEnumerable<DbParameter> parameters)
        {
            // Create the SQL Command
            using (DbCommand Command = this.CreateCommand(sql))
            {
                // Add params
                foreach (DbParameter Param in parameters)
                    Command.Parameters.Add(Param);

                // Execute command, and dispose of the command
                return Command.ExecuteScalar();
            }
        }

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result 
        /// set returned by the query. Additional columns or rows are ignored.
        /// </summary>
        /// <param name="sql">The SQL statement to be executed</param>
        /// <param name="items"></param>
        /// <returns></returns>
        public object ExecuteScalar(string sql, params object[] items)
        {
            // Create the SQL Command
            using (DbCommand Command = this.CreateCommand(sql))
            {
                // Add params
                for (int i = 0; i < items.Length; i++)
                {
                    DbParameter Param = this.CreateParameter();
                    Param.ParameterName = "@P" + i;
                    Param.Value = items[i];
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
        /// <param name="sql">The SQL statement to be executed</param>
        public T ExecuteScalar<T>(string sql, params object[] items) where T : IConvertible
        {
            // Create the SQL Command
            using (DbCommand Command = this.CreateCommand(sql))
            {
                // Add params
                for (int i = 0; i < items.Length; i++)
                {
                    DbParameter Param = this.CreateParameter();
                    Param.ParameterName = "@P" + i;
                    Param.Value = items[i];
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
        /// <param name="command">The SQL Command to run on this database</param>
        public T ExecuteScalar<T>(DbCommand command) where T : IConvertible
        {
            // Create the SQL Command
            using (command)
            {
                // Execute command, and dispose of the command
                object Value = command.ExecuteScalar();
                return (T)Convert.ChangeType(Value, typeof(T), CultureInfo.InvariantCulture);
            }
        }

        #endregion Execute Methods

        #region Query Methods

        /// <summary>
        /// Queries the database, and returns a result set
        /// </summary>
        /// <param name="sql">The SQL Statement to run on the database</param>
        /// <param name="parameters">Additional parameters are parameter values for the query.
        /// The first parameter replaces @P0, second @P1 etc etc.
        /// </param>
        /// <returns></returns>
        public IEnumerable<Dictionary<string, object>> Query(string sql, params object[] parameters)
        {
            var paramItems = new List<SQLiteParameter>(parameters.Length);
            for (int i = 0; i < parameters.Length; i++)
            {
                SQLiteParameter Param = this.CreateParameter();
                Param.ParameterName = "@P" + i;
                Param.Value = parameters[i];
                paramItems.Add(Param);
            }

            return this.Query(sql, paramItems);
        }

        /// <summary>
        /// Queries the database, and returns a result set
        /// </summary>
        /// <param name="sql">The SQL Statement to run on the database</param>
        /// <param name="parameters">A list of sql params to add to the command</param>
        /// <returns></returns>
        public IEnumerable<Dictionary<string, object>> Query(string sql, IEnumerable<SQLiteParameter> parameters)
        {
            // Create our Rows result
            var rows = new List<Dictionary<string, object>>();

            // Create the SQL Command
            using (SQLiteCommand command = this.CreateCommand(sql))
            {
                // Add params
                foreach (SQLiteParameter Param in parameters)
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
        /// <param name="sql">The SQL Statement to run on the database</param>
        /// <param name="parameters">Additional parameters are parameter values for the query.
        /// The first parameter replaces @P0, second @P1 etc etc.
        /// </param>
        /// <returns></returns>
        public IEnumerable<T> Query<T>(string sql, params object[] parameters) where T : class
        {
            var paramItems = new List<SQLiteParameter>(parameters.Length);
            for (int i = 0; i < parameters.Length; i++)
            {
                SQLiteParameter Param = this.CreateParameter();
                Param.ParameterName = "@P" + i;
                Param.Value = parameters[i];
                paramItems.Add(Param);
            }

            return this.Query<T>(sql, paramItems);
        }

        /// <summary>
        /// Queries the database, and returns a result set
        /// </summary>
        /// <param name="sql">The SQL Statement to run on the database</param>
        /// <param name="parameters">A list of sql params to add to the command</param>
        /// <returns></returns>
        public IEnumerable<T> Query<T>(string sql, IEnumerable<SQLiteParameter> parameters) where T : class
        {
            // Get our Table Mapping
            Type objType = typeof(T);
            TableMapping table = EntityCache.GetTableMap(objType);

            // Create the SQL Command
            using (SQLiteCommand command = this.CreateCommand(sql))
            {
                // Add params
                foreach (SQLiteParameter param in parameters)
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
        /// Executes the given Sql command and returns the result rows as entities
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<T> ExecuteReader<T>(SQLiteCommand command) where T : class
        {
            // Get our Table Mapping
            Type objType = typeof(T);
            TableMapping table = EntityCache.GetTableMap(objType);
            command.Connection = this.Connection;

            // Create the SQL Command
            using (command)
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

        /// <summary>
        /// Executes the given Sql command and returns the result rows
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Dictionary<string, object>> ExecuteReader(SQLiteCommand command)
        {
            // Create our Rows result
            var rows = new List<Dictionary<string, object>>();

            // Create the SQL Command
            using (command)
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
            string sql = $"SELECT * FROM {QuoteIdentifier(table.TableName)};";

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

        #region Indexing Methods

        /// <summary>
        /// Creates an index with the specified name on the specified table
        /// </summary>
        /// <param name="name"></param>
        /// <param name="table"></param>
        /// <param name="cols"></param>
        /// <param name="options"></param>
        /// <param name="where"></param>
        public void CreateIndex(string name, string table, IndexedColumn[] cols, IndexCreationOptions options, WhereStatement where = null)
        {
            // -----------------------------------------
            // Begin the SQL generation
            // -----------------------------------------
            StringBuilder sql = new StringBuilder("CREATE ", 256);
            sql.AppendIf(options.HasFlag(IndexCreationOptions.Unique), "UNIQUE ");
            sql.Append("INDEX ");
            sql.AppendIf(options.HasFlag(IndexCreationOptions.IfNotExists), "IF NOT EXISTS ");

            // Append index name
            sql.Append($"{name} ON ");
            sql.Append(QuoteIdentifier(table, this.IdentifierQuoteMode, this.IdentifierQuoteKind));
            sql.Append("(");

            // Append columns
            int i = cols.Length;
            foreach (var col in cols)
            {
                --i;
                sql.Append(QuoteIdentifier(col.Name, this.IdentifierQuoteMode, this.IdentifierQuoteKind));
                sql.AppendIf(col.Collate != Collation.Default, $" COLLATE {col.Collate.ToString().ToUpperInvariant()}");
                sql.AppendIf(col.SortOrder == Sorting.Descending, " DESC");
                sql.AppendIf(i > 0, ", ");
            }

            // Close
            sql.Append(")");

            // Add where if we have one
            if (where != null)
            {
                sql.Append(" WHERE ");
                sql.Append(where.BuildStatement());
            }

            // -----------------------------------------
            // Execute the command on the database
            // -----------------------------------------
            using (SQLiteCommand command = CreateCommand(sql.ToString()))
            {
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Drops an index with the specified name from the database. 
        /// </summary>
        /// <param name="name"></param>
        public void DropIndex(string name)
        {
            string sql = $"DROP INDEX IF EXISTS {this.QuoteIdentifier(name)}";

            // -----------------------------------------
            // Execute the command on the database
            // -----------------------------------------
            using (SQLiteCommand command = CreateCommand(sql.ToString()))
            {
                command.ExecuteNonQuery();
            }
        }

        #endregion Indexing Methods

        #region Helper Methods

        /// <summary>
        /// Creates a new command to be executed on the database
        /// </summary>
        public SQLiteCommand CreateCommand() => new SQLiteCommand(Connection);

        /// <summary>
        /// Creates a new command to be executed on the database
        /// </summary>
        /// <param name="queryString">The query string this command will use</param>
        public SQLiteCommand CreateCommand(string queryString) => new SQLiteCommand(queryString, Connection);

        /// <summary>
        /// Creates a DbParameter using the current Database engine's Parameter object
        /// </summary>
        /// <returns></returns>
        public SQLiteParameter CreateParameter() => new SQLiteParameter();

        /// <summary>
        /// Begins a new database transaction
        /// </summary>
        /// <returns></returns>
        public SQLiteTransaction BeginTransaction() => Connection.BeginTransaction();

        /// <summary>
        /// Converts attributes from an <see cref="SQLiteDataReader"/> to an Entity
        /// </summary>
        /// <param name="table">The <see cref="TableMapping"/> for this Entity</param>
        /// <param name="reader">The current, open DataReader object</param>
        /// <returns></returns>
        internal TEntity ConvertToEntity<TEntity>(TableMapping table, SQLiteDataReader reader)
        {
            // Use reflection to map the column name to the object Property
            TEntity entity = (TEntity)Activator.CreateInstance(table.EntityType, new object[] { });
            for (int i = 0; i < reader.FieldCount; ++i)
            {
                string attrName = reader.GetName(i);
                PropertyInfo property = table.GetAttribute(attrName).Property;

                if (property.PropertyType.IsEnum)
                {
                    var value = Enum.Parse(property.PropertyType, reader.GetValue(i).ToString());
                    property.SetValue(entity, value);
                }
                else
                {
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
                        case TypeCode.DateTime:
                            if (!reader.IsDBNull(i))
                                property.SetValue(entity, reader.GetDateTime(i));
                            break;
                        default:
                            // Correct DBNull values
                            object val = reader.GetValue(i);
                            if (val is DBNull)
                                continue;

                            property.SetValue(entity, val);
                            break;
                    }
                }
            }

            // Foreign keys!
            table.CreateRelationships(entity, this);

            // Add object
            return entity;
        }

        /// <summary>
        /// Converts a C# data type to a textual SQLite data type
        /// </summary>
        /// <param name="propertyType">The C# property type that we are converting to</param>
        /// <returns></returns>
        internal static SQLiteDataType GetSQLiteType(Type propertyType)
        {
            // Store enums as their underlying type
            if (propertyType.IsEnum)
                propertyType = Enum.GetUnderlyingType(propertyType);

            switch (Type.GetTypeCode(propertyType))
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Char:
                    return SQLiteDataType.INTEGER;
                case TypeCode.String:
                case TypeCode.DateTime:
                    return SQLiteDataType.TEXT;
                case TypeCode.Object:
                    return SQLiteDataType.BLOB;
                case TypeCode.Decimal:
                    return SQLiteDataType.NUMERIC;
                case TypeCode.Double:
                    return SQLiteDataType.REAL;
                default:
                    throw new NotSupportedException($"Invalid object type conversion to \"{propertyType.Name}\".");
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
        /// Takes an identifier and qoutes it if the name is a reserved keyword. Passing
        /// a prefixed identifier (ex: "table.column") is valid. The <see cref="IdentifierQuoteKind"/> 
        /// and <see cref="IdentifierQuoteMode"/> options are used when determining if the identifier
        /// needs to be quoted or not.
        /// </summary>
        /// <param name="value">The attribute name</param>
        /// <returns></returns>
        public string QuoteIdentifier(string value) => QuoteIdentifier(value, IdentifierQuoteMode, IdentifierQuoteKind);

        /// <summary>
        /// Takes an identifier and qoutes it if the name is a reserved keyword. Passing
        /// a prefixed identifier (ex: "table.column") is valid.
        /// </summary>
        /// <param name="value">The attribute name</param>
        /// <returns></returns>
        public static string QuoteIdentifier(string value, IdentifierQuoteMode mode, IdentifierQuoteKind kind)
        {
            // Lets make this simple and fast!
            if (mode == IdentifierQuoteMode.None) return value;

            // Split the value by the period seperator, and determine if any identifiers are a keyword
            var parts = value.Split('.');
            var hasKeyword = mode == IdentifierQuoteMode.All;
            if (mode == IdentifierQuoteMode.KeywordsOnly)
                hasKeyword = (parts.Length > 1) ? ContainsKeyword(parts) : IsKeyword(value);

            // Appy the quoting where needed..
            if (parts.Length > 1)
            {
                switch (mode)
                {
                    case IdentifierQuoteMode.All: return ApplyQuotes(parts, mode, kind);
                    case IdentifierQuoteMode.KeywordsOnly: return (hasKeyword) ? ApplyQuotes(parts, mode, kind) : value;
                    default: return value;
                }
            }
            else // Non-array
            {
                switch (mode)
                {
                    case IdentifierQuoteMode.All: return ApplyQuotes(value, kind);
                    case IdentifierQuoteMode.KeywordsOnly: return (hasKeyword) ? ApplyQuotes(value, kind) : value;
                    default: return value;
                }
            }
        }

        /// <summary>
        /// Performs the actual quoting of the indentifier. Passing a prefixed indentifier
        /// (ex: "table.column") is NOT valid, and should be passed to the 
        /// <see cref="ApplyQuotes(string[], IdentifierQuoteMode, IdentifierQuoteKind))"/> 
        /// method instead.
        /// </summary>
        private static string ApplyQuotes(string value, IdentifierQuoteKind kind)
        {
            // Don't escape wildcard
            if (value == "*") return value;

            // Apply quotes to value
            var chars = EscapeChars[kind];
            return $"{chars[0]}{value}{chars[1]}";
        }

        /// <summary>
        /// Applies quoting to each identifier parameter that needs it, based on the IdentifierQuoteMode,
        /// and chains the result back into a string.
        /// </summary>
        private static string ApplyQuotes(string[] values, IdentifierQuoteMode mode, IdentifierQuoteKind kind)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                // Do we need to apply quoting to this string?
                if (mode == IdentifierQuoteMode.All || (mode == IdentifierQuoteMode.KeywordsOnly && IsKeyword(values[i])))
                    builder.Append(ApplyQuotes(values[i], kind));
                else
                    builder.Append(values[i]);

                builder.AppendIf(i + 1 < values.Length, ".");
            }
            return builder.ToString();
        }

        /// <summary>
        /// Returns whether the specified value is an SQLite reserved keyword.
        /// Passing a prefixed attribute (ex: "table.attribute") is NOT valid, and should
        /// be passed to the <see cref="ContainsKeyword(string[])"/> method instead.
        /// </summary>
        /// <param name="value">The attribute name</param>
        /// <returns></returns>
        public static bool IsKeyword(string value)
        {
            return Keywords.Contains(value);
        }

        /// <summary>
        /// Returns whether any of the specified values is an SQLite reserved keyword.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        private static bool ContainsKeyword(string[] values)
        {
            foreach (var key in values)
                if (Keywords.Contains(key))
                    return true;

            return false;
        }

        #endregion Helper Methods

        #region Static Properties

        internal static IReadOnlyDictionary<IdentifierQuoteKind, char[]> EscapeChars = new Dictionary<IdentifierQuoteKind, char[]>()
        {
            { IdentifierQuoteKind.Default, new char[2] { '"', '"' } },
            { IdentifierQuoteKind.SingleQuotes, new char[2] { '\'', '\'' } },
            { IdentifierQuoteKind.SquareBrackets, new char[2] { '[', ']' } },
            { IdentifierQuoteKind.Accents, new char[2] { '`', '`' } },
        };

        /// <summary>
        /// Gets or sets the list of SQLite reserved keywords
        /// </summary>
        public static HashSet<string> Keywords = new HashSet<string>(new string[] 
            {
                "ABORT",
                "ACTION",
                "ADD",
                "AFTER",
                "ALL",
                "ALTER",
                "ANALYZE",
                "AND",
                "AS",
                "ASC",
                "ATTACH",
                "AUTOINCREMENT",
                "BEFORE",
                "BEGIN",
                "BETWEEN",
                "BY",
                "CASCADE",
                "CASE",
                "CAST",
                "CHECK",
                "COLLATE",
                "COLUMN",
                "COMMIT",
                "CONFLICT",
                "CONSTRAINT",
                "CREATE",
                "CROSS",
                "CURRENT_DATE",
                "CURRENT_TIME",
                "CURRENT_TIMESTAMP",
                "DATABASE",
                "DEFAULT",
                "DEFERRABLE",
                "DEFERRED",
                "DELETE",
                "DESC",
                "DETACH",
                "DISTINCT",
                "DROP",
                "EACH",
                "ELSE",
                "END",
                "ESCAPE",
                "EXCEPT",
                "EXCLUSIVE",
                "EXISTS",
                "EXPLAIN",
                "FAIL",
                "FOR",
                "FOREIGN",
                "FROM",
                "FULL",
                "GLOB",
                "GROUP",
                "HAVING",
                "IF",
                "IGNORE",
                "IMMEDIATE",
                "IN",
                "INDEX",
                "INDEXED",
                "INITIALLY",
                "INNER",
                "INSERT",
                "INSTEAD",
                "INTERSECT",
                "INTO",
                "IS",
                "ISNULL",
                "JOIN",
                "KEY",
                "LEFT",
                "LIKE",
                "LIMIT",
                "MATCH",
                "NATURAL",
                "NO",
                "NOT",
                "NOTNULL",
                "NULL",
                "OF",
                "OFFSET",
                "ON",
                "OR",
                "ORDER",
                "OUTER",
                "PLAN",
                "PRAGMA",
                "PRIMARY",
                "QUERY",
                "RAISE",
                "RECURSIVE",
                "REFERENCES",
                "REGEXP",
                "REINDEX",
                "RELEASE",
                "RENAME",
                "REPLACE",
                "RESTRICT",
                "RIGHT",
                "ROLLBACK",
                "ROW",
                "SAVEPOINT",
                "SELECT",
                "SET",
                "TABLE",
                "TEMP",
                "TEMPORARY",
                "THEN",
                "TO",
                "TRANSACTION",
                "TRIGGER",
                "UNION",
                "UNIQUE",
                "UPDATE",
                "USING",
                "VACUUM",
                "VALUES",
                "VIEW",
                "VIRTUAL",
                "WHEN",
                "WHERE",
                "WITH",
                "WITHOUT",
            }, 
            StringComparer.OrdinalIgnoreCase
        );

        #endregion
    }
}
