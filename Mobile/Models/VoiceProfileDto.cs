using System;
using System.Collections.Generic;
using System.Text;
namespace Mobile.Models
{
    public class VoiceProfileDto
    {
        public Guid Id { get; set; }
        public Guid LanguageId { get; set; }

        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? VoiceName { get; set; }           // Quan trọng nhất khi gọi TTS
        public string? Style { get; set; }
        public string? Role { get; set; }
        public string? Provider { get; set; }

        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
        public int Priority { get; set; }

        // Thuộc tính hỗ trợ hiển thị
        public string DisplayText => IsDefault
            ? $"{DisplayName} (Mặc định)"
            : DisplayName;
    }
}