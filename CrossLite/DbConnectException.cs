using System;
using System.Runtime.Serialization;

namespace CrossLite
{
    [Serializable]
    public class DbConnectException : Exception
    {
        public DbConnectException()
        {
        }

        public DbConnectException(string message) : base(message)
        {
        }

        public DbConnectException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DbConnectException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}