using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Web.Models
{
    public class StallMediaFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Stall là bắt buộc.")]
        [Display(Name = "Stall")]
        public Guid StallId { get; set; }

        [Display(Name = "Ảnh")]
        public IFormFile? ImageFile { get; set; }

        public string? CurrentImageUrl { get; set; }

        [MaxLength(256, ErrorMessage = "Caption tối đa 256 ký tự.")]
        [Display(Name = "Caption")]
        public string? Caption { get; set; }

        [Display(Name = "Thứ tự")]
        public int SortOrder { get; set; } = 0;

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;
    }
}
