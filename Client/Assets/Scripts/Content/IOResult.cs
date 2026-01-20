using System;

public enum IOFailReason
{
    None = 0,
    InvalidUri,
    InvalidPath,
    NotFound,
    AccessDenied,
    NetworkError,
    SaveFailed,
    Canceled,
    Unknown,
}

public sealed class IOResult
{
    public bool succeed;
    public long httpResponseCode;
    public IOFailReason failReason;
    public Exception exception;

    public void Clear()
    {
        succeed = false;
        httpResponseCode = 0;
        failReason = IOFailReason.None;
        exception = null;
    }
}