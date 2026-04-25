using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PBL3_DUTLibrary.Models;
using Microsoft.AspNetCore.Identity;
using PBL3_DUTLibrary.Data;
using PBL3_DUTLibrary.Models;
using PBL3_DUTLibrary.Interfaces;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace PBL3_DUTLibrary.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly LibraryContext _context;
        private readonly IBookRepository _bookRepository;
        private readonly IUserRepository _userRepository;
        private readonly IFeedbackService _feedbackService;

        public HomeController(ILogger<HomeController> logger, LibraryContext context, IBookRepository bookRepository, IUserRepository userRepository, IFeedbackService feedbackService)
        {
            _logger = logger;
            _context = context;
            _bookRepository = bookRepository;
            _userRepository = userRepository;
            _feedbackService = feedbackService;
        }

        public IActionResult CheckBorrow()
        {
            List<Borrow> borrows = _context.Borrows.ToList();
            foreach (Borrow br in borrows)
            {
                if (br.Status == 3 || br.Status == 2)
                {
                    continue;
                }
                DateTime Start = br.Time.Date;
                DateTime End = br.Time.AddDays(br.Deadline + 1);
                DateTime now = DateTime.Now;
                if (now.Subtract(Start).Days >= 5 && br.Status == 0)
                {
                    br.Status = 3;
                    Book book = _bookRepository.GetbyId((int)br.BookId);
                    book.Amount += 1;
                    book.Available = 1;
                    _context.Books.Update(book);
                    _context.Borrows.Update(br);
                    _context.SaveChanges();
                }
                else if (now > End)
                {
                    if (br.Status == 1)
                    {
                        br.Status = 2;
                        _context.Borrows.Update(br);
                        _context.SaveChanges();
                    }
                }
            }
            WebUser curUser = _userRepository.GetCurrentUser();
            if (curUser != null)
            {
                if (curUser.Role == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }
            }
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Index()
        {
            var books = _bookRepository.RandomBooks(8);
            var feedbacks = await _feedbackService.GetAllFeedback();
            ViewBag.Feedbacks = feedbacks;
            return View(books);
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFeedback(string feedback)
        {
            if (string.IsNullOrWhiteSpace(feedback))
            {
                TempData["Error"] = "Feedback cannot be empty.";
                return RedirectToAction("Index");
            }
            var user = _userRepository.GetCurrentUser();
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }
            var feedbackObj = new FeedbackCustomers
            {
                name = user.RealName,
                email = user.Email,
                profilePicture = user.Image,
                feedback = feedback,
                username = user.Username,
                CreatedAt = DateTime.UtcNow
            };
            await _feedbackService.AddFeedback(feedbackObj);
            TempData["Success"] = "Thank you for your feedback";
            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
