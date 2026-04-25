
using System.Security.Claims;
using PBL3_DUTLibrary.Data;
using PBL3_DUTLibrary.Interfaces;
using PBL3_DUTLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace PBL3_DUTLibrary.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly LibraryContext _libraryContext;
        private readonly IHttpContextAccessor _contextAccessor;
        public UserRepository(LibraryContext libraryContext, IHttpContextAccessor contextAccessor)
        {
            _libraryContext = libraryContext;
            _contextAccessor = contextAccessor;
        }

        public WebUser GetCurrentUser()
        {
            var userid = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int id = Convert.ToInt32(userid);
            WebUser user = _libraryContext.WebUsers.Include(i => i.Books)
                .Include(i => i.Borrows).ThenInclude(w => w.Book)
                .FirstOrDefault(i => i.UserId == id);
            return user;
        }

        public bool Save()
        {
            var saved = _libraryContext.SaveChanges();
            return saved > 0 ? true : false;
        }

        public bool Update(WebUser user)
        {
            _libraryContext.Update(user);
            return Save();
        }

        public WebUser GetById(int id)
        {
            return _libraryContext.WebUsers.FirstOrDefault(i => i.UserId == id);
        }

        public bool WishlistDelete(int id)
        {
            int check = 0;
            WebUser user = GetCurrentUser();
            foreach(Book book in user.Books)
            {
                if (book.BookId == id)
                {
                    user.Books.Remove(book);
                    check = 1;
                    break;
                }
            }
            if (check == 0)
            {
                return false;
            }
            else
            {
                return Update(user);
            }
        }

        public bool AddWishList(int id)
        {
            WebUser user = GetCurrentUser();
            foreach(Book book in user.Books)
            {
                if (book.BookId == id)
                {
                    return false;
                }
            }
            Book newwl = _libraryContext.Books.FirstOrDefault(i => i.BookId == id);
            user.Books.Add(newwl);

            return Update(user);

        }

        public List<Borrow> GetReturnedBooksList(WebUser user)
        {
            List<Borrow> returned = new List<Borrow>();
            foreach(Borrow br in user.Borrows)
            {
                if (br.Status == 3)
                {
                    returned.Add(br);
                }
            }
            returned.Reverse();
            return returned;
        }

        public List<Borrow> GetBorrowingBooksList(WebUser user)
        {
            List<Borrow> borrowing = new List<Borrow>();
            foreach (Borrow br in user.Borrows)
            {
                if (br.Status != 3)
                {
                    br.ProlongRequests = _libraryContext.ProlongRequests.Where(p => p.BorrowId == br.BorrowId).ToList();
                    borrowing.Add(br);
                }
            }
            borrowing.Reverse();
            return borrowing;
        }
        // Phương thức để đếm số lượng sách có trong wishlist của mỗi user
        public int GetWishlistCount(int userId)
        {
            var user = _libraryContext.WebUsers
                .Include(u => u.Books)
                .FirstOrDefault(u => u.UserId == userId);
            return user?.Books.Count ?? 0;
        }
    }
}
