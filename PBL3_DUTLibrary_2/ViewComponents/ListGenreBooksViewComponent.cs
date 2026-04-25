using Microsoft.AspNetCore.Mvc;
using PBL3_DUTLibrary.Data;
using PBL3_DUTLibrary.ViewModels;
using PBL3_DUTLibrary.Models;

namespace PBL3_DUTLibrary.ViewComponents
{
	public class ListGenreBooksViewComponent : ViewComponent
	{
		private readonly LibraryContext dbLibrary;
		public ListGenreBooksViewComponent(LibraryContext dbLibrary) => this.dbLibrary = dbLibrary;
		public IViewComponentResult Invoke()
		{
			var data = dbLibrary.Genres
				.GroupBy(lo => lo.Genre1)
				.Select(g => new MenuLoaiVM
				{
					TenLoai = g.Key,
					SoLuong = g.Count()
				}).OrderBy(p => p.TenLoai);
			return View(data);
			//return View("ListGenre", data);
		}
	}
}
