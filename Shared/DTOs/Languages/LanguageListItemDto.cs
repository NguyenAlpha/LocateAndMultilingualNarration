namespace Shared.DTOs.Languages
{
    public class LanguageListItemDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}
