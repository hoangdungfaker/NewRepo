using System;
using System.Collections.Generic;

namespace PBL3_DUTLibrary.Models;

public partial class WebUser
{
    public int UserId { get; set; }

    public string? Username { get; set; }

    public string? RealName { get; set; }

    public int Sdt { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? Image { get; set; }

    public string? Role { get; set; }

    public bool? Status { get; set; }

    public virtual ICollection<AccessHistory> AccessHistories { get; set; } = new List<AccessHistory>();

    public virtual ICollection<Borrow> Borrows { get; set; } = new List<Borrow>();

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
}
