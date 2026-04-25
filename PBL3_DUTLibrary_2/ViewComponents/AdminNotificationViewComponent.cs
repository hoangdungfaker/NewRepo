using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PBL3_DUTLibrary.Interfaces;
using PBL3_DUTLibrary.ViewModels;

namespace PBL3_DUTLibrary.ViewComponents
{
	public class AdminNotificationViewComponent : ViewComponent
	{
		private readonly IBorrowRepository _borrowRepository;
		public AdminNotificationViewComponent(IBorrowRepository borrowRepository)
		{
			_borrowRepository = borrowRepository;
		}

		public IViewComponentResult Invoke()
		{
			var model = new AdminNotificationViewModel
			{
				NewBorrowRequests = _borrowRepository.GetAllNotReceivedBorrow(),
				NewProlongRequests = _borrowRepository.GetAllProlongRequests()
			};
			return View(model);
		}
	}
}
