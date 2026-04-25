using System.ComponentModel.DataAnnotations;

namespace PBL3_DUTLibrary.ViewModels
{
    public class SendEmailViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Subject is required.")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Message is required.")]
        public string Message { get; set; }
    }
}
