using Microsoft.AspNetCore.Mvc;

namespace EmailWeb.Controllers
{
    public class EmailController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Email()
        {
            return View();
        }

    }
}
