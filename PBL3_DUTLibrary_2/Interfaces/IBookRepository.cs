using PBL3_DUTLibrary.Models;

namespace PBL3_DUTLibrary.Interfaces
{
    public interface IBookRepository
    {
        Book GetbyId(int id);
        bool Update(Book book);
        bool Save();

        List<Book> RandomBooks(int i);
    }
}
