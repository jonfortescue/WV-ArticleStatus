using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WV_ArticleStatus.Models;

namespace WV_ArticleStatus.Data
{
    public class WV_ArticleStatusContext : DbContext
    {
        public WV_ArticleStatusContext (DbContextOptions<WV_ArticleStatusContext> options)
            : base(options)
        {
        }

        public DbSet<ArticleModel> Articles { get; set; }
    }
}
