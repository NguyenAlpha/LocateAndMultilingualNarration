using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Api.Application.DTOs.StallMedia
{
    public class StallMediaUploadUpdateRequest
    {
        [Required(ErrorMessage = "ImageFile là bắt buộc")]
        public IFormFile ImageFile { get; set; } = null!;

        [MaxLength(256, ErrorMessage = "Caption tối đa 256 ký tự")]
        public string? Caption { get; set; }

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}
