using System.ComponentModel.DataAnnotations;

namespace PBL3_DUTLibrary.ViewModels
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters.")]
        public string Username { get; set; }

        [StringLength(100, ErrorMessage = "Real name cannot exceed 100 characters.")]
        [Display(Name = "Full Name")]
        public string? RealName { get; set; }

        // Phone is now optional
        [RegularExpression(@"^(0[0-9]{9,10})$", ErrorMessage = "Invalid phone number format. Must be 10-11 digits starting with 0.")]
        [Display(Name = "Phone Number")]
        public string? Sdt { get; set; } // Changed to string for validation flexibility, convert to int in controller

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } // e.g., "Admin", "Librarian", "Member"

        [Display(Name = "User Status (Active)")]
        public bool Status { get; set; } = true; // Default to active
    }
}