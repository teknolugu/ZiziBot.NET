using System;
using System.Runtime.Serialization;

namespace WinTenDev.Zizi.Exceptions;

[Serializable()]
public class ConnectionStringNullOrEmptyException : Exception
{
    protected ConnectionStringNullOrEmptyException(
        SerializationInfo serializationInfo,
        StreamingContext context
    ) : base(serializationInfo, context)
    {
    }

    public ConnectionStringNullOrEmptyException(
        string message
    ) : base($"{message} Connection string must be set!")
    {
    }

    public ConnectionStringNullOrEmptyException() : base()
    {
    }

    public ConnectionStringNullOrEmptyException(
        string message,
        Exception innerException
    ) : base(message, innerException)
    {
    }
}