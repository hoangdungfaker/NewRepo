using Microsoft.EntityFrameworkCore;
using PBL3_DUTLibrary.Data;
using PBL3_DUTLibrary.Interfaces;
using PBL3_DUTLibrary.Models;

namespace PBL3_DUTLibrary.Repository
{
    public class BorrowRepository : IBorrowRepository
    {
        long overduePrice;
        private readonly LibraryContext _context;
        public BorrowRepository(LibraryContext context)
        {
            _context = context;
            Loan overdue = _context.Loans.FirstOrDefault(x => x.Name == "Overdue Fee");
            overduePrice = overdue.Price == null ? 0 : (long)overdue.Price;
        }
        public bool DeleteBorrow(Borrow borrow)
        {
            _context.Borrows.Remove(borrow);
            return Save();
        }

        public List<Borrow> GetAllBorrows()
        {
            List<Borrow> borrows = _context.Borrows.ToList();
            borrows.Reverse();
            foreach(Borrow br in borrows)
            {
                br.Book = _context.Books.FirstOrDefault(b => b.BookId == br.BookId);
                br.User = _context.WebUsers.FirstOrDefault(u => u.UserId == br.UserId);
                br.ProlongRequests = _context.ProlongRequests.Where(p => p.BorrowId == br.BorrowId).ToList();
            }
            return borrows;
        }

        public List<Borrow> GetAllCompletedBorrow()
        {
            List<Borrow> borrows = _context.Borrows.Include(b => b.Book).Include(b => b.User).Where(b => b.Status == 3)
                .OrderByDescending(b => b.ReturnedTime)
                .ToList();
            foreach (Borrow br in borrows)
            {
                br.Book = _context.Books.FirstOrDefault(b => b.BookId == br.BookId);
                br.User = _context.WebUsers.FirstOrDefault(u => u.UserId == br.UserId);
                br.ProlongRequests = _context.ProlongRequests.Where(p => p.BorrowId == br.BorrowId).ToList();
            }
            return borrows;
        }

        public List<Borrow> GetAllNotReceivedBorrow()
        {
            List<Borrow> borrows = _context.Borrows.Include(b => b.Book).Include(b => b.User).Where(b => b.Status == 0).ToList();
            borrows.Reverse();
            foreach (Borrow br in borrows)
            {
                br.Book = _context.Books.FirstOrDefault(b => b.BookId == br.BookId);
                br.User = _context.WebUsers.FirstOrDefault(u => u.UserId == br.UserId);
                br.ProlongRequests = _context.ProlongRequests.Where(p => p.BorrowId == br.BorrowId).ToList();
                br.Fee = 0;
            }
            return borrows;
        }

        public List<Borrow> GetAllOverdueBorrow()
        {
            List<Borrow> borrows = _context.Borrows.Include(b => b.Book).Include(b => b.User).Where(b => b.Status == 2).ToList();
            borrows.Reverse();
            foreach (Borrow br in borrows)
            {
                br.Book = _context.Books.FirstOrDefault(b => b.BookId == br.BookId);
                br.User = _context.WebUsers.FirstOrDefault(u => u.UserId == br.UserId);
                br.ProlongRequests = _context.ProlongRequests.Where(p => p.BorrowId == br.BorrowId).ToList();
                int days = DateTime.Now.Subtract(br.Time).Days;
                if (days < br.Deadline)
                {
                    days = 0;
                }
                else
                {
                    days -= br.Deadline;
                }
                br.Fee = days * overduePrice;
            }
            return borrows;
        }

        public List<Borrow> GetAllReceivedBorrow()
        {
            List<Borrow> borrows = _context.Borrows.Include(b => b.Book).Include(b => b.User).Where(b => b.Status == 1).ToList();
            borrows.Reverse();
            foreach (Borrow br in borrows)
            {
                br.Book = _context.Books.FirstOrDefault(b => b.BookId == br.BookId);
                br.User = _context.WebUsers.FirstOrDefault(u => u.UserId == br.UserId);
                br.ProlongRequests = _context.ProlongRequests.Where(p => p.BorrowId == br.BorrowId).ToList();
                br.Fee = 0;
            }
            return borrows;
        }

        public List<Borrow> GetAllUnCompletedBorrow()
        {
            List<Borrow> borrows = _context.Borrows.Include(b => b.Book).Include(b => b.User).Where(b => b.Status != 3).ToList();
            borrows.Reverse();
            foreach (Borrow br in borrows)
            {
                br.Book = _context.Books.FirstOrDefault(b => b.BookId == br.BookId);
                br.User = _context.WebUsers.FirstOrDefault(u => u.UserId == br.UserId);
                br.ProlongRequests = _context.ProlongRequests.Where(p => p.BorrowId == br.BorrowId).ToList();
                int days = DateTime.Now.Subtract(br.Time).Days;
                if (days < br.Deadline)
                {
                    days = 0;
                }
                else
                {
                    days -= br.Deadline;
                }
                br.Fee = days * overduePrice;
            }
            return borrows;
        }

        public Borrow GetById(int id)
        {
            Borrow br = _context.Borrows.Include(b => b.Book).Include(b => b.User).FirstOrDefault(i => i.BorrowId == id);
            br.Book = _context.Books.FirstOrDefault(b => b.BookId == br.BookId);
            br.User = _context.WebUsers.FirstOrDefault(u => u.UserId == br.UserId);
            br.ProlongRequests = _context.ProlongRequests.Where(p => p.BorrowId == br.BorrowId).ToList();
            if (br.Status != 3)
            {
                int days = DateTime.Now.Subtract(br.Time).Days;
                if (days < br.Deadline)
                {
                    days = 0;
                }
                else
                {
                    days -= br.Deadline;
                }
                br.Fee = days * overduePrice;
            }
            return br;
        }

        public bool Save()
        {
            return _context.SaveChanges() > 0;
        }

        public bool Update(Borrow borrow)
        {
            _context.Update(borrow);
            return Save();
        }

        public List<ProlongRequest> GetAllProlongRequests()
        {
            List<ProlongRequest> prolongRequests = _context.ProlongRequests
                .Include(p => p.Borrow)
                .ThenInclude(b => b.User)
                .Include(p => p.Borrow)
                .ThenInclude(b => b.Book)
                .ToList();
            prolongRequests.Reverse();
            return prolongRequests;
        }

        public List<ProlongRequest> GetAllUnCompletedProlongRequests()
        {
            List<ProlongRequest> prolongRequests = _context.ProlongRequests
                .Include(p => p.Borrow)
                .ThenInclude(b => b.User)
                .Include(p => p.Borrow)
                .ThenInclude(b => b.Book)
                .Where(p => p.Status == 0)
                .ToList();
            prolongRequests.Reverse();
            return prolongRequests;
        }

        public List<ProlongRequest> GetAllCompletedProlongRequests()
        {
            List<ProlongRequest> prolongRequests = _context.ProlongRequests
                .Include(p => p.Borrow)
                .ThenInclude(b => b.User)
                .Include(p => p.Borrow)
                .ThenInclude(b => b.Book)
                .Where(p => p.Status == 1 || p.Status == 2)
                .ToList();
            prolongRequests.Reverse();
            return prolongRequests;
        }

        public ProlongRequest GetProlongRequestById(int id)
        {
            return _context.ProlongRequests
                .Include(p => p.Borrow)
                .ThenInclude(b => b.User)
                .Include(p => p.Borrow)
                .ThenInclude(b => b.Book)
                .FirstOrDefault(p => p.RequestId == id);
        }

        public bool Update(ProlongRequest prolongRequest)
        {
            _context.Update(prolongRequest);
            return Save();
        }
    }
}
