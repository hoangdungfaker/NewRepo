using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // Required for IFormFile

namespace PBL3_DUTLibrary.ViewModels
{
    public class EditUserViewModel
    {
        [Required]
        public int UserId { get; set; }

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

        // Optional: For changing password
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long if provided.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password (leave blank to keep current)")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match if new password is provided.")]
        [Display(Name = "Confirm New Password")]
        public string? ConfirmNewPassword { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; }

        [Display(Name = "User Status (Active)")]
        public bool Status { get; set; } // Directly maps to WebUser.Status (nullable bool converted to bool)

        [Display(Name = "Profile Image")]
        public IFormFile? UserImage { get; set; } // For uploading a new image

        public string? ExistingImage { get; set; } // To store the path of the current image
    }
}