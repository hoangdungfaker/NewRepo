using System;
using System.Collections.Generic;

namespace PBL3_DUTLibrary.Models;

public partial class Loan
{
    public long LoanId { get; set; }

    public string? Name { get; set; }

    public long? Price { get; set; }
}
