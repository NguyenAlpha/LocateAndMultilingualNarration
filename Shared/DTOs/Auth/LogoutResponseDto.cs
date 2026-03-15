namespace Shared.DTOs.Auth
{
    public class LogoutResponseDto
    {
        public bool Success { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}
