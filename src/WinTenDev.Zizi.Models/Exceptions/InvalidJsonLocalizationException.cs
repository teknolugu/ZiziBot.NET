using System;
using System.Runtime.Serialization;

namespace WinTenDev.Zizi.Models.Exceptions;

[Serializable()]
public class InvalidJsonLocalizationException : Exception
{
    protected InvalidJsonLocalizationException(
        SerializationInfo serializationInfo,
        StreamingContext context
    ) : base(serializationInfo, context)
    {
    }

    public InvalidJsonLocalizationException() : base()
    {
    }

    public InvalidJsonLocalizationException(string message) : base(message)
    {
    }

    public InvalidJsonLocalizationException(
        string message,
        Exception innerException
    ) : base($"Error loading localization files {message}", innerException)
    {
    }

    public InvalidJsonLocalizationException(Exception innerException) :
        base("Error loading localization files", innerException)
    {
    }
}
