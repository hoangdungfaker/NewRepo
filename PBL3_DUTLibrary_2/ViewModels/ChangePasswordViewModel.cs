using System.ComponentModel.DataAnnotations;

namespace PBL3_DUTLibrary.ViewModels
{
    public class ChangePasswordViewModel
    {
        public string? ConfirmCode { get; set; }
        [DataType(DataType.Password)]
        public string? Password { get; set; }
        [DataType (DataType.Password)]
        [Compare("Password", ErrorMessage ="Password doesn't match!")]
        public string? Confirm {  get; set; }
        public string? State { get; set; }
    }
}
