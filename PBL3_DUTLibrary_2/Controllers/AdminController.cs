using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PBL3_DUTLibrary.Interfaces;
using PBL3_DUTLibrary.ViewModels;
using PBL3_DUTLibrary.Models;
using PBL3_DUTLibrary.Interface;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using PBL3_DUTLibrary.Data;


namespace PBL3_DUTLibrary.Controllers
{
    public class AdminController : Controller
    {
        private readonly IMyEmailSender _emailSender;
        private readonly IUserRepository _userRepository;
        private readonly IBorrowRepository _borrowRepository;
        private readonly LibraryContext _context;
        private readonly IBookRepository _bookRepository;
        public AdminController(IUserRepository userRepository, IBorrowRepository borrowRepository, IMyEmailSender emailSender, LibraryContext context, IBookRepository bookRepository)
        {
            _userRepository = userRepository;
            _borrowRepository = borrowRepository;
            _emailSender = emailSender;
            _context = context;
            _bookRepository = bookRepository;
        }



        private List<long> GetRevenueData(int year)
        {
            var revenueData = new List<long>();

            for (int i = 1; i <= 12; i++)
            {
                var totalRevenue = _context.Borrows
                    .Where(b => b.ReturnedTime.HasValue && b.ReturnedTime.Value.Year == year && b.ReturnedTime.Value.Month == i)
                    .Sum(b => (long)(b.Fee ?? 0));
                revenueData.Add(totalRevenue);
            }
            return revenueData;
        }
        //[Authorize(Roles = "Admin")]
        //public IActionResult ColumnChartRevenue()
        //{
        //	var revenueData = GetRevenueData(DateTime.Now.Year);
        //	ViewBag.RevenueData = revenueData;
        //	ViewBag.SelectedYear = DateTime.Now.Year;
        //	Console.WriteLine("Revenue: " + revenueData);
        //	return View(revenueData);
        //}

        [Authorize(Roles = "Admin")]
        public IActionResult Index(string startDate, string endDate)
        {
            var currentUsername = _userRepository.GetCurrentUser().Username;

            DateTime? start = null, end = null;
            if (!string.IsNullOrEmpty(startDate))
            {
                start = DateTime.ParseExact(startDate, "MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture);
            }
            if (!string.IsNullOrEmpty(endDate))
            {
                end = DateTime.ParseExact(endDate, "MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture);
            }
            var adminUsername = _context.WebUsers
                                .Where(u => u.Username == currentUsername && u.Role == "Admin")
                                .Select(u => u.Username)
                                .FirstOrDefault();

            // Thống kê tổng quan
            var stats = new
            {
                TotalBooks = _context.Books.Count(),
                AvailableCopies = _context.Books.Count(b => b.Available == 1),
                TotalMembers = _context.WebUsers.Count(),
                //ActiveLoan = _dbLibrary.Borrows.Count(b => b.ReturnedTime == null),
                TotalFee = _context.Borrows
                    .Where(f => (start == null || f.ReturnedTime >= start) && (end == null || f.ReturnedTime <= end))
                    .Sum(f => f.Fee ?? 0)
            };
            // thong ke active loan theo tháng
            var ActiveLoan = _context.Borrows
                .Count(b => b.ReturnedTime == null
                    && (start == null || b.Time >= start)
                    && (end == null || b.Time <= end));
            // Thống kê sách đã trả 
            var returnedLoan = _context.Borrows
                .Count(b => b.ReturnedTime != null
                    && (start == null || b.Time >= start)
                    && (end == null || b.Time <= end));

            // Thống kê top 5 sách được mượn nhiều nhất
            var popularBooks = _context.Books
                .Join(
                    _context.Borrows,
                    book => book.BookId,
                    borrow => borrow.BookId,
                    (book, borrow) => new { book.BookId, book.Title, book.Image, book.Author }
                )
                .GroupBy(x => new { x.BookId, x.Title, x.Image, x.Author })
                .Select(g => new
                {
                    g.Key.BookId,
                    g.Key.Title,
                    g.Key.Image,
                    g.Key.Author,
                    BorrowCount = g.Count()
                })
                .OrderByDescending(x => x.BorrowCount)
                .Take(5)
                .ToList();

            //  Thống kê top 5 độc giả mượn nhiều sách nhất
            var mostUserBorrows = _context.WebUsers
                .Join(
                    _context.Borrows,
                    user => user.UserId,
                    borrow => borrow.UserId,
                    (user, borrow) => new { user.UserId, user.Username, user.RealName, user.Email, user.Image }
            )
            .GroupBy(user => new { user.UserId, user.Username, user.RealName, user.Email, user.Image })
            .Select(g => new
            {
                g.Key.UserId,
                g.Key.Username,
                g.Key.RealName,
                g.Key.Email,
                g.Key.Image,
                BorrowCount = g.Count()
            })
            .OrderByDescending(x => x.BorrowCount)
            .Take(5)
            .ToList();

            // Lấy dữ liệu về các thể loại sách để đưa vào biểu đồ 
            var genreCounts = _context.Books
                .Join(
                    _context.Genres,
                    book => book.BookId,
                    genre => genre.BookId,
                    (book, genre) => new { genre.Genre1 }
                )
                .GroupBy(x => x.Genre1)
                .Select(g => new object[] { g.Key, g.Count() })
                .ToList();

            // Lấy số lượng thể loại có trong thư viện
            var totalGenreCount = _context.Genres
                .Select(g => g.Genre1)
                .Distinct()
                .Count();

            // Thống kê user đăng nhập nhiều nhất
            var mostAccessLogin = _context.AccessHistories
                .GroupBy(a => a.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    LoginCount = g.Count()
                })
                .OrderByDescending(x => x.LoginCount)
                .Take(5)
                .Join(
                    _context.WebUsers,
                    login => login.UserId,
                    user => user.UserId,
                    (login, user) => new
                    {
                        user.Image,
                        user.UserId,
                        user.Username,
                        user.Email,
                        user.RealName,
                        LoginCount = login.LoginCount
                    }
                )
                .ToList();

            // Thống kê số lượt mượn dựa theo thể loại sách 
            var borrowsByGenre = _context.Borrows
                .Where(b => b.BookId != null)
                .Join(
                    _context.Genres,
                    borrow => borrow.BookId,
                    genre => genre.BookId,
                    (borrow, genre) => new { genre.Genre1 }
                )
                .GroupBy(X => X.Genre1)
                .Select(g => new
                {
                    GenreName = g.Key,
                    BorrowCount = g.Count()
                })
                .OrderByDescending(x => x.BorrowCount)
                .ToList();

            var top5Genres = borrowsByGenre.Take(5).ToList();
            int otherCount = borrowsByGenre.Skip(5).Sum(x => x.BorrowCount);
            int totalCount = borrowsByGenre.Sum(x => x.BorrowCount);

            var genreBorrowData = top5Genres.Select(g =>
                new object[] { g.GenreName, g.BorrowCount, false, false }
            ).ToList();

            if (otherCount > 0)
            {
                genreBorrowData.Add(new object[] { "Other", otherCount, false, false });
            }



            var revenueData = GetRevenueData(DateTime.Now.Year);
            ViewBag.RevenueData = revenueData;
            ViewBag.SelectedYear = DateTime.Now.Year;

            ViewBag.AdminUsername = adminUsername ?? "";
            ViewBag.CurrentDate = DateTime.Now.ToString("dddd, MMMM dd, yyyy",
            new System.Globalization.CultureInfo("en-VN"));
            ViewBag.Stats = stats;
            ViewBag.PopularBooks = popularBooks;
            ViewBag.MostUserBorrows = mostUserBorrows;
            ViewBag.GenreCounts = genreCounts;
            ViewBag.ActiveLoan = ActiveLoan;
            ViewBag.ReturnedLoan = returnedLoan;
            ViewBag.SelectedStartDate = startDate;
            ViewBag.SelectedEndDate = endDate;
            //ViewBag.MonthName = new DateTime((int)year, month, 1).ToString("MMMM");
            ViewBag.TotalGenreCount = totalGenreCount;
            ViewBag.MostAccessLogin = mostAccessLogin;

            ViewBag.GenreBorrowData = genreBorrowData;
            ViewBag.TotalBorrows = totalCount;

            return View();
        }


        [Authorize(Roles = "Admin")]
        public IActionResult Home()
        {
            List<Borrow> borrow = _borrowRepository.GetAllBorrows();
            return View(borrow);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult BorrowReturn()
        {
            BorrowReturnViewModel brVM = new BorrowReturnViewModel();
            brVM.completed = _borrowRepository.GetAllCompletedBorrow();
            brVM.uncompleted = _borrowRepository.GetAllUnCompletedBorrow();
            brVM.uncompletedProlongRequests = _borrowRepository.GetAllUnCompletedProlongRequests();
            brVM.completedProlongRequests = _borrowRepository.GetAllCompletedProlongRequests();
            return View(brVM);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult BorrowSearch(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return RedirectToAction("BorrowReturn");
            }
            BorrowReturnViewModel brVM = new BorrowReturnViewModel();
            List<Borrow> completed = _borrowRepository.GetAllCompletedBorrow();
            List<Borrow> uncompleted = _borrowRepository.GetAllUnCompletedBorrow();
            List<ProlongRequest> completedProlongRequests = _borrowRepository.GetAllCompletedProlongRequests();
            List<ProlongRequest> uncompletedProlongRequests = _borrowRepository.GetAllUnCompletedProlongRequests();
            foreach (Borrow br in completed)
            {
                if (br.Book.Title.ToLower().Contains(query.ToLower()) || br.User.Email.ToLower().Contains(query.ToLower()))
                {
                    brVM.completed.Add(br);
                }
            }
            foreach (Borrow br in uncompleted)
            {
                if (br.Book.Title.ToLower().Contains(query.ToLower()) || br.User.Email.ToLower().Contains(query.ToLower()))
                {
                    brVM.uncompleted.Add(br);
                }
            }
            foreach (ProlongRequest p in completedProlongRequests)
            {
                if (p.Borrow.Book.Title.ToLower().Contains(query.ToLower()) || p.Borrow.User.Email.ToLower().Contains(query.ToLower()))
                {
                    brVM.completedProlongRequests.Add(p);
                }
            }
            foreach (ProlongRequest p in uncompletedProlongRequests)
            {
                if (p.Borrow.Book.Title.ToLower().Contains(query.ToLower()) || p.Borrow.User.Email.ToLower().Contains(query.ToLower()))
                {
                    brVM.uncompletedProlongRequests.Add(p);
                }
            }
            return View("BorrowReturn", brVM);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult ConfirmDeliver(int id)
        {
            Borrow borrow = _borrowRepository.GetById(id);
            if (borrow != null)
            {
                borrow.Status = 1;
                _borrowRepository.Update(borrow);
            }
            return RedirectToAction("BorrowReturn");
        }

        public IActionResult sendMail(SendEmailViewModel emailVM)
        {
            if (string.IsNullOrEmpty(emailVM.Email) || string.IsNullOrEmpty(emailVM.Subject) || string.IsNullOrEmpty(emailVM.Message))
            {
                return BadRequest("Email, Subject, and Message cannot be empty.");
            }
            ThreadPool.QueueUserWorkItem(state => SendVeriEmail(emailVM.Email, emailVM.Subject, emailVM.Message));
            TempData["Notification"] = "Email sent successfully!";
            return RedirectToAction("BorrowReturn");
        }

        protected void SendVeriEmail(string email, string subject, string message)
        {
            _emailSender.SendEmailAsync(email, subject, message);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult ReturnBook(int id)
        {
            Borrow br = _borrowRepository.GetById(id);
            Loan overdue = _context.Loans.FirstOrDefault(x => x.Name == "Overdue Fee");
            Loan lost = _context.Loans.FirstOrDefault(x => x.Name == "Lost Fee");
            Loan damage = _context.Loans.FirstOrDefault(x => x.Name == "Damage Fee");
            long overduePrice = overdue.Price == null ? 0 : (long)overdue.Price;
            long lostPrice = lost.Price == null ? 0 : (long)lost.Price;
            long damagePrice = damage.Price == null ? 0 : (long)damage.Price;
            ReturnBookViewModel rbVM = new ReturnBookViewModel()
            {
                borrow = br,
                Overdue = overduePrice,
                Lost = lostPrice,
                Damage = damagePrice,
                days = DateTime.Now.Subtract(br.Time).Days,
            };
            if (rbVM.days < br.Deadline)
            {
                rbVM.days = 0;
            }
            else
            {
                rbVM.days -= br.Deadline;
            }
            rbVM.TotalFee = br.Fee == null ? 0 : (long)br.Fee;
            return View(rbVM);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult ReturnBook(ReturnBookViewModel rbVM)
        {
            Borrow br = _borrowRepository.GetById(rbVM.borrow.BorrowId);
            br.Status = 3;
            br.Fee = rbVM.TotalFee;
            br.ReturnedTime = DateTime.Now.Date;
            bool lost = false;
            if (rbVM.TotalFee > (rbVM.days*rbVM.Overdue) + rbVM.Damage)
            {
                lost = true;
            }
            _borrowRepository.Update(br);
            Book book = _bookRepository.GetbyId((int)br.BookId);
            if (lost == false)
            {
                book.Available = 1;
                book.Amount += 1;
            }
            else
            {
                book.TotalAmount -= 1;
            }
            _bookRepository.Update(book);
            return RedirectToAction("BorrowReturn");
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AcceptRequest(int id)
        {
            ProlongRequest pr = _borrowRepository.GetProlongRequestById(id);
            if (pr != null)
            {
                pr.Status = 1;
                pr.Borrow.Deadline += (int)pr.Days;
                if (pr.Borrow.Status == 2)
                {
                    DateTime start = pr.Borrow.Time;
                    DateTime now = DateTime.Now;
                    DateTime end = start.AddDays((int)pr.Borrow.Deadline);
                    if (end > now)
                    {
                        pr.Borrow.Status = 1;
                    }
                }
                _borrowRepository.Update(pr);
            }
            return RedirectToAction("BorrowReturn");
        }

        [Authorize(Roles = "Admin")]
        public IActionResult RejectRequest(int id)
        {
            ProlongRequest pr = _borrowRepository.GetProlongRequestById(id);
            if (pr != null)
            {
                pr.Status = 2;
                _borrowRepository.Update(pr);
            }
            return RedirectToAction("BorrowReturn");
        }
    }
}
