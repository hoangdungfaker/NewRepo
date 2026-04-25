using System;
using System.Collections.Generic;

namespace PBL3_DUTLibrary.Models;

public partial class Book
{
    public long BookId { get; set; }

    public string? Title { get; set; }

    public string? Author { get; set; }

    public long? Available { get; set; }

    public string? Description { get; set; }

    public long? Amount { get; set; }

    public string? Image { get; set; }

    public long? TotalAmount { get; set; }

    public virtual ICollection<BookCopy> BookCopies { get; set; } = new List<BookCopy>();

    public virtual ICollection<Borrow> Borrows { get; set; } = new List<Borrow>();

    public virtual ICollection<Genre> GenresNavigation { get; set; } = new List<Genre>();

    public virtual ICollection<BookGenre> Genres { get; set; } = new List<BookGenre>();

    public virtual ICollection<WebUser> Users { get; set; } = new List<WebUser>();
}
