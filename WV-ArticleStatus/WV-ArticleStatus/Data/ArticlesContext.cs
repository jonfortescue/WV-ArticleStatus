using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WV_ArticleStatus.Models;

namespace WV_ArticleStatus.Data
{
    public class ArticlesContext : DbContext
    {
        public ArticlesContext (DbContextOptions<ArticlesContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            var splitStringConverter = new ValueConverter<List<string>, string>(v => string.Join(";", v), v => v.Split(new[] { ';' }).ToList());
            builder.Entity<ArticleModel>().Property(nameof(ArticleModel.RegionsOrDistricts)).HasConversion(splitStringConverter);
            builder.Entity<ArticleModel>().Property(nameof(ArticleModel.Cities)).HasConversion(splitStringConverter);
            builder.Entity<ArticleModel>().Property(nameof(ArticleModel.OtherDestinations)).HasConversion(splitStringConverter);

            var titleConverter = new ValueConverter<string, string>(v => v.Replace("ș", "sssss"), v => v.Replace("sssss", "ș"));
            builder.Entity<ArticleModel>().Property(nameof(ArticleModel.Title)).HasConversion(titleConverter);
        }

        public DbSet<ArticleModel> Articles { get; set; }
    }
}
