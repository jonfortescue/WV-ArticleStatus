using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WV_ArticleStatus.Data;
using WV_ArticleStatus.Models;

namespace WV_ArticleStatus.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly WV_ArticleStatusContext _context;

        public HomeController(ILogger<HomeController> logger, WV_ArticleStatusContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var malformedArticles = await _context.Articles.Where(a => a.Malformed == true).ToListAsync();
            return View(malformedArticles);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // GET: Article/5
        public async Task<IActionResult> Article(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var articleModel = await _context.Articles.FirstOrDefaultAsync(m => m.Title == id);
            if (articleModel == null)
            {
                return NotFound();
            }

            return View(articleModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
