using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PBL3_DUTLibrary.Data;
using PBL3_DUTLibrary.Models;
using PBL3_DUTLibrary.Repository;
using System.Security.Claims;
using PBL3_DUTLibrary.Interfaces;
using PBL3_DUTLibrary.ViewModels;
using PBL3_DUTLibrary.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using PBL3_DUTLibrary.Interface;

namespace PBL3_DUTLibrary.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhotoService _photoService;
        private readonly IBookRepository _bookRepository;
        private readonly LibraryContext _libraryContext;
        private readonly IMyEmailSender _emailSender;
        public DashboardController(IUserRepository userRepository, IPhotoService photoService, IBookRepository bookRepository, 
            LibraryContext libraryContext, IMyEmailSender emailSender)
        {
            _userRepository = userRepository;
            _photoService = photoService;
            _bookRepository = bookRepository;
            _libraryContext = libraryContext;
            _emailSender = emailSender;
        }

        [Authorize]
        public IActionResult Index()
        {
            WebUser user = _userRepository.GetCurrentUser();
            UserProfileViewModel profileViewModel = new UserProfileViewModel
            {
                Image = user.Image,
                Username = user.Username,
                Email = user.Email,
                Sdt = user.Sdt,
                Books = user.Books.ToList()
            };
            profileViewModel.Borrowing = _userRepository.GetBorrowingBooksList(user);
            profileViewModel.Returned = _userRepository.GetReturnedBooksList(user);
            return View(profileViewModel);
        }

        public IActionResult EditUserProfile()
        {
            WebUser user = _userRepository.GetCurrentUser();
            EditUserProfileViewModel editVM = new EditUserProfileViewModel
            { 
                UserName = user.Username,
                Realname = user.RealName,
                sdt = user.Sdt
            };

            return View(editVM);
        }

        [HttpPost]
        public async Task<IActionResult> EditUserProfile(EditUserProfileViewModel editVM)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Failed to edit user");
                return View("EditUserProfile", editVM);
            }
            WebUser user = _userRepository.GetCurrentUser();
            if (editVM.Image != null)
            {
                if (user.Image != null)
                {
                    try
                    {
                        await _photoService.DeletePhotoAsync(user.Image);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Could not Delete Photo");
                        return View(editVM);
                    }
                } 
                var photoresult = await _photoService.AddPhotoAsync(editVM.Image);
                user.Image = photoresult.Url.ToString();
            }
            user.Username = editVM.UserName;
            user.RealName = editVM.Realname;
            user.Sdt = editVM.sdt;
            _userRepository.Update(user);
            return RedirectToAction("Index");
        }


        public async Task<IActionResult> WishlistDelete(int id)
        {
            _userRepository.WishlistDelete(id);
            return RedirectToAction("Index", "Cart");
        }

        [Authorize]
        public async Task<IActionResult> AddWishList(int id)
        {
            if (_userRepository.AddWishList(id))
            {
                TempData["AddWlStatus"] = "Success"; 
                TempData["Notification"] = "Add to WishList Successfully!";
                return RedirectToAction("DetailBooks", "Books", new { id = id });
            }
            else
            {
                TempData["AddWlStatus"] = "Failed";
                TempData["Notification"] = "Add to Wishlist failed!";
                return RedirectToAction("DetailBooks", "Books", new { id = id });
            }
        }

        [Authorize]
        public async Task<IActionResult> BorrowBook(int id)
        {
            Book book = _bookRepository.GetbyId(id);
            WebUser user = _userRepository.GetCurrentUser();
            int borrowing = 0;
            foreach (Borrow borrow1 in user.Borrows)
            {
                if (borrow1.Status != 3)
                {
                    borrowing++;
                    if (borrowing >= 5)
                    {
                        break;
                    }
                }
            }
            if (borrowing >= 5)
            {
                TempData["AddWlStatus"] = "Failed";
                TempData["Notification"] = "You have already borrowed 5 books!";
                return RedirectToAction("DetailBooks", "Books", new { id = id });
            }
            if (book.Available == 1)
            {
                BorrowBookViewModel bookViewModel = new BorrowBookViewModel
                {
                    Id = book.BookId,
                    Title = book.Title,
                    Author = book.Author
                };
                return View(bookViewModel);
            }
            else
            {
                TempData["BorrowStatus"] = "Failed";
                TempData["Notification"] = "Borrow failed!";
                return RedirectToAction("DetailBooks", "Books", new { id = id });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> BorrowBook(BorrowBookViewModel book)
        {
            int borrowid;
            Book book_con = _bookRepository.GetbyId((int)book.Id);
            if (_libraryContext.Borrows.Count() == 0)
            {
                borrowid = 0;
            }
            else
            {
                borrowid = _libraryContext.Borrows.Max(u => u.BorrowId) + 1;
            }
            WebUser user = _userRepository.GetCurrentUser();
            Borrow borrow = new Borrow
            {
                UserId = user.UserId,
                BookId = book.Id,
                BorrowId = borrowid,
                Time = DateTime.Now,
                Deadline = 10,
                Status = 0
            };
            user.Borrows.Add(borrow);
            if (_userRepository.Update(user))
            {
                book_con.Amount -= 1;
                if (book_con.Amount == 0)
                {
                    book_con.Available = 0;
                }
                else
                {
                    book_con.Available = 1;
                }
                _bookRepository.Update(book_con);
                return RedirectToAction("Index");
            }
            else
            {
                TempData["BorrowStatus"] = "Failed";
                TempData["Notification"] = "Borrow failed!";
                return RedirectToAction("DetailBooks", "Books", new { id = book.Id });
            }
            
        }

        public async Task<IActionResult> ChangePassword()
        {
            ChangePasswordViewModel ChangeVM = new ChangePasswordViewModel();
            return View(ChangeVM);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel ChangeVM)
        {
            WebUser user = _userRepository.GetCurrentUser();
            string email = user.Email;
            if (string.IsNullOrEmpty(ChangeVM.State))
            {
                ChangeVM.State = "Code Sent";
                Random rand = new Random();
                string code = rand.Next(100000, 999999).ToString();
                HttpContext.Session.SetString("ChangePWCode", code);
                ThreadPool.QueueUserWorkItem(state => SendVeriEmail(email));
                return View(ChangeVM);
            }
            else if (ChangeVM.State == "Code Sent")
            {
                string code = HttpContext.Session.GetString("ChangePWCode");
                if (code == ChangeVM.ConfirmCode)
                {
                    ChangeVM.State = "Change Password";
                    return View(ChangeVM);
                }
                else
                {
                    TempData["Error"] = "Wrong confirmation code. Please check your email again!";
                    return View(ChangeVM);
                }
            }
            else
            {
                if (!ModelState.IsValid)
                {
                    return View(ChangeVM);
                }
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(ChangeVM.Password);
                user.Password = hashedPassword;
                _userRepository.Update(user);
                return RedirectToAction("Index");
            }
            return View(ChangeVM);
        }
        protected void SendVeriEmail(string email)
        {
            string subject = "DUTLibrary Password Recovery Code";
            string message = "Your Verification Code is: " + HttpContext.Session.GetString("ChangePWCode");
            _emailSender.SendEmailAsync(email, subject, message);
        }

        public IActionResult sendRequest(SendRequestViewModel sendVM)
        {
            ProlongRequest req = new ProlongRequest
            {
                BorrowId = sendVM.id,
                Days = sendVM.Days,
                Reason = sendVM.Reason,
                Status = 0
            };
            _libraryContext.ProlongRequests.Add(req);
            _libraryContext.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
