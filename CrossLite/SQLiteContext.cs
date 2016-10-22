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
        /// Gets or sets the default <see cref="CrossLite.AttributeQuoteMode"/> for queries. New instances of
        /// <see cref="SQLiteContext"/> with automatically dfefault to this value.
        /// </summary>
        public static AttributeQuoteMode DefaultAttributeQuoteMode { get; set; } = AttributeQuoteMode.None;

        /// <summary>
        /// Gets or sets the default <see cref="CrossLite.AttributeQuoteKind"/> for queries. New instances of
        /// <see cref="SQLiteContext"/> with automatically dfefault to this value.
        /// </summary>
        public static AttributeQuoteKind DefaultAttributeQuoteKind { get; set; } = AttributeQuoteKind.Default;

        /// <summary>
        /// The database connection
        /// </summary>
        public SQLiteConnection Connection { get; protected set; }

        /// <summary>
        /// Indicates whether the disposed method was called
        /// </summary>
        protected bool IsDisposed = false;

        /// <summary>
        /// Gets or sets the <see cref="CrossLite.AttributeQuoteMode"/> this instance will use for queries
        /// </summary>
        public AttributeQuoteMode AttributeQuoteMode { get; set; } = DefaultAttributeQuoteMode;

        /// <summary>
        /// Gets or sets the <see cref="CrossLite.AttributeQuoteKind"/> this instance will use for queries
        /// </summary>
        public AttributeQuoteKind AttributeQuoteKind { get; set; } = DefaultAttributeQuoteKind;

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
                    DbParameter Param = this.CreateParameter();
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
                    DbParameter Param = this.CreateParameter();
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
                    DbParameter Param = this.CreateParameter();
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
                SQLiteParameter Param = this.CreateParameter();
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
                SQLiteParameter Param = this.CreateParameter();
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
                    string value = reader.GetString(i);
                    object val = Enum.Parse(property.PropertyType, value);
                    property.SetValue(entity, val);
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
                        default:
                            // Correct DBNull values
                            object val = reader.GetValue(i);
                            if (val is DBNull)
                                val = String.Empty;

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
        /// <param name="propertyType"></param>
        /// <returns></returns>
        internal static SQLiteDataType GetSQLiteType(Type propertyType)
        {
            if (propertyType.IsEnum)
            {
                return SQLiteDataType.TEXT;
            }

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
        /// Takes an attribute name and qoutes it if the name is a reserved keyword. Passing
        /// a prefixed attribute (ex: "table.attribute") is valid. The <see cref="AttributeQuoteKind"/> 
        /// and <see cref="AttributeQuoteMode"/> options are used.
        /// </summary>
        /// <param name="value">The attribute name</param>
        /// <returns></returns>
        public string QuoteAttribute(string value)
        {
            // Configuration setting
            return QuoteKeyword(value, AttributeQuoteMode, AttributeQuoteKind);
        }

        /// <summary>
        /// Takes an attribute name and qoutes it if the name is a reserved keyword. Passing
        /// a prefixed attribute (ex: "table.attribute") is valid. The <see cref="DefaultAttributeQuoteKind"/> 
        /// and <see cref="DefaultAttributeQuoteMode"/> options are used.
        /// </summary>
        /// <param name="value">The attribute name</param>
        /// <returns></returns>
        public static string QuoteKeyword(string value)
        {
            // Global configuration for escaping for now...
            return QuoteKeyword(value, DefaultAttributeQuoteMode, DefaultAttributeQuoteKind);
        }

        /// <summary>
        /// Takes an attribute name and qoutes it if the name is a reserved keyword. Passing
        /// a prefixed attribute (ex: "table.attribute") is valid.
        /// </summary>
        /// <param name="value">The attribute name</param>
        /// <returns></returns>
        public static string QuoteKeyword(string value, AttributeQuoteMode mode, AttributeQuoteKind kind)
        {
            // Lets make this simple and fast!
            if (mode == AttributeQuoteMode.None) return value;

            // Split the value by the period seperator, and determine if any identifiers are a keyword
            var parts = value.Split('.');
            var hasKeyword = (parts.Length > 1) ? ContainsKeyword(parts) : IsKeyword(value);

            // Appy the quoting where needed..
            if (parts.Length > 1)
            {
                switch (mode)
                {
                    case AttributeQuoteMode.All: return ApplyQuotes(parts, mode, kind);
                    case AttributeQuoteMode.KeywordsOnly: return (hasKeyword) ? ApplyQuotes(parts, mode, kind) : value;
                    default: return value;
                }
            }
            else // Non-array
            {
                switch (mode)
                {
                    case AttributeQuoteMode.All: return ApplyQuote(value, kind);
                    case AttributeQuoteMode.KeywordsOnly: return (hasKeyword) ? ApplyQuote(value, kind) : value;
                    default: return value;
                }
            }
        }

        /// <summary>
        /// Performs the actual quoting of the attribute. Passing a prefixed attribute 
        /// (ex: "table.attribute") is NOT valid, and should be passed to the 
        /// <see cref="ApplyQuotes(string[], AttributeQuoteMode, AttributeQuoteKind))"/> 
        /// method instead.
        /// </summary>
        private static string ApplyQuote(string value, AttributeQuoteKind kind)
        {
            var chars = EscapeChars[kind];
            return $"{chars[0]}{value}{chars[1]}";
        }

        /// <summary>
        /// Applies quoting to each attribute parameter that needs it, based on the AttributeQuoteMode,
        /// and chains the result back into a string.
        /// </summary>
        private static string ApplyQuotes(string[] values, AttributeQuoteMode mode, AttributeQuoteKind kind)
        {
            var chars = EscapeChars[kind];
            var builder = new StringBuilder().Append(chars[0]);
            for (int i = 0; i < values.Length; i++)
            {
                // Do we need to apply quoting to this string?
                if (mode == AttributeQuoteMode.All || (mode == AttributeQuoteMode.KeywordsOnly && IsKeyword(values[i])))
                    builder.Append($"{chars[0]}{values[i]}{chars[1]}");
                else
                    builder.Append(values[i]);

                builder.AppendIf(i + 1 == values.Length, $"{chars[1]}.{chars[0]}", String.Empty);
            }
            return builder.Append(chars[1]).ToString();
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
            return Keywords.FindIndex(x => x.Equals(value, StringComparison.OrdinalIgnoreCase)) >= 0;
        }

        /// <summary>
        /// Returns whether any of the specified values is an SQLite reserved keyword.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        private static bool ContainsKeyword(string[] values)
        {
            foreach (var key in values)
                if (Keywords.FindIndex(x => x.Equals(key, StringComparison.OrdinalIgnoreCase)) >= 0)
                    return true;

            return false;
        }

        #endregion Helper Methods

        #region Static Properties

        internal static IReadOnlyDictionary<AttributeQuoteKind, char[]> EscapeChars = new Dictionary<AttributeQuoteKind, char[]>()
        {
            { AttributeQuoteKind.Default, new char[2] { '"', '"' } },
            { AttributeQuoteKind.SingleQuotes, new char[2] { '\'', '\'' } },
            { AttributeQuoteKind.SquareBrackets, new char[2] { '[', ']' } },
            { AttributeQuoteKind.Accents, new char[2] { '`', '`' } },
        };

        /// <summary>
        /// Gets or sets the list of SQLite reserved keywords
        /// </summary>
        public static List<string> Keywords = new List<string>(new string[] 
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
        });

        #endregion
    }
}
