﻿using Microsoft.EntityFrameworkCore;
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

        private static Regex StatusRegex = new Regex(@"{{(outline|usable|useable|guide|star|disamb|disambig|disambiguation|stub|extra|historical|gallerypageof|"
                                                   + @"title-index page)(district|city|ruralarea|airport|park|event|diveguide|region|country|continent|itinerary|topic|phrasebook)?"
                                                   + @"(\|(?:subregion=(?:yes|no)|[\w /()-\.]+))?}}", RegexOptions.IgnoreCase);

        [Key]
        public string Title { get; set; }

        public string Text { get; set; }
        public string Status { get; set; }
        public string AnalyzedStatus { get; set; }
        public string Type { get; set; }
        public bool Malformed { get; set; } = false;
        public string Log { get; set; }

        public double TemplateMatchPercentage { get; set; }
        public string TemplateSectionsMissing { get; set; }

        public List<string> RegionsOrDistricts { get; set; } = new();
        public List<string> Cities { get; set; } = new();
        public List<string> OtherDestinations { get; set; } = new();

        [NotMapped]
        public bool Include { get; set; } = true;

        [NotMapped]
        public string Lead { get; set; }

        [NotMapped]
        public List<Section> Sections { get; set; } = new();

        public ArticleModel(string title, string text)
        {
            Title = title;
            Text = text;
            DetermineStatusAndType();
            ParseSections();
        }

        private void DetermineStatusAndType()
        {
            Match statusTemplateMatch = StatusRegex.Match(Text);
            if (!statusTemplateMatch.Success)
            {
                Malformed = true;
                Log += $"Article contains no or a malformed status template: could not be found in text.\n";
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

                    case "useable":
                        articleStatus = "usable";
                        break;
                }

                if (statusTemplateMatch.Groups.Count == 1)
                {
                    Malformed = true;
                    Log += $"Article contains a malformed type template; article status found: {articleStatus}\n";
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
            if (Text.Contains("{{printDistricts}}"))
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

        private void ParseSections()
        {
            string[] sectionsSplit = Regex.Split(Text, @"\s== *(?=[A-z])(?!-->)");
            Lead = sectionsSplit[0];

            if (sectionsSplit.Length > 0)
            {
                for (int i = 1; i < sectionsSplit.Length; i++)
                {
                    Section section = new(null, sectionsSplit[i], "==");
                    if (section.Malformed)
                    {
                        Malformed = true;
                    }
                    Sections.Add(section);
                }
            }
        }

        public void GetLinkedArticles(IEnumerable<ArticleModel> articles)
        {
            try
            {
                Regex regionAndDistrictRegex = new(@"region.+name=.*\[\[(?<link>[^\|\]]+)(?:\|(?<name>[^\]]+))?\]\]");
                Regex citiesAndDestinationsRegex = new(@"{{marker\|(?:type=.+\|)name=\[\[(?<link>[^\|\]]+)(?:\|(?<name>[^\]]+))?\]\]");
                switch (Type)
                {
                    case "huge city":
                        Section districtSection = Sections.FirstOrDefault(s => ArticleHeaders.DISTRICTS(s.Title));
                        if (districtSection is not null)
                        {
                            var matches = regionAndDistrictRegex.Matches(districtSection.Text);
                            foreach (Match match in matches)
                            {
                                string link = match.Groups["link"].Value.Replace("/", ":");
                                ArticleModel article = articles.FirstOrDefault(a => a.Title == link);
                                RegionsOrDistricts.Add(link);
                            }
                        }
                        else
                        {
                            Log += "District section not found.\n";
                            Malformed = true;
                        }
                        break;

                    case "region":
                    case "continent":
                        Section regionsSection = Sections.FirstOrDefault(s => ArticleHeaders.REGIONS(s.Title));
                        if (regionsSection is not null)
                        {
                            var matches = regionAndDistrictRegex.Matches(regionsSection.Text);
                            foreach (Match match in matches)
                            {
                                string link = match.Groups["link"].Value.Replace("/", ":");
                                ArticleModel article = articles.FirstOrDefault(a => a.Title == link);
                                if (article is not null)
                                {
                                    RegionsOrDistricts.Add(link);
                                }
                                else
                                {
                                    Log += $"Region '{link}' not found.\n";
                                }
                            }
                        }
                        else
                        {
                            Log += "Region section not found.\n";
                        }

                        Section citiesSection = Sections.FirstOrDefault(s => ArticleHeaders.CITIES(s.Title));
                        if (citiesSection is not null)
                        {
                            var matches = citiesAndDestinationsRegex.Matches(citiesSection.Text);
                            foreach (Match match in matches)
                            {
                                string link = match.Groups["link"].Value.Replace("/", ":");
                                ArticleModel article = articles.FirstOrDefault(a => a.Title == link);
                                if (article is not null)
                                {
                                    Cities.Add(link);
                                }
                                else
                                {
                                    Log += $"City '{link}' not found.\n";
                                }
                            }
                        }
                        else
                        {
                            Log += "Cities section not found.\n";
                        }

                        Section destinationsSection = Sections.FirstOrDefault(s => ArticleHeaders.OTHER_DESTINATIONS(s.Title));
                        if (destinationsSection is not null)
                        {
                            var matches = citiesAndDestinationsRegex.Matches(destinationsSection.Text);
                            foreach (Match match in matches)
                            {
                                string link = match.Groups["link"].Value.Replace("/", ":");
                                ArticleModel article = articles.FirstOrDefault(a => a.Title == link);
                                if (article is not null)
                                {
                                    OtherDestinations.Add(link);
                                }
                                else
                                {
                                    Log += $"Destination '{link}' not found.\n";
                                }
                            }
                        }
                        else
                        {
                            Log += "Other destinations section not found.\n";
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in article {Title}; {ex.Message}");
            }
        }

        //the article model has been constructed. now, we analyze the model
        //and programmatically analyze the article to determine its status.
        //this requires us to break out the articles by type.
        //CITY        small city / big city / district / rural area; huge city used as well with +
        //REGION      continent / continental section / state / region
        //the rest are self-explanatory

        //CITY
        //For city articles, the following criterion is used:
        //STAR:
        //      * No MOS deviations
        //      * Number of (non-map photos) > 1; preferably 2-3
        //      + All districts are GUIDE status
        //      * All requirements for GUIDE are met
        //GUIDE:
        //      * Understand section has at least 100 words of prose (N/A for districts)
        //      * See/Do/Eat/Drink/Sleep sections all meet 7(+/-)2 rule. If there are >9 entries,
        //          the entries are divided up into multiple lists (Splurge/Mid-range/Budget)
        //      * MOS deviations at 19:1 ratio (19 correct MOS implementations for every one incorrect)
        //      * Get in section has >=2 subsections with prose
        //      * Get around section has at least 100 words of prose and/or >=2 subsections with prose (N/A for districts)
        //      * Go next has >=3 bullet points with appropriate one-liner descriptions (N/A for districts)
        //      * 50% of listings have geocoordinates
        //      * All sections have at least 25 words of prose
        //      + All districts are USABLE status
        //      * All requirements for USABLE are met
        //USABLE:
        //      * Get in section is not empty
        //      * Eat and Sleep each have at least one listing with contact information
        //      * See or Do section has at least one listing
        //      * All requirements for OUTLINE are met
        //OUTLINE:
        //      * Lead section has at least one sentence (50 characters)
        //      * 70% of sections required by the template are present; all essential sections present
        //STUB:
        //      * Requirements for OUTLINE are not met
        public void AnalyzeStatus(IEnumerable<ArticleModel> articles)
        {
            bool requiredSectionsPresent = RequiredSectionsPresent();
            TemplateMatchPercentageAndSectionsMissing();

            switch (Type)
            {
                case "district":
                case "small city":
                case "big city":
                case "huge city":
                case "rural area":
                    if (Lead.Length < ONE_SENTENCE_LENGTH || TemplateMatchPercentage < 0.70 || !requiredSectionsPresent)
                    {
                        AnalyzedStatus = "stub";
                    }
                    else if (Sections.First(s => ArticleHeaders.GET_IN(s.Title)).Text.Length == 0
                        || !Sections.First(s => ArticleHeaders.EAT(s.Title)).AtLeastOneListingWithContactInfo()
                        || !Sections.First(s => ArticleHeaders.SLEEP(s.Title)).AtLeastOneListingWithContactInfo()
                        || Sections.First(s => ArticleHeaders.SEE(s.Title)).NumberOfListings == 0)
                    {
                        AnalyzedStatus = "outline";
                    }
                    else if ((Sections.FirstOrDefault(s => ArticleHeaders.UNDERSTAND(s.Title))?.Words ?? 0) < 100
                        || !(Sections.FirstOrDefault(s => ArticleHeaders.SEE(s.Title))?.SectionMeets7PlusOrMinus2Rule() ?? false)
                        || !(Sections.FirstOrDefault(s => ArticleHeaders.DO(s.Title))?.SectionMeets7PlusOrMinus2Rule() ?? false)
                        || !(Sections.FirstOrDefault(s => ArticleHeaders.EAT(s.Title))?.SectionMeets7PlusOrMinus2Rule() ?? false)
                        || !(Sections.FirstOrDefault(s => ArticleHeaders.DRINK(s.Title))?.SectionMeets7PlusOrMinus2Rule() ?? false)
                        || !(Sections.FirstOrDefault(s => ArticleHeaders.SLEEP(s.Title))?.SectionMeets7PlusOrMinus2Rule() ?? false)
                        || (Sections.FirstOrDefault(s => ArticleHeaders.GET_IN(s.Title))?.NumberOfSubsectionsWithProse() ?? 0) < 2
                        || (Type != "district" && (Sections.FirstOrDefault(s => ArticleHeaders.GET_AROUND(s.Title))?.Words ?? 0) < 100 &&
                                (Sections.FirstOrDefault(s => ArticleHeaders.GET_AROUND(s.Title))?.NumberOfSubsectionsWithProse() ?? 0) < 2)
                        || Type != "huge city" && Sections.Sum(s => s.NumberOfListingsWithGeoCoordinates()) / Sections.Sum(s => s.NumberOfListings) < 0.50
                        || Type != "district" && (Sections.FirstOrDefault(s => ArticleHeaders.GO_NEXT(s.Title))?.Text.Split('*').Length ?? 0) < 3
                        || Sections.Any(s => s.Words < 25)
                        || (Type == "huge city" && RegionsOrDistricts.Any(d => (articles.FirstOrDefault(a => a.Title == d)?.Status ?? "") == "outline"
                                || (articles.FirstOrDefault(a => a.Title == d)?.Status ?? "") == "stub"))
                        )
                    {
                        AnalyzedStatus = "usable";
                    }
                    else if (!Text.Contains("{{mapframe}}")
                        || !Text.Contains("[[File:")
                        || (Type == "huge city" && RegionsOrDistricts.Any(d => (articles.FirstOrDefault(a => a.Title == d)?.Status ?? "") == "usable" ||
                            (articles.FirstOrDefault(a => a.Title == d)?.Status ?? "") == "outline" || (articles.FirstOrDefault(a => a.Title == d)?.Status ?? "") == "stub"))
                        )
                    {
                        AnalyzedStatus = "guide";
                    }
                    else
                    {
                        AnalyzedStatus = "star";
                    }
                    bool getAroundRequirementsMet = Type != "district" || (Sections.FirstOrDefault(s => ArticleHeaders.GET_AROUND(s.Title))?.Words ?? 0) >= 250 ||
                        (Sections.FirstOrDefault(s => ArticleHeaders.GET_AROUND(s.Title))?.NumberOfSubsectionsWithProse() ?? 0) >= 2;
                    bool usableDistrictRequirementsMet = Type != "huge city" || RegionsOrDistricts.All(d => (articles.FirstOrDefault(a => a.Title == d)?.Status ?? "") != "outline"
                        && (articles.FirstOrDefault(a => a.Title == d)?.Status ?? "") != "stub");
                    bool guideDistrictRequirementsMet = Type != "huge city" || RegionsOrDistricts.All(d => (articles.FirstOrDefault(a => a.Title == d)?.Status ?? "") != "usable" &&
                        (articles.FirstOrDefault(a => a.Title == d)?.Status ?? "") != "outline" && (articles.FirstOrDefault(a => a.Title == d)?.Status ?? "") != "stub");

                    Log += $"\nLead > One sentence: {Lead.Length >= ONE_SENTENCE_LENGTH}\nTemplate match percentage: {TemplateMatchPercentage * 100}%\n" +
                        $"Required sections present: {requiredSectionsPresent}\nTemplate sections missing: {string.Join(", ", TemplateSectionsMissing)}\n" +
                        $"Get in section not empty: {(Sections.FirstOrDefault(s => ArticleHeaders.GET_IN(s.Title))?.Text.Length ?? 0) > 0}\n" +
                        $"Eat has one listing w/ contact info: {Sections.FirstOrDefault(s => ArticleHeaders.EAT(s.Title))?.AtLeastOneListingWithContactInfo() ?? false}\n" +
                        $"Sleep has one listing w/ contact info: {Sections.FirstOrDefault(s => ArticleHeaders.SLEEP(s.Title))?.AtLeastOneListingWithContactInfo() ?? false}\n" +
                        $"Percentage of Listings with geocoordinates: {Sections.Sum(s => s.NumberOfListingsWithGeoCoordinates()) / Sections.Sum(s => s.NumberOfListings) * 100}%\n" +
                        $"See has at least one listing: {(Sections.FirstOrDefault(s => ArticleHeaders.SEE(s.Title))?.NumberOfListings ?? 0) > 0}\n" +
                        $"Understand word count: {Sections.FirstOrDefault(s => ArticleHeaders.UNDERSTAND(s.Title))?.Words ?? 0}\n" +
                        $"See meets 7+/-2 rule: {Sections.FirstOrDefault(s => ArticleHeaders.SEE(s.Title))?.SectionMeets7PlusOrMinus2Rule() ?? false}\n" +
                        $"Do meets 7+/-2 rule: {Sections.FirstOrDefault(s => ArticleHeaders.DO(s.Title))?.SectionMeets7PlusOrMinus2Rule() ?? false}\n" +
                        $"Eat meets 7+/-2 rule: {Sections.FirstOrDefault(s => ArticleHeaders.EAT(s.Title))?.SectionMeets7PlusOrMinus2Rule() ?? false}\n" +
                        $"Drink meets 7+/-2 rule: {Sections.FirstOrDefault(s => ArticleHeaders.DRINK(s.Title))?.SectionMeets7PlusOrMinus2Rule() ?? false}\n" +
                        $"Sleep meets 7+/-2 rule: {Sections.FirstOrDefault(s => ArticleHeaders.SLEEP(s.Title))?.SectionMeets7PlusOrMinus2Rule() ?? false}\n" +
                        $"Get in subsections with prose: {Sections.FirstOrDefault(s => ArticleHeaders.GET_IN(s.Title))?.NumberOfSubsectionsWithProse() ?? 0}\n" +
                        $"Get around requirements met: {getAroundRequirementsMet}\n" +
                        $"Go next number of destinations: {Sections.FirstOrDefault(s => ArticleHeaders.GO_NEXT(s.Title))?.Text.Split('*').Length}\n" +
                        $"All sections have more than 25 words: {Sections.All(s => s.Words >= 25)}\n" +
                        $"Usable districts requirement met: {usableDistrictRequirementsMet}\n" +
                        $"Map present: {Text.Contains("{{mapframe}}")}\n" +
                        $"Image present: {Text.Contains("[[File:")}\n" +
                        $"Guide districts requirement met: {guideDistrictRequirementsMet}\n";
                    break;
            }
        }

        private void TemplateMatchPercentageAndSectionsMissing()
        {
            double sectionsCount = 0.0;
            List<Func<string, bool>> templateSections = new();
            List<string> missingSections = new();
            Dictionary<Func<string, bool>, List<string>> templateSubsections = new();

            switch (Type)
            {
                case "district":
                    templateSections.AddRange(new Func<string, bool>[] { ArticleHeaders.GET_IN, ArticleHeaders.SEE, ArticleHeaders.DO, ArticleHeaders.BUY,
                        ArticleHeaders.EAT, ArticleHeaders.DRINK, ArticleHeaders.SLEEP, ArticleHeaders.CONNECT });
                    break;
                case "small city":
                    templateSections.AddRange(new Func<string, bool>[] { ArticleHeaders.UNDERSTAND, ArticleHeaders.GET_IN, ArticleHeaders.GET_AROUND,
                        ArticleHeaders.SEE, ArticleHeaders.DO, ArticleHeaders.BUY, ArticleHeaders.EAT, ArticleHeaders.DRINK, ArticleHeaders.SLEEP,
                        ArticleHeaders.CONNECT, ArticleHeaders.GO_NEXT });
                    break;
                case "big city":
                    templateSections.AddRange(new Func<string, bool>[] { ArticleHeaders.UNDERSTAND, ArticleHeaders.GET_IN, ArticleHeaders.GET_AROUND,
                        ArticleHeaders.SEE, ArticleHeaders.DO, ArticleHeaders.LEARN, ArticleHeaders.WORK, ArticleHeaders.BUY, ArticleHeaders.EAT,
                        ArticleHeaders.DRINK, ArticleHeaders.SLEEP, ArticleHeaders.STAY_SAFE, ArticleHeaders.CONNECT, ArticleHeaders.COPE, ArticleHeaders.GO_NEXT });
                    templateSubsections.Add(ArticleHeaders.GET_IN, ArticleHeaders.SubsectionHeaders[ArticleHeaders.GET_IN]);
                    templateSubsections.Add(ArticleHeaders.DO, ArticleHeaders.SubsectionHeaders[ArticleHeaders.DO]);
                    templateSubsections.Add(ArticleHeaders.EAT, ArticleHeaders.SubsectionHeaders[ArticleHeaders.EAT]);
                    templateSubsections.Add(ArticleHeaders.SLEEP, ArticleHeaders.SubsectionHeaders[ArticleHeaders.SLEEP]);
                    break;
                case "rural area":
                    templateSections.AddRange(new Func<string, bool>[] { ArticleHeaders.UNDERSTAND, ArticleHeaders.GET_IN, ArticleHeaders.GET_AROUND, ArticleHeaders.SEE,
                        ArticleHeaders.DO, ArticleHeaders.BUY, ArticleHeaders.EAT, ArticleHeaders.DRINK, ArticleHeaders.SLEEP, ArticleHeaders.CONNECT, ArticleHeaders.STAY_SAFE,
                        ArticleHeaders.GO_NEXT });
                    templateSubsections.Add(ArticleHeaders.GET_AROUND, new List<string> { "By car", "By boat", "By public transit", "By bicycle", "On foot" });
                    templateSubsections.Add(ArticleHeaders.SLEEP, new List<string> { "Camping", "Backcountry" });
                    break;
                case "huge city":
                    templateSections.AddRange(new Func<string, bool>[] { ArticleHeaders.DISTRICTS, ArticleHeaders.UNDERSTAND, ArticleHeaders.GET_IN, ArticleHeaders.GET_AROUND,
                        ArticleHeaders.SEE, ArticleHeaders.DO, ArticleHeaders.LEARN, ArticleHeaders.WORK, ArticleHeaders.BUY, ArticleHeaders.EAT, ArticleHeaders.DRINK,
                        ArticleHeaders.SLEEP, ArticleHeaders.CONNECT, ArticleHeaders.STAY_SAFE, ArticleHeaders.COPE, ArticleHeaders.GO_NEXT});
                    templateSubsections.Add(ArticleHeaders.GET_IN, ArticleHeaders.SubsectionHeaders[ArticleHeaders.GET_IN]);
                    templateSubsections.Add(ArticleHeaders.COPE, ArticleHeaders.SubsectionHeaders[ArticleHeaders.COPE]);
                    break;
            }

            foreach (Func<string, bool> templateSection in templateSections)
            {
                if (Sections.Any(s => templateSection(s.Title)))
                {
                    sectionsCount++;
                }
                else
                {
                    missingSections.Add(ArticleHeaders.FuncToString[templateSection]);
                }
            }

            TemplateMatchPercentage = sectionsCount / (templateSections.Count == 0 ? 1 : templateSections.Count);
            TemplateSectionsMissing = string.Join(", ", missingSections);
        }

        public bool RequiredSectionsPresent()
        {
            bool correct = Sections.Any(s => ArticleHeaders.GET_IN(s.Title)) && Sections.Any(s => ArticleHeaders.SEE(s.Title)) && Sections.Any(s => ArticleHeaders.SEE(s.Title))
                && Sections.Any(s => ArticleHeaders.EAT(s.Title)) && Sections.Any(s => ArticleHeaders.SLEEP(s.Title));
            if (Type != "district")
            {
                correct = correct && Sections.Any(s => ArticleHeaders.GET_AROUND(s.Title));
            }
            if (Type == "huge city")
            {
                correct = correct && Sections.Any(s => ArticleHeaders.DISTRICTS(s.Title));
            }
            if (Type == "region")
            {
                correct = correct && (Sections.Any(s => ArticleHeaders.REGIONS(s.Title)) || Sections.Any(s => ArticleHeaders.CITIES(s.Title)));
            }

            return correct;
        }
    }

    public class Section
    {
        public Section Parent { get; set; }
        public List<Listing> Listings { get; set; } = new();
        public string Title { get; set; }
        public string Text { get; set; }
        public string Lead { get; set; }
        public List<Section> Subsections { get; set; } = new();
        public bool Malformed { get; set; } = false;

        public Section(Section parent, string text, string sectionPrefix)
        {
            Parent = parent;
            try
            {
                Title = text[0..text.IndexOf(sectionPrefix)].Trim();
                Text = text[text.IndexOf(sectionPrefix)..];
            }
            catch
            {
                Title = "!! MALFORMED SECTION !!";
                Malformed = true;
                return;
            }

            Regex listingsRegex = new(@"{{(?<type>listing|see|do|buy|eat|drink|sleep|go)");
            var listings = listingsRegex.Split(Text);
            for (int i = 1; i < listings.Length - 1; i += 2)
            {
                Listing listing = new(listings[i], listings[i + 1]);
                if (!string.IsNullOrWhiteSpace(listing.Name))
                {
                    Listings.Add(listing);
                }
            }

            string newPrefix = $"{sectionPrefix}=";
            if (Text.Contains(newPrefix))
            {
                string[] subsectionsSplit = Regex.Split(Text, @$"\s{newPrefix} *(?=[A-z])(?!-->)");
                Lead = subsectionsSplit[0];
                if (subsectionsSplit.Length > 1)
                {
                    for (int i = 1; i < subsectionsSplit.Length; i++)
                    {
                        Section subsection = new Section(this, subsectionsSplit[i], newPrefix);
                        if (subsection.Malformed)
                        {
                            Malformed = true;
                        }
                        Subsections.Add(subsection);
                    }
                }
            }
        }

        public int Words => Text.Split(' ').Length;
        public int NumberOfListings => Listings.Count + (Text.Contains("{{seeDistricts}}", StringComparison.OrdinalIgnoreCase) ? 1 : 0);

        public bool AtLeastOneListingWithContactInfo()
        {
            return Listings.Any(l => !string.IsNullOrEmpty(l.Phone) || !string.IsNullOrEmpty(l.Email) || !string.IsNullOrEmpty(l.Url))
                || Text.Contains("{{seeDistricts}}", StringComparison.OrdinalIgnoreCase);
        }

        public double NumberOfListingsWithGeoCoordinates()
        {
            if (Text.Contains("{{seeDistricts}}", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }
            return Listings.Where(l => l.Lat > double.MinValue && l.Long > double.MinValue).Count();
        }

        public int NumberOfSubsectionsWithProse()
        {
            return Subsections.Count(s => s.Text.Length > 0);
        }

        public bool SectionMeets7PlusOrMinus2Rule()
        {
            if (Text.Contains("{{seeDistricts}}", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (Listings.Count > 9 && Subsections.Count > 0)
            {
                int numSubsections = Subsections.Count;
                numSubsections += Lead.Contains("{{") ? 1 : 0;

                int average = Listings.Count / numSubsections;
                return average <= 9;
            }
            else
            {
                return Listings.Count >= 5 && Listings.Count <= 9;
            }
        }

        public string ToHtml()
        {
            string html = $"\n<li>{Title}";
            if (Subsections.Count > 0)
            {
                html += "\n<ul>";
                foreach (var subsection in Subsections)
                {
                    html += subsection.ToHtml();
                }
                html += "</ul>";
            }
            html += "</li>";
            return html;
        }
    }

    public class Listing
    {
        public Listing(string type, string text)
        {
            Type = type;
            Regex listingRegex = new(@"(?<key>\w+)=(?<value>[^\|\n]{2,})(?:\s*\|\s*|\s*}})");
            var matches = listingRegex.Matches(text);
            foreach (Match match in matches)
            {
                if (string.Equals(match.Groups["key"].Value, "type", StringComparison.OrdinalIgnoreCase))
                {
                    Type = match.Groups["value"].Value;
                }
                else if (string.Equals(match.Groups["key"].Value, "name", StringComparison.OrdinalIgnoreCase))
                {
                    Name = match.Groups["value"].Value;
                }
                else if (string.Equals(match.Groups["key"].Value, "phone", StringComparison.OrdinalIgnoreCase))
                {
                    Phone = match.Groups["value"].Value;
                }
                else if (string.Equals(match.Groups["key"].Value, "email", StringComparison.OrdinalIgnoreCase))
                {
                    Email = match.Groups["value"].Value;
                }
                else if (string.Equals(match.Groups["key"].Value, "hours", StringComparison.OrdinalIgnoreCase))
                {
                    Hours = match.Groups["value"].Value;
                }
                else if (string.Equals(match.Groups["key"].Value, "url", StringComparison.OrdinalIgnoreCase))
                {
                    Url = match.Groups["value"].Value;
                }
                else if (string.Equals(match.Groups["key"].Value, "lat", StringComparison.OrdinalIgnoreCase))
                {
                    _ = double.TryParse(match.Groups["value"].Value, out double lat);
                    Lat = lat;
                }
                else if (string.Equals(match.Groups["key"].Value, "long", StringComparison.OrdinalIgnoreCase))
                {
                    _ = double.TryParse(match.Groups["value"].Value, out double @long);
                    Long = @long;
                }
            }
        }

        public string Type { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Hours { get; set; }
        public string Url { get; set; }
        public double Lat { get; set; } = double.MinValue;
        public double Long { get; set; } = double.MinValue;
    }

    public static class ArticleHeaders
    {
        public static Func<string, bool> DISTRICTS = n => n == "Districts" || n == "Boroughs" || n == "Subdivisions";
        public static Func<string, bool> REGIONS = n => n == "Regions" || n == "Provinces" || n == "States" || n == "Counties";
        public static Func<string, bool> CITIES = n => n == "Cities" || n == "Towns" || n == "Villages" || n == "Communities" || n == "Islands" || n == "Hamlets" || n == "Municipalities" || n == "Settlements" || n == "Parishes"
                                                                     || n == "Parishes and cities" || n == "Cities and towns" || n == "Towns and cities" || n == "Towns and villages" || n == "Cities and villages" || n == "Towns and hamlets"
                                                                     || n == "Towns and islands" || n == "Islands and towns" || n == "Towns and districts" || n == "Cities and municipalities" || n == "Cities, towns and villages";
        public static Func<string, bool> OTHER_DESTINATIONS = n => n == "Other destinations";
        public static Func<string, bool> UNDERSTAND = n => n == "Understand";
        public static Func<string, bool> PREPARE = n => n == "Prepare";
        public static Func<string, bool> FLIGHTS = n => n == "Flights";
        public static Func<string, bool> TALK = n => n == "Talk";
        public static Func<string, bool> GET_IN = n => n == "Get in";
        public static Func<string, bool> FEES_AND_PERMITS = n => n == "Fees and permits";
        public static Func<string, bool> GET_AROUND = n => n == "Get around";
        public static Func<string, bool> WAIT = n => n == "Wait";
        public static Func<string, bool> GO_WALK_DRIVE = n => n == "Go" || n == "Walk" || n == "Drive";
        public static Func<string, bool> SEE = n => n == "See" || n == "See and do";
        public static Func<string, bool> DO = n => n == "Do" || n == "See and do";
        public static Func<string, bool> LEARN = n => n == "Learn";
        public static Func<string, bool> WORK = n => n == "Work";
        public static Func<string, bool> BUY = n => n == "Buy";
        public static Func<string, bool> EAT = n => n == "Eat" || n == "Eat and drink";
        public static Func<string, bool> DRINK = n => n == "Drink" || n == "Eat and drink";
        public static Func<string, bool> SLEEP = n => n == "Sleep";
        public static Func<string, bool> STAY_SAFE = n => n == "Stay safe";
        public static Func<string, bool> CONNECT = n => n == "Connect";
        public static Func<string, bool> COPE = n => n == "Cope";
        public static Func<string, bool> GO_NEXT = n => n == "Go next";
        public static Func<string, bool> NEARBY = n => n == "Nearby";
        public static Func<string, bool> PRONUNCIATION_GUIDE = n => n == "Pronunciation guide";
        public static Func<string, bool> PHRASE_LIST = n => n == "Phrase list";

        public static Dictionary<Func<string, bool>, string> FuncToString = new()
        {
            { DISTRICTS, "Districts" },
            { UNDERSTAND, "Understand" },
            { PREPARE, "Prepare" },
            { FLIGHTS, "Flights" },
            { TALK, "Talk" },
            { GET_IN, "Get in" },
            { FEES_AND_PERMITS, "Fees and permits" },
            { GET_AROUND, "Get around" },
            { WAIT, "Wait" },
            { GO_WALK_DRIVE, "Go/Walk/Drive" },
            { SEE, "See" },
            { DO, "Do" },
            { LEARN, "Learn" },
            { WORK, "Work" },
            { BUY, "Buy" },
            { EAT, "Eat" },
            { DRINK, "Drink" },
            { SLEEP, "Sleep" },
            { STAY_SAFE, "Stay safe" },
            { CONNECT, "Connect" },
            { COPE, "Cope" },
            { GO_NEXT, "Go next" },
            { NEARBY, "Nearby" },
            { PRONUNCIATION_GUIDE, "Pronunciation guide" },
            { PHRASE_LIST, "Phrase list" },
        };

        public static Dictionary<Func<string, bool>, List<string>> SubsectionHeaders = new()
        {
            { GET_IN, new List<string> { "By plane", "By train", "By car", "By bus", "By boat" } },
            { SEE, new List<string> { "Itineraries" } },
            { DO, new List<string> { "Events" } },
            { BUY, new List<string> { "Costs", "Tipping" } },
            { EAT, new List<string> { "Budget", "Mid-range", "Splurge" } },
            { SLEEP, new List<string> { "Budget", "Mid-range", "Splurge" } },
            { COPE, new List<string> { "Embassies" } }
        };
    }
}
