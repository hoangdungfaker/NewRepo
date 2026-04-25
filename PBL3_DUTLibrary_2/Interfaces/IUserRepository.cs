using PBL3_DUTLibrary.Data;
using PBL3_DUTLibrary.Models;

namespace PBL3_DUTLibrary.Interfaces
{
    public interface IUserRepository
    {
        WebUser GetCurrentUser();
        bool Update(WebUser user);
        bool Save();
        WebUser GetById(int id);
        bool WishlistDelete(int id);
        public bool AddWishList(int id);

        List<Borrow> GetReturnedBooksList(WebUser user);

        List<Borrow> GetBorrowingBooksList(WebUser user);
        int GetWishlistCount(int userId);

    }
}
