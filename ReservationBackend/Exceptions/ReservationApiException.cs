namespace ReservationBackend.Exceptions;

public sealed class ReservationApiException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }

    public ReservationApiException(int statusCode, string code, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }
}