using System;

public enum IOFailReason
{
    None = 0,
    InvalidUri,
    InvalidPath,
    InvalidResponse,
    DecodeFailed,
    NotFound,
    AccessDenied,
    LoadFailed,
    RegistrationFailed,
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

    public static IOResult Ok()
    {
        IOResult r = new IOResult();
        r.succeed = true;
        r.failReason = IOFailReason.None;
        return r;
    }

    public static IOResult Fail(IOFailReason reason, Exception e = null, long httpCode = 0)
    {
        IOResult r = new IOResult();
        r.succeed = false;
        r.failReason = reason;
        r.exception = e;
        r.httpResponseCode = httpCode;
        return r;
    }
}