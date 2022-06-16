using System;
using System.Runtime.Serialization;

namespace WinTenDev.Zizi.Models.Exceptions;

[Serializable()]
public class TimeSpanInvalidException : Exception
{
    public TimeSpanInvalidException()
    {
    }

    public TimeSpanInvalidException(
        SerializationInfo serializationInfo,
        StreamingContext streamingContext
    )
        : base(serializationInfo, streamingContext)
    {
    }

    public TimeSpanInvalidException(string message) : base(message)
    {
    }

    public TimeSpanInvalidException(
        string message,
        Exception innerException
    ) : base(message, innerException)
    {
    }
}
