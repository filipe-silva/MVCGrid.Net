using Microsoft.AspNetCore.Mvc;

namespace MVCGrid.Example.AspNetCore.Controllers
{
    // One controller per top-nav page so the URLs match the classic MVC example
    // (/gettingstarted, /documentation, /download, /support — action defaults to Index).

    public class GettingStartedController : Controller
    {
        public IActionResult Index() => View();
    }

    public class DocumentationController : Controller
    {
        public IActionResult Index() => View();
    }

    public class DownloadController : Controller
    {
        public IActionResult Index() => View();
    }

    public class SupportController : Controller
    {
        public IActionResult Index() => View();
    }
}
