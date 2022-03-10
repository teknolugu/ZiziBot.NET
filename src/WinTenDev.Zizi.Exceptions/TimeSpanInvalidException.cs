using System;

namespace WinTenDev.Zizi.Exceptions;

public class TimeSpanInvalidException : Exception
{
    public TimeSpanInvalidException()
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