using PBL3_DUTLibrary.Models;

namespace PBL3_DUTLibrary.ViewModels
{
    public class UserProfileViewModel
    {
        public string? Image {  get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public int Sdt {  get; set; }
        public List<Borrow> Borrowing = new List<Borrow>();
        public List<Borrow> Returned = new List<Borrow>();
        public List<Book> Books = new List<Book>();
    }
}
