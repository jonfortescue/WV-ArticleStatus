import re
from pymongo import MongoClient
from bson.binary import Binary
import pickle
import sys

# definitions of "article standards"
def ONE_SENTENCE(text):
    return len(text) >= 50

statusRegex = re.compile(r'{{(outline|usable|guide|star|disamb|disambig|disambiguation|stub|extra|historical|gallerypageof|title-index page)(district|city|airport|park|diveguide|region|country|continent|itinerary|topic|phrasebook)?(\|(?:subregion=(?:yes|no)|[\w /()-\.]+))?}}', re.I)

def bsonLoadSections(dump):
    return pickle.loads(dump.decode('latin1'))

class Article:
    "Articles are basically like special sections"

    def __init__(self, db, title, text):
        self.title = title
        self.text = text
        (self.status, self.type, self.malformed, self.log, delete) = self.determine_status_and_type()
        if delete:
            db.delete_one({'title': self.title})
        else:
            db.update_one( {'title': self.title}, { '$set': {'status': self.status, 'type': self.type, 'malformed': self.malformed} })
            (self.lead, self.sections, log, self.malformed) = self.parse_sections()
            self.log += log
            db.update_one( {'title': self.title}, { '$set': {'sections': self.bsonDumpSections(), 'malformed': self.malformed} })
            self.analyze_status(db)
        print self.log.strip()

    def __contains__(self, item):
        return any([item in section.title for section in self.sections])

    def determine_status_and_type(self):
        "Returns a tuple of the format (string ArticleStatus, string ArticleType, bool ArticleIsMalformed, string log, bool delete)"
        log = ""
        statusTemplateMatch = statusRegex.search(self.text)
        if statusTemplateMatch is None:
            log += "Article " + self.title + " has a malformed or no status template."
            return ('none', 'none', True, log, False)
        else:
            # our regex has the advantage of having two capture groups which correspond to status and type
            articleStatus = statusTemplateMatch.group(1).lower()
            # we want to remove all disambiguation articles from the database
            if articleStatus == "disamb" or articleStatus == "disambig" or articleStatus == "disambiguation":
                log += "Article " + self.title + " is a disambiguation article. Deleting from MongoDB...\n"
                return ('disambiguation', 'disambiguation', False, log, True)
            # we also want to remove gallery articles
            elif articleStatus == "gallerypageof":
                log += "Article " + self.title + " is a gallery article. Deleting from MongoDB...\n"
                return ('gallery', 'gallery', False, log, True)
            # and all title/index pages
            elif articleStatus == "title-index page":
                log +=  "Article " + self.title + " is a title/index page. Deleting from MongoDB...\n"
                return ('title/index', 'title/index', False, log, True)
            elif articleStatus == "historical":
                log += "Article" + self.title + " is archived as inactive/historical.\n"
                return ('historical', 'historical', False, log, False)
            # next we find stub articles -- stubs don't have an article type, so they get marked as 'stub' for both
            elif articleStatus == "stub":
                log += "Article " + self.title + " is a stub.\n"
                return ('stub', 'stub', False, log, False)
            # if the template is malformed...
            elif statusTemplateMatch.group(2) is None:
                log +=  "Article " + self.title + " has a malformed status template\n"
                return ('none', 'none', True, log, False)
            else:
                if articleStatus == "extra":
                    articleStatus = "extra-hierarchical"
                articleType = statusTemplateMatch.group(2).lower()
                if (articleType) == "diveguide": # correct dive guide to make it prettier
                    articleType = "dive guide"
                if articleType == "city":
                    articleType = self.determine_city_type()
                log += "Article " + self.title + " is a(n) " + articleType + " article of " + articleStatus + " status\n"
                return (articleStatus, articleType, False, log, False)

    def parse_sections(self):
        log = ""
        sections = []
        malformed = False
        sectionsSplit = re.split(r'\s==(?=[A-z])(?!-->)', self.text)
        lead = sectionsSplit[0]
        if len(sectionsSplit) > 1:
            for section in sectionsSplit[1:]:
                newSection = Section(self, section, '==')
                if newSection.malformed:
                    malformed = True
                self.log += newSection.log
                sections.append(newSection)

        return (lead, sections, log, malformed)

    def determine_city_type(self):
        if "==Districts==" in self.text: # only huge cities have districts
            return "huge city"
        elif "==Learn==" in self.text or "==Work==" in self.text or "==Cope==" in self.text: # these are unique to the big city template
            return "big city"
        else:
            return "small city"

    def analyze_status(self, db):
        # the article model has been constructed. now, we analyze the model
        # and programmatically analyze the article to determine its status.
        # this requires us to break out the articles by type.
        #   CITY        small city / big city / district
        #   REGION      continent / continental section / region / huge city
        # the rest are self-explanatory

        # CITY
        # For city articles, the following criterion is used:
        #   STAR:
        #       * An SVG map file is detected
        #       * No MOS deviations
        #       * Number of (non-map photos) > 1
        #       * All requirements for GUIDE are met
        #   GUIDE:
        #       * Understand section has at least 250 words of prose (N/A for districts)
        #       * Eat/Drink/Sleep sections all meet 7(+/-)2 rule. If there are >9 entries,
        #           the entries are divided up into multiple lists (Splurge/Mid-range/Budget)
        #       * MOS deviations at 19:1 ratio (19 correct MOS implementations for every one incorrect)
        #       * 60% of listings have geocoordiantes
        #       * Get in section has >=2 subsections with prose
        #       * Get around section has at least 250 words of prose and/or >=2 subsections with prose (N/A for districts)
        #       * Go next has >=3 bullet points with appropriate one-liner descriptions (N/A for districts)
        #       * All sections have at least 50 words of prose
        #       * All requirements for USABLE are met
        #   USABLE:
        #       * Get in section is not empty
        #       * Eat and Sleep each have at least one listing with contact information
        #       * See or Do section has at least one listing
        #       * All requirements for OUTLINE are met
        #   OUTLINE:
        #       * Lead section has at least one sentence (50 characters)
        #       * 75% of sections required by the template are present; all essential sections present
        #   STUB:
        #       * Requirements for OUTLINE are not met
        if self.type == "district" or self.type == "small city" or self.type == "big city":
            # outline
            tmpasm = self.template_match_percentage_and_sections_missing()
            templateMatchPercentage = tmpasm[0]
            templateSectionsMissing = tmpasm[1]
            requiredSectionsPresent = self.required_sections_present()
            leadSectionNotEmpty = ONE_SENTENCE(self.lead)
            #usable

            db.update_one( { 'title': self.title }, { '$set': { 'leadSectionNotEmpty': leadSectionNotEmpty,
                                'templateMatchPercentage': templateMatchPercentage, 'requiredSectionsPresent': requiredSectionsPresent,
                                'templateSectionsMissing': templateSectionsMissing } } )

    # returns a tuple containing the percentage of template article sections that
    # are present in the article model and a list of sections missing from the template
    def template_match_percentage_and_sections_missing(self):
        sectionsCount = 0.0
        templateSections = []
        missingSections = []

        if self.type == "district":
            templateSections = ["Get in", "See", "Do", "Buy", "Eat", "Drink", "Sleep", "Connect"]
        elif self.type == "small city":
            templateSections = ["Understand", "Get in", "Get around", "See", "Do", "Buy", "Eat", "Drink", "Sleep", "Connect", "Go next"]
        elif self.type == "big city":
            templateSections = ["Understand", "Get in", "Get around", "See", "Do", "Learn", "Work", "Buy", "Eat", "Drink", "Sleep", "Stay safe", "Connect", "Cope", "Go next"]

        for templateSection in templateSections:
            if templateSection in self or templateSection == "See" and "See and Do" in self or templateSection == "Do" and "See and Do" in self:
                sectionsCount += 1
            else:
                missingSections.append(templateSection)
        return (sectionsCount / len(templateSections), missingSections)

    # all destination articles require certain sections
    def required_sections_present(self):
        if self.type is not "district":
            return "Get in" in self and "Get around" in self and ("See" in self or "See and Do" in self) and "Eat" in self and "Sleep" in self
        else:
            return "Get in" in self and ("See" in self or "See and Do" in self) and "Eat" in self and "Sleep" in self

    def bsonDumpSections(self):
        return Binary(pickle.dumps(self.sections))

class Section:
    def __init__(self, parent, section, sectionPrefix):
        self.parent = parent
        self.log = ""
        self.listings = []
        self.lead = ""
        self.subsections = []
        self.malformed = False
        try:
            self.title = section[0:section.index(sectionPrefix)]
            self.text = section[section.index(sectionPrefix):]
        except:
            # TODO: Remove if/else casing -- this is temporary for debugging. All exceptions should be treated as a malformed section
            if '==' in section or 'By train' in section or re.search(r'(Get in|Get around|See|Do|Buy|Eat|Drink|Sleep|Connect|Go next)=', section) is not None:
                self.log += "\t!! MALFORMED SECTION !!\n"
                self.title = "!! MALFORMED SECTION !!"
                self.text = section
                self.malformed = True
            else:
                print section
                sys.exit()
        newPrefix = sectionPrefix + r'='
        if newPrefix in section:
            subsectionsSplit = re.split(r'\s' + newPrefix + r'(?=[A-z])(?!-->)', self.text)
            self.lead = subsectionsSplit[0]
            if len(subsectionsSplit) > 1:
                for subsection in subsectionsSplit[1:]:
                    newSubsection = Section(self, subsection, newPrefix)
                    if newSubsection.malformed:
                        self.malformed = True
                    self.subsections.append(newSubsection)

    def toHtml(self):
        html = "\n<li>" + self.title
        if len(subsections) > 0:
            html += "\n</ul>"
            for subsection in subsections:
                html += subsection.toHtml()
            html += "\n</ul>\n"
        html += "</li>"
