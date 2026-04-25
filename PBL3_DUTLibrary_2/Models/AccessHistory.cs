using System;
using System.Collections.Generic;

namespace PBL3_DUTLibrary.Models;

public partial class AccessHistory
{
    public long AccessId { get; set; }

    public int UserId { get; set; }

    public DateTime? LoginTime { get; set; }

    public virtual WebUser User { get; set; } = null!;
}
