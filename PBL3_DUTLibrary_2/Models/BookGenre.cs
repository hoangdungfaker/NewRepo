using System;
using System.Collections.Generic;

namespace PBL3_DUTLibrary.Models;

public partial class BookGenre
{
    public int GenreId { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
}
