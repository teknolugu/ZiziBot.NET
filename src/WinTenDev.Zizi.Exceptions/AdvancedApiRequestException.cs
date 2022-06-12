using System;
using System.Runtime.Serialization;

namespace WinTenDev.Zizi.Exceptions;

[Serializable()]
public class AdvancedApiRequestException : Exception
{
    protected AdvancedApiRequestException(
        SerializationInfo serializationInfo,
        StreamingContext context
    ) : base(serializationInfo, context)
    {
    }

    public AdvancedApiRequestException() : base()
    {
    }

    public AdvancedApiRequestException(string message) : base(message)
    {
    }

    public AdvancedApiRequestException(
        string message,
        Exception innerException
    ) : base($"BadRequest: {message}", innerException)
    {

    }
}
