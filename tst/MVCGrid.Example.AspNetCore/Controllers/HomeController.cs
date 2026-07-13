using Microsoft.AspNetCore.Mvc;

namespace MVCGrid.Example.AspNetCore.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Detail(int id)
        {
            ViewBag.Id = id;
            return View();
        }
    }
}
