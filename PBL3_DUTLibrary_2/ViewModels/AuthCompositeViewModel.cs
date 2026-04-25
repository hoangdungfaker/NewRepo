using System.ComponentModel.DataAnnotations;

namespace PBL3_DUTLibrary.ViewModels
{
    public class AuthCompositeViewModel
    {
        public LoginViewModel LoginModel { get; set; } = new LoginViewModel();
        public RegisterViewModel RegisterModel { get; set; } = new RegisterViewModel();
        public ForgotPasswordViewModel ForgotPasswordModel { get; set; } = new ForgotPasswordViewModel();
    }
}
