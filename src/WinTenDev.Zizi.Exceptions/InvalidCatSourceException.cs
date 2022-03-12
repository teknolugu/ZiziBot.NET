using System;

namespace WinTenDev.Zizi.Exceptions;

public class InvalidCatSourceException : Exception
{
    public InvalidCatSourceException() : base()
    {
    }

    public InvalidCatSourceException(string message) : base("Invalid Cat Source. " + message)
    {
    }

    public InvalidCatSourceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}