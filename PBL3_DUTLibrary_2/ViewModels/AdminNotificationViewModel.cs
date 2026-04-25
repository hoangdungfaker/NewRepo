using PBL3_DUTLibrary.Models;

namespace PBL3_DUTLibrary.ViewModels
{
	public class AdminNotificationViewModel
	{
		public List<Borrow> NewBorrowRequests { get; set; }
		public List<ProlongRequest> NewProlongRequests { get; set; }
	}
}
