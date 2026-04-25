using System;
using System.Collections.Generic;

namespace PBL3_DUTLibrary.Models;

public partial class BookCopy
{
    public int BookCopyId { get; set; }

    public long BookId { get; set; }

    public string? Status { get; set; }

    public virtual Book Book { get; set; } = null!;
}
