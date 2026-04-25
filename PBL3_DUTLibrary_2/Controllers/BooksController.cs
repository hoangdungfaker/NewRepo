using Microsoft.AspNetCore.Mvc;
using PBL3_DUTLibrary.Data;
using PBL3_DUTLibrary.Models;
using System.Data.Entity;
using X.PagedList;
using X.PagedList.Extensions;

namespace PBL3_DUTLibrary.Controllers
{
    public class BooksController : Controller
    {
        private readonly LibraryContext dbLibrary;

        public BooksController(LibraryContext dbLibrary)
        {
            this.dbLibrary = dbLibrary;
        }
		//public IActionResult Index(List<string> loai, int? page)
		public IActionResult Index(List<string> loai, int? page)
		{
			int pageSize = 15;
			int pageNumber = (page ?? 1);

			var books = dbLibrary.Books.AsQueryable();

			if (loai != null && loai.Any())
			{
				books = books.Where(p => dbLibrary.Genres
					.Any(g => g.BookId == p.BookId && loai.Contains(g.Genre1)));
			}

			var result = books.Select(p => new Book
			{
				BookId = p.BookId,
				Title = p.Title,
				Image = p.Image,
				Author = p.Author,
				Available = p.Available
			});

			// Áp dụng phân trang
			var pagedBooks = result.ToPagedList(pageNumber, pageSize); 

			ViewBag.Loai = loai; 
			return View(pagedBooks); 
		}

		public IActionResult Search(string? query, List<string> loai, int? page)
		{
			int pageSize = 15;
			int pageNumber = (page ?? 1);
			var book = dbLibrary.Books.AsQueryable();
			//bool noResults = false; // Biến để kiểm tra nếu không tìm được sách
			if (loai != null && loai.Any())
			{
				book = book.Where(p => dbLibrary.Genres
							.Any(g => g.BookId == p.BookId && loai.Contains(g.Genre1)));
			}
			if (!string.IsNullOrWhiteSpace(query))
			{
				book = book.Where(p => p.Title.Contains(query));
			}
			var results = book.Select(p => new Book
			{
				BookId = p.BookId,
				Title = p.Title,
				Image = p.Image,
				Author = p.Author,
				Available = p.Available
			});

			//if (results.Count == 0)
			//{
			//	noResults = true;
			//}
			// Truyen result va noresult den view
			//ViewBag.NoResults = noResults;
			//ViewBag.Query = query;

			var pagedBooks = results.ToPagedList(pageNumber, pageSize);
			ViewBag.Query = query;
			ViewBag.Loai = loai;
			return View(pagedBooks);
		}

		public IActionResult PhanTrang(int? page)
		{
			int pageSize = 10;
			int pageNumber = (page ?? 1);

			var books = dbLibrary.Books
				.OrderBy(b => b.Title)
				.Select(p => new Book
				{
					BookId = p.BookId,
					Title = p.Title,
					Image = p.Image,
					Author = p.Author,
					Available = p.Available
				});

			// Áp dụng phân trang
			var pagedBooks = books.ToPagedList(pageNumber, pageSize);
			return View(pagedBooks);
		}

		public IActionResult DetailBooks(int id)
		{
			var data = dbLibrary.Books
				.Include(p => p.Genres)
				.SingleOrDefault(p => p.BookId == id);
			if (data == null)
			{
				//TempData["Message"] = "Không tìm thấy sách";
				return Redirect("/404");
			}

 			return View(data);
		}
	}
}
