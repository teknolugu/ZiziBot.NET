using System;
using System.Runtime.Serialization;

namespace WinTenDev.Zizi.Models.Exceptions;

[Serializable()]
public class TimeZoneOffsetNotFoundException : Exception
{
    protected TimeZoneOffsetNotFoundException(
        SerializationInfo serializationInfo,
        StreamingContext context
    ) : base(serializationInfo, context)
    {
    }

    public TimeZoneOffsetNotFoundException() : base()
    {
    }

    public TimeZoneOffsetNotFoundException(string message) :
        base($"TimeZone with offset '{message}' not found")
    {
    }

    public TimeZoneOffsetNotFoundException(
        string message,
        Exception innerException
    ) : base(message, innerException)
    {
    }
}
