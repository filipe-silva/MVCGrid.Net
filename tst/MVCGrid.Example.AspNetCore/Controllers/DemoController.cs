using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MVCGrid.Example.Common;

namespace MVCGrid.Example.AspNetCore.Controllers
{
    public class DemoController : Controller
    {
        // Grid URL expressions generate links via UrlHelper.Action("detail", "demo", new { id }).
        [HttpGet("demo/detail/{id:int?}")]
        public IActionResult Detail(int id)
        {
            ViewBag.Id = id;
            return View();
        }

        // One action serves every demo in the shared catalog: /demo/sorting, /demo/filtering, ...
        [HttpGet("demo/{key?}")]
        public IActionResult Show(string key)
        {
            var demo = DemoCatalog.All.FirstOrDefault(d => d.Key == (key ?? "employees"))
                       ?? DemoCatalog.All.First(d => d.Key == "employees");
            return View(demo);
        }
    }
}
