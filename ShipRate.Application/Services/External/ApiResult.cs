namespace ShipRate.Application.Services.External
{
    public sealed record ApiResult<T>(
        bool Success,
        int Code,
        string? Message,
        T? Data)
    {
        public static ApiResult<T> Fail(string message, int code = 0)
            => new(false, code, message, default);
    }
}
