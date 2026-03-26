namespace Shared.DTOs.Languages
{
    public class LanguageDetailDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? DisplayName { get; set; }
        public string? FlagCode { get; set; }
        public bool IsActive { get; set; }
    }
}
