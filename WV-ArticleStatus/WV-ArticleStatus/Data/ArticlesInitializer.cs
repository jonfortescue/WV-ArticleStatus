using ICSharpCode.SharpZipLib.BZip2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using WV_ArticleStatus.Models;

namespace WV_ArticleStatus.Data
{
    public static class ArticlesInitializer
    {
        public static void Initialize(WV_ArticleStatusContext context)
        {
            context.Database.EnsureCreated();

            if (context.Articles.Any())
            {
                context.Articles.RemoveRange(context.Articles);
                context.SaveChanges();
                return;
            }

            string dumpFilePath = "dump/enwikivoyage-latest-pages-articles.xml";
            if (!File.Exists(dumpFilePath))
            {
                if (!Directory.Exists("dump"))
                {
                    Directory.CreateDirectory("dump");
                }
                string compressedDumpFilePath = "dump/enwikivoyage-latest-pages-articles.xml.bz2";
                using var client = new WebClient();
                client.DownloadFile(@"https://dumps.wikimedia.org/enwikivoyage/latest/enwikivoyage-latest-pages-articles.xml.bz2", compressedDumpFilePath);

                using FileStream compressedFile = File.OpenRead(compressedDumpFilePath);
                using FileStream decompressedFile = File.Create(dumpFilePath);
                BZip2.Decompress(compressedFile, decompressedFile, false);
            }

            List<ArticleModel> articles = new();
            foreach (XElement page in XElement.Parse(File.ReadAllText(dumpFilePath)).Elements().Where(e => e.Name == "{http://www.mediawiki.org/xml/export-0.10/}page"))
            {
                var elements = page.Elements();
                string title = elements.FirstOrDefault(e => e.Name == "{http://www.mediawiki.org/xml/export-0.10/}title").Value;
                if (!title.Contains(":") &&
                    !elements.Any(e => e.Name == "{http://www.mediawiki.org/xml/export-0.10/}redirect") &&
                    elements.Any(e => e.Name == "{http://www.mediawiki.org/xml/export-0.10/}revision"))
                {
                    var article = new ArticleModel(title, elements.First(e => e.Name == "{http://www.mediawiki.org/xml/export-0.10/}revision")
                        .Elements().First(e => e.Name == "{http://www.mediawiki.org/xml/export-0.10/}text").Value);
                    if (articles.Any(e => e.Title == title))
                    {
                        throw new Exception($"Article {title} is a duplicate.");
                    }
                    if (article.Include)
                    {
                        articles.Add(article);
                    }
                }
            }

            context.Articles.AddRange(articles);
            context.SaveChanges();
        }
    }
}
