using Microsoft.AspNetCore.Mvc;
using PBL3_DUTLibrary.Data;
using PBL3_DUTLibrary.Models;

namespace PBL3_DUTLibrary.Controllers
{
	public class PrincipleController : Controller
	{
		private readonly LibraryContext _libraryContext;
		public PrincipleController(LibraryContext libraryContext)
		{
			_libraryContext = libraryContext;
		}

		public IActionResult Index()
		{
			// Lấy giá trị của bảng giá tiền
			var overdueFee = _libraryContext.Loans.FirstOrDefault(l => l.Name == "Overdue Fee")?.Price ?? 0;
			var damageFee = _libraryContext.Loans.FirstOrDefault(l => l.Name == "Damage Fee")?.Price ?? 0;
			var lostFee = _libraryContext.Loans.FirstOrDefault(l => l.Name == "Lost Fee")?.Price ?? 0;
			ViewBag.OverdueFee = overdueFee;
			ViewBag.DamageFee = damageFee;
			ViewBag.LostFee = lostFee;
			return View();
		}
	}
}
