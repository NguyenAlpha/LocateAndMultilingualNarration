using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    public class DocsController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public DocsController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Brd()
        {
            var filePath = Path.Combine(_environment.ContentRootPath, "Views", "Docs", "Brd.html");
            return PhysicalFile(filePath, "text/html; charset=utf-8");
        }
    }
}
