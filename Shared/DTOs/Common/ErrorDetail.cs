namespace Shared.DTOs.Common
{
    public class ErrorDetail
    {
        public ErrorCode Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Field { get; set; }
    }
}
