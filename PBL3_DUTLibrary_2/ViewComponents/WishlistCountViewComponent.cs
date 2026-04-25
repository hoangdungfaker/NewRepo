using Microsoft.AspNetCore.Mvc;
using PBL3_DUTLibrary.Interfaces;

namespace PBL3_DUTLibrary.ViewComponents
{
    public class WishlistCountViewComponent : ViewComponent
    {
        private readonly IUserRepository _userRepository;

        public WishlistCountViewComponent(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public IViewComponentResult Invoke()
        {
            if (User?.Identity?.IsAuthenticated != true) // Added null checks for User and Identity
            {
                return View(0);
            }
            var currentUser = _userRepository.GetCurrentUser();
            int count = currentUser?.Books?.Count ?? 0;
            return View(count);
        }
    }
}