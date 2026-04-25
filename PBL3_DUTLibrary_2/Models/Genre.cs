using System;
using System.Collections.Generic;

namespace PBL3_DUTLibrary.Models;

public partial class Genre
{
    public long BookId { get; set; }

    public string Genre1 { get; set; } = null!;

    public virtual Book Book { get; set; } = null!;
}
