using PBL3_DUTLibrary.Data;
using PBL3_DUTLibrary.Interfaces;
using PBL3_DUTLibrary.Models;

namespace PBL3_DUTLibrary.Repository
{
    public class BookRepository : IBookRepository
    {
        private readonly LibraryContext _libraryContext;
        public BookRepository(LibraryContext libraryContext)
        {
            _libraryContext = libraryContext;
        }

        public Book GetbyId(int id)
        {
            return _libraryContext.Books.FirstOrDefault(i => i.BookId == id);
        }

        public bool Save()
        {
            var saved = _libraryContext.SaveChanges();
            return saved > 0 ? true : false;
        }

        public bool Update(Book book)
        {
            _libraryContext.Update(book);
            return Save();
        }

        public List<Book> RandomBooks (int i)
        {
            var books = _libraryContext.Books.OrderBy(b => Guid.NewGuid()).Take(i).ToList();
            return books;
        }
    }
}
