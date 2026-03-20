namespace Shared.DTOs.Users
{
    public class VisitorProfileDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid LanguageId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
