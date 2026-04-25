using System.ComponentModel.DataAnnotations;

namespace PBL3_DUTLibrary.ViewModels
{
    public class EditUserProfileViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Username is required")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Realname is required")]
        public string Realname { get; set; }

        [DataType(DataType.PhoneNumber)]
        public int sdt { get; set; }

        public IFormFile? Image { get; set; }
    }
}