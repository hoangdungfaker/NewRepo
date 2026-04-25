using System;
using System.Collections.Generic;

namespace PBL3_DUTLibrary.Models;

public partial class ProlongRequest
{
    public int RequestId { get; set; }

    public int? BorrowId { get; set; }

    public int? Days { get; set; }

    public string? Reason { get; set; }

    public int? Status { get; set; }

    public virtual Borrow? Borrow { get; set; }
}
