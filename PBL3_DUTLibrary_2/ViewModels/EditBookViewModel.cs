using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace PBL3_DUTLibrary.ViewModels
{
    public class EditBookViewModel
    {
        [Required]
        public long BookId { get; set; }

        [Required(ErrorMessage = "Book title is required.")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Author name is required.")]
        [StringLength(100, ErrorMessage = "Author name cannot exceed 100 characters.")]
        public string Author { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Number of copies is required.")]
        [Range(1, 1000, ErrorMessage = "Amount must be between 1 and 1000.")]
        public int? Amount { get; set; }

        // Image path is not directly part of the edit form data,
        // it's handled by IFormFile if provided in the action.
        // public string? BookCover { get; set; } 

        // Changed from List<int> SelectedGenreIds to List<string> SelectedGenreNames
        public List<string>? SelectedGenreNames { get; set; } = new List<string>();
    }
}