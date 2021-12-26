using System;

namespace WinTenDev.Zizi.Exceptions;

[Serializable()]
public class ConnectionStringNullOrEmptyException : Exception
{
    public ConnectionStringNullOrEmptyException(string message) : base($"{message} Connection string must be set!")
    {
    }

    public ConnectionStringNullOrEmptyException() : base()
    {
    }

    public ConnectionStringNullOrEmptyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}