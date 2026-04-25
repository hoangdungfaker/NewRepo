namespace PBL3_DUTLibrary.ViewModels
{
    public class AdminBookListViewModel
    {
        public long BookId { get; set; }
        public string? Title { get; set; }
        public string? Author { get; set; }
        public int TotalAmount { get; set; } // Renamed from Amount - Represents total physical copies
        public string? Image { get; set; }
        public string? Description { get; set; } // Thêm Description nếu cần

        public int Amount { get; set; } // Renamed from Available - Represents currently available copies

        // Property to display genres as a comma-separated string
        public string? GenresDisplay { get; set; }
    }
}