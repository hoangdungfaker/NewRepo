using System.ComponentModel.DataAnnotations;

namespace PBL3_DUTLibrary.ViewModels
{
    public class SendRequestViewModel
    {
        [Required(ErrorMessage = "Id required")]
        public int id { get; set; }

        [Required(ErrorMessage = "Days required")]
        public int Days { get; set; }

        [Required(ErrorMessage = "Reason required")]
        [StringLength(1000, ErrorMessage = "Reason must be less than 1000 characters")]
        public string Reason { get; set; } = string.Empty;
    }
}
