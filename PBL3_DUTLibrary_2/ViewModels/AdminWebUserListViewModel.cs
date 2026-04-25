using System;
using System.Collections.Generic;
using PBL3_DUTLibrary.Models;

namespace PBL3_DUTLibrary.ViewModels
{
    public class AdminWebUserListViewModel
    {
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? RealName { get; set; }
        public int? Sdt { get; set; } // Changed to nullable int to handle "no phone number" case
        public string? Email { get; set; }
        public string? Role { get; set; }
        public bool? Status { get; set; } // Matches WebUser.Status type (nullable bool)
        public string? Image { get; set; } // For displaying user image in the list

        // Added properties for borrowing information
        public int BorrowCount { get; set; } // Total number of books borrowed
        public int ActiveBorrowCount { get; set; } // Currently borrowed books count
        public DateTime? LastBorrowDate { get; set; } // Date of the last borrowing
    }
}