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
        private readonly ArticlesContext _context;

        public HomeController(ILogger<HomeController> logger, ArticlesContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var malformedArticles = (await _context.Articles.Where(a => a.Malformed == true).ToListAsync()).OrderBy(a => a.Title).ToList();
            return View(malformedArticles);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult AnalysisDiffers()
        {
            var analysisDiffersArticles = _context.Articles.Where(
                a => a.Status != a.AnalyzedStatus && (a.Type == "district" || a.Type == "small city" || a.Type == "big city" || a.Type == "rural area" || a.Type == "huge city"))
                .OrderBy(a => a.Status).ThenBy(a => a.Title);
            return View(analysisDiffersArticles);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
