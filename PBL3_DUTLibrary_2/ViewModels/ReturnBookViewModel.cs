using PBL3_DUTLibrary.Models;

namespace PBL3_DUTLibrary.ViewModels
{
    public class ReturnBookViewModel
    {
        public Borrow borrow { get; set; }
        public long Overdue { get; set; }
        public int days { get; set; }
        public long Lost { get; set; }
        public long Damage { get; set; }

        public long TotalFee { get; set; }

    }
}
