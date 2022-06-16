using System;
using System.Runtime.Serialization;

namespace WinTenDev.Zizi.Models.Exceptions;

[Serializable()]
public class InvalidCatSourceException : Exception
{
    public InvalidCatSourceException(
        SerializationInfo serializationInfo,
        StreamingContext context
    ) : base(serializationInfo, context)
    {
    }

    public InvalidCatSourceException() : base()
    {
    }

    public InvalidCatSourceException(string message) : base("Invalid Cat Source. " + message)
    {
    }

    public InvalidCatSourceException(
        string message,
        Exception innerException
    ) : base(message, innerException)
    {
    }
}