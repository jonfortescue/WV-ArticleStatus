using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WV_ArticleStatus.Models
{
    public class ArticleModel
    {
        private const int ONE_SENTENCE_LENGTH = 50;

        private static Regex StatusRegex = new Regex(@"{{(outline|usable|guide|star|disamb|disambig|disambiguation|stub|extra|historical|gallerypageof|"
                                                   + @"title-index page)(district|city|ruralarea|airport|park|diveguide|region|country|continent|itinerary|topic|phrasebook)?"
                                                   + @"(\|(?:subregion=(?:yes|no)|[\w /()-\.]+))?}}", RegexOptions.IgnoreCase);

        [Key]
        public string Title { get; set; }

        public string Text { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public bool Malformed { get; set; } = false;
        public string Log { get; set; }
        public bool Include { get; set; } = true;

        public ArticleModel(string title, string text)
        {
            Title = title;
            Text = text;
            DetermineStatusAndType();
        }

        private void DetermineStatusAndType()
        {
            Match statusTemplateMatch = StatusRegex.Match(Text);
            if (!statusTemplateMatch.Success)
            {
                Malformed = true;
                Log += "Article contains no or a malformed status template.\n";
                Status = "none";
                Type = "none";
                return;
            }
            else
            {
                string articleStatus = statusTemplateMatch.Groups[1].Value.ToLowerInvariant();

                switch (articleStatus)
                {
                    case "disamb":
                    case "disambig":
                    case "disambiguation":
                    case "gallerypageof":
                    case "title-index page":
                        Include = false;
                        return;

                    case "stub":
                        Status = "stub";
                        Type = "stub";
                        return;

                    case "extra":
                        articleStatus = "extra-hierarchical";
                        break;
                }

                if (statusTemplateMatch.Groups.Count == 1)
                {
                    Malformed = true;
                    Log += "Article contains a malformed status template.\n";
                    Status = "none";
                    Type = "none";
                    return;
                }
                string articleType = statusTemplateMatch.Groups[2].Value.ToLowerInvariant();

                switch (articleType)
                {
                    case "diveguide":
                        articleType = "dive guide";
                        break;

                    case "ruralarea":
                        articleType = "rural area";
                        break;

                    case "city":
                        articleType = DetermineCityType();
                        break;
                }

                Status = articleStatus;
                Type = articleType;
            }
        }

        private string DetermineCityType()
        {
            if (Text.Contains("==Districts=="))
            {
                return "huge city";
            }
            else if (Text.Contains("==Learn==") || Text.Contains("==Work==") || Text.Contains("==Cope=="))
            {
                return "big city";
            }
            else
            {
                return "small city";
            }
        }
    }

    public class Section
    { 
    
    }
}
