using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PBL3_DUTLibrary.Data;
using PBL3_DUTLibrary.Interface;
using PBL3_DUTLibrary.Interfaces;
using PBL3_DUTLibrary.Models;
using PBL3_DUTLibrary.Repository;
using PBL3_DUTLibrary.Services;
using PBL3_DUTLibrary.ViewModels;

namespace PBL3_DUTLibrary.Controllers
{
    public class CartController : Controller
    {
        private readonly IUserRepository _userRepository;
        public CartController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
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
            return View(profileViewModel);
        }
    }
}
