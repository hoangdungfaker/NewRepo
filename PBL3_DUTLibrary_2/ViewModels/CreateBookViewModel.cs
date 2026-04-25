using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace PBL3_DUTLibrary.ViewModels
{
    public class CreateBookViewModel
    {
        [Required(ErrorMessage = "Book title is required.")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Author name is required.")]
        [StringLength(100, ErrorMessage = "Author name cannot exceed 100 characters.")]
        public string Author { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Number of copies is required.")]
        [Range(1, 1000, ErrorMessage = "Amount must be between 1 and 1000.")]
        public int? Amount { get; set; } // Made nullable to match Book.Amount more closely if needed, but Range implies not null

        // No longer using BookImage here as it's handled by IFormFile in controller action

        // Changed from List<int> SelectedGenreIds to List<string> SelectedGenreNames
        public List<string>? SelectedGenreNames { get; set; } = new List<string>();
    }
}