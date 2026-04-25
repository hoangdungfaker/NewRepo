using Microsoft.AspNetCore.Mvc;

namespace PBL3_DUTLibrary.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/404")]
        public IActionResult PageNotFound()
        {
            return View();
        }
    }
}
