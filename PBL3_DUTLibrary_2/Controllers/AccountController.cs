using PBL3_DUTLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using PBL3_DUTLibrary.Data;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using PBL3_DUTLibrary.ViewModels;

//namespace PBL3_DUTLibrary.Controllers
//{
//    public class AccountController : Controller
//    {
//        private readonly LibraryContext _libraryContext;
//        public AccountController(LibraryContext libraryContext)
//        {
//            _libraryContext = libraryContext;
//        }
//        public IActionResult Login()
//        {
//            var loginVM = new LoginViewModel();
//            return View(loginVM);
//        }

//        [HttpPost]
//        public async Task<IActionResult> Login(LoginViewModel LoginVM)
//        {
//            if (!ModelState.IsValid)
//            {
//                return View(LoginVM);
//            }
//            WebUser user = _libraryContext.WebUsers.FirstOrDefault(i => i.Email == LoginVM.email);
//            if (user == null)
//            {
//                TempData["Error"] = "Wrong credentials. Please, try again";
//                return View(LoginVM);
//            }
//            if (user.Password != LoginVM.password)
//            {
//                TempData["Error"] = "Wrong credentials. Please, try again";
//                return View(LoginVM);
//            }
//            TempData.Remove("Error");
//            var claims = new List<Claim>
//            {
//                new Claim(ClaimTypes.Name, LoginVM.email),
//                new Claim(ClaimTypes.Role, "User"),
//                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
//            };
//            var claimsIdentity = new ClaimsIdentity(
//            claims, CookieAuthenticationDefaults.AuthenticationScheme);
//            var authProperties = new AuthenticationProperties
//            {
//                IsPersistent = true // Set to true to keep the session alive after closing the browser
//            };
//            await HttpContext.SignInAsync(
//           CookieAuthenticationDefaults.AuthenticationScheme,
//           new ClaimsPrincipal(claimsIdentity),
//           authProperties);
//            AccessHistory ah = new AccessHistory();
//            if (_libraryContext.AccessHistories.Count() == 0)
//            {
//                ah.AccessId = 0;
//            }
//            else
//            {
//                long max_id = _libraryContext.AccessHistories.Max(u => u.AccessId);
//                ah.AccessId = max_id + 1;
//            }
//            ah.UserId = user.UserId;
//            ah.LoginTime = DateTime.Now;
//            _libraryContext.AccessHistories.Add(ah);
//            _libraryContext.SaveChanges();
//            return RedirectToAction("Index", "Books");

//        }

//        public async Task<IActionResult> Logout()
//        {
//            await HttpContext.SignOutAsync(
//        CookieAuthenticationDefaults.AuthenticationScheme);
//            return RedirectToAction("Index", "Books");
//        }
//    }
//}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using BCrypt.Net; // Sử dụng BCrypt.Net-Next
using PBL3_DUTLibrary.Data;
using PBL3_DUTLibrary.Models;
using PBL3_DUTLibrary.ViewModels;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using PBL3_DUTLibrary.Interface;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;

namespace PBL3_DUTLibrary.Controllers
{
    public class AccountController : Controller
    {
        private readonly LibraryContext _libraryContext;
        private readonly IMyEmailSender _emailSender;

        public AccountController(LibraryContext libraryContext, IMyEmailSender emailSender)
        {
            _libraryContext = libraryContext;
            _emailSender = emailSender;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View(); // Sẽ load Views/Account/Login.cshtml
        }

        public IActionResult LoginWithGoogle(string returnUrl = "/Home/Index")
        {
            var redirectUrl = Url.Action("GoogleResponse", "Account", new { ReturnUrl = returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (result.Succeeded)
            {
                WebUser user = _libraryContext.WebUsers.FirstOrDefault(u => u.Email == result.Principal.FindFirst(ClaimTypes.Email).Value);
                if (user == null)
                {
                    user = new WebUser();
                    user.UserId = _libraryContext.WebUsers.Count() == 0 ? 0 : _libraryContext.WebUsers.Max(u => u.UserId) + 1;
                    user.Username = result.Principal.FindFirst(ClaimTypes.Name).Value;
                    user.Email = result.Principal.FindFirst(ClaimTypes.Email).Value;
                    user.Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());
                    user.Role = "User";
                    user.Status = true;
                    _libraryContext.WebUsers.Add(user);
                }
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, result.Principal.FindFirst(ClaimTypes.Name).Value),
                    new Claim(ClaimTypes.Email, result.Principal.FindFirst(ClaimTypes.Email).Value),
                    new Claim(ClaimTypes.Role, "User"),
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
                };
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                              new ClaimsPrincipal(claimsIdentity));
                AccessHistory ah = new AccessHistory();
                if (_libraryContext.AccessHistories.Count() == 0)
                {
                    ah.AccessId = 0;
                }
                else
                {
                    long max_id = _libraryContext.AccessHistories.Max(u => u.AccessId);
                    ah.AccessId = max_id + 1;
                }
                ah.UserId = user.UserId;
                ah.LoginTime = DateTime.Now;
                _libraryContext.AccessHistories.Add(ah);
                _libraryContext.SaveChangesAsync();
                return RedirectToAction("Index", "Home");
            }
            return RedirectToAction("Login");
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Tìm user theo email
            WebUser user = _libraryContext.WebUsers.FirstOrDefault(u => u.Email == model.email);
            if (user == null)
            {
                TempData["Error"] = "Invalid email or password.";
                return View(model);
            }

            // Kiểm tra mật khẩu bằng BCrypt
            bool isValidPassword = false;
            try
            {
                isValidPassword = BCrypt.Net.BCrypt.Verify(model.password, user.Password);
            }
            catch
            {
                isValidPassword = false;
            }

            if (!isValidPassword)
            {
                TempData["Error"] = "Invalid email or password.";
                return View(model);
            }

            // Tạo danh sách Claims cho user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? "User"),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                          new ClaimsPrincipal(claimsIdentity));

            AccessHistory ah = new AccessHistory();
            if (_libraryContext.AccessHistories.Count() == 0)
            {
                ah.AccessId = 0;
            }
            else
            {
                long max_id = _libraryContext.AccessHistories.Max(u => u.AccessId);
                ah.AccessId = max_id + 1;
            }
            ah.UserId = user.UserId;
            ah.LoginTime = DateTime.Now;
            _libraryContext.AccessHistories.Add(ah);
            _libraryContext.SaveChanges();

            if (user.Role == "Admin")
            {
                // Nếu là Admin, chuyển hướng đến trang quản lý
                return RedirectToAction("Index", "Admin");
            }

            //Redirect sau khi đăng nhập thành công
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            RegisterViewModel model = new RegisterViewModel();
            return View(model); // Đảm bảo rằng Register.cshtml tồn tại trong Views/Account
        }
        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["RegError"] = "Invalid input.";
                TempData["ActiveForm"] = "register";
                return View("Register", model);
            }

            var existingUser = await _libraryContext.WebUsers.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (existingUser != null)
            {
                TempData["RegError"] = "Email already in use.";
                TempData["ShowRegError"] = "true";
                TempData["ActiveForm"] = "register";
                return View("Register", model);
            }

            // Tính UserId = max + 1
            int newId = 1;
            if (_libraryContext.WebUsers.Any())
            {
                newId = _libraryContext.WebUsers.Max(u => u.UserId) + 1;
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var newUser = new WebUser
            {
                UserId = newId,
                Username = model.Username,
                Email = model.Email,
                Password = hashedPassword,
                Role = "User"
            };

            _libraryContext.WebUsers.Add(newUser);
            await _libraryContext.SaveChangesAsync();

            // Đăng nhập luôn
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, newUser.Username ?? ""),
                new Claim(ClaimTypes.Email, newUser.Email ?? ""),
                new Claim(ClaimTypes.Role, newUser.Role ?? "User"),
                new Claim(ClaimTypes.NameIdentifier, newUser.UserId.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Home");
        }

        // =========================
        // 2) FORGOT PASSWORD - 3 bước
        // =========================

        // Bước 1: nhập email
        [HttpGet]
        public IActionResult ForgotPasswordEmail()
        {
            //TempData["ActiveForm"] = "reset_email"; // hiển thị container resetEmailFormContainer
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPasswordEmail(ForgotPasswordEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ForgotError"] = "Invalid input.";
                TempData["ActiveForm"] = "reset_email";
                return View("Login", model);
            }

            var user = await _libraryContext.WebUsers.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                TempData["ForgotError"] = "Email not found.";
                TempData["ShowForgotError"] = "true";
                TempData["ActiveForm"] = "reset_email";
                //return View("Login", model);
                return View("ForgotPasswordEmail", model);
            }

            // Sinh mã xác nhận
            var random = new Random();
            string verificationCode = random.Next(100000, 999999).ToString();

            HttpContext.Session.SetString("VerificationCode", verificationCode);
            HttpContext.Session.SetString("VerificationEmail", model.Email);
            HttpContext.Session.SetString("VerificationExpiration", DateTime.Now.AddMinutes(10).ToString());
            // ThreadPool.QueueUserWorkItem(state => SendVeriEmail(model.Email));

            await SendVeriEmail(model.Email);

            TempData["Message"] = "A verification code has been sent to your email.";
            // chuyển sang bước 2
            return RedirectToAction("VerifyCodeForm");
        }

        // protected void SendVeriEmail(string email)
        // {
        //     string subject = "DUTLibrary Password Recovery Code";
        //     string message = "Your Verification Code is: " + HttpContext.Session.GetString("VerificationCode");
        //     _emailSender.SendEmailAsync(email, subject, message);
        // }
        protected async Task SendVeriEmail(string email)
        {
            var verificationCode = HttpContext.Session.GetString("VerificationCode");
            await _emailSender.SendVerificationEmailAsync(email, verificationCode);
        }

        // Bước 2: Nhập mã xác nhận
        [HttpGet]
        public IActionResult VerifyCodeForm()
        {
            //TempData["ActiveForm"] = "reset_code";
            return View();
        }

        [HttpPost]
        public IActionResult VerifyCodeForm(string inputCode)
        {
            var savedCode = HttpContext.Session.GetString("VerificationCode");
            var expiration = HttpContext.Session.GetString("VerificationExpiration");
            if (string.IsNullOrEmpty(savedCode) || inputCode != savedCode)
            {
                TempData["CodeError"] = "Verification code is incorrect.";
                TempData["ShowCodeError"] = "true";
                TempData["ActiveForm"] = "reset_code";
                return View("VerifyCodeForm");
            }

            if (DateTime.Now > DateTime.Parse(expiration))
            {
                TempData["ForgotError"] = "Verification code has expired.";
                TempData["ActiveForm"] = "reset_code";
                return View("ForgotPasswordEmail");
            }
            // Mã ok -> qua bước 3
            return RedirectToAction("ResetPasswordForm");
        }

        // Bước 3: Nhập mật khẩu mới
        [HttpGet]
        public IActionResult ResetPasswordForm()
        {
            //TempData["ActiveForm"] = "reset_newpass";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPasswordForm(string newPassword, string confirmNewPassword)
        {
            if (newPassword != confirmNewPassword)
            {
                TempData["ForgotError"] = "Passwords do not match.";
                TempData["ShowForgotError"] = "true";
                TempData["ActiveForm"] = "reset_newpass";
                return View("ResetPasswordForm");
            }

            var email = HttpContext.Session.GetString("VerificationEmail");
            if (string.IsNullOrEmpty(email))
            {
                TempData["ForgotError"] = "Session expired. Please try again.";
                return RedirectToAction("ForgotPasswordEmail");
            }

            var user = await _libraryContext.WebUsers.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                TempData["ForgotError"] = "User not found.";
                return RedirectToAction("ForgotPasswordEmail");
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _libraryContext.SaveChangesAsync();

            TempData["Message"] = "Password has been reset successfully. Please log in with your new password.";
            // quay lại login
            return RedirectToAction("Login");
        }

        // =========================
        // LOGOUT
        // =========================
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Message"] = "You have been logged out.";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            TempData["ActiveForm"] = "login";
            return View("Login");
        }
    }
}
