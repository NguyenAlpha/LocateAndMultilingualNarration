namespace Api.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string TokenHash { get; set; } = null!;
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string? DeviceId { get; set; }
        public string? CreatedByIp { get; set; }
        public Guid? ReplacedByTokenId { get; set; }

        public User User { get; set; } = null!;
    }
}
