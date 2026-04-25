using System;
using System.Collections.Generic;

namespace PBL3_DUTLibrary.Models;

public partial class Borrow
{
    public int BorrowId { get; set; }

    public int? UserId { get; set; }

    public long? BookId { get; set; }

    public DateTime Time { get; set; }

    public int Deadline { get; set; }

    public int Status { get; set; }

    public long? Fee { get; set; }

    public DateTime? ReturnedTime { get; set; }

    public virtual Book? Book { get; set; }

    public virtual ICollection<ProlongRequest> ProlongRequests { get; set; } = new List<ProlongRequest>();

    public virtual WebUser? User { get; set; }
}
