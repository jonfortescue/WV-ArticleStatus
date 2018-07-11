class Article:
    "Articles are basically like special sections"
    statusRegex = re.compile(r'{{(outline|usable|guide|star|disamb|disambig|disambiguation|stub|extra|historical|gallerypageof|title-index page)(district|city|airport|park|diveguide|region|country|continent|itinerary|topic|phrasebook)?(\|(?:subregion=(?:yes|no)|[\w /()-\.]+))?}}', re.I)

    def __init__(self, db, title, url, text):
        self.title = title
        self.url = url
        self.text = text
        (self.status, self.type, self.malformed, self.log, delete) = determine_status_and_type()
        if delete:
            db.delete_one({'title': self.title})
        else:
            db.update_one( {'title': self.title}, { '$set': {'status': self.status, 'type': self.type, 'malformed': self.malformed} })
            (self.lead, self.sections, log, self.malformed) = parse_sections()
            self.log += log
            db.update_one( {'title': self.title}, { '$set': {'sections': self.sections, 'malformed': self.malformed} })
        print self.log

    def determine_status_and_type():
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
                log += "Article " + title + " is a disambiguation article. Deleting from MongoDB...\n"
                return ('disambiguation', 'disambiguation', False, log, True)
            # we also want to remove gallery articles
            elif articleStatus == "gallerypageof":
                log += "Article " + title + " is a gallery article. Deleting from MongoDB...\n"
                return ('gallery', 'gallery', False, log, True)
            # and all title/index pages
            elif articleStatus == "title-index page":
                log +=  "Article " + title + " is a title/index page. Deleting from MongoDB...\n"
                return ('title/index', 'title/index', False, log, True)
            elif articleStatus == "historical":
                log += "Article" + title + " is archived as inactive/historical.\n"
                return ('historical', 'historical', False, log, False)
            # next we find stub articles -- stubs don't have an article type, so they get marked as 'stub' for both
            elif articleStatus == "stub":
                log += "Article " + title + " is a stub.\n"
                return ('stub', 'stub', False, log, False)
            # if the template is malformed...
            elif statusTemplateMatch.group(2) is None:
                log +=  "Article " + title + " has a malformed status template\n"
                return ('none', 'none', True, log, False)
            else:
                if articleStatus == "extra":
                    articleStatus = "extra-hierarchical"
                articleType = statusTemplateMatch.group(2).lower()
                if (articleType) == "diveguide": # correct dive guide to make it prettier
                    articleType = "dive guide"
                if articleType == "city":
                    articleType = determine_city_type()
                log += "Article " + title + " is a(n) " + articleType + " article of " + articleStatus + " status\n"
                return (articleStatus, articleType, False, log, False)

    def parse_sections():
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
                sections += newSection

        return (lead, sections, log, malformed)

    def determine_city_type():
        if "==Districts==" in self.text: # only huge cities have districts
            return "huge city"
        elif "==Learn==" in self.text or "==Work==" in self.text or "==Cope==" in self.text: # these are unique to the big city template
            return "big city"
        else:
            return "small city"

class Section:
    def __init__(self, parent, section, sectionPrefix):
        self.parent = parent
        self.log = ""
        self.listings = []
        self.lead = ""
        self.subsections = []
        self.malformed = False
        try:
            self.title = storageifySectionTitle(section[0:section.index(sectionPrefix)])
            self.text = section[section.index(sectionPrefix):]
        except:
            # TODO: Remove if/else casing -- this is temporary for debugging. All exceptions should be treated as a malformed section
            if '==' in section or 'By train' in section or re.search(r'(Get in|Get around|See|Do|Buy|Eat|Drink|Sleep|Connect|Go next)=', section) is not None:
                self.log += "!! MALFORMED SECTION !!"
                self.title = "!! MALFORMED SECTION !!"
                self.text = section
                self.malformed = True
            else:
                print section
                sys.exit())
        newPrefix = sectionPrefix + r'='
        if newPrefix in section:
            subsectionsSplit = re.split(r'\s' + newPrefix + r'(?=[A-z])(?!-->)', self.text)
            self.lead = subsectionsSplit[0]
            if len(subsectionsSplit) > 1:
                for subsection in subsectionsSplit[1:]:
                    newSubsection = Section(self, subsection, newPrefix)
                    if newSubsection.malformed:
                        self.malformed = True
                    self.subsections += newSection

    # python dicts can't handle '.' characters in key titles -- so we replace those with ';' for storageifiedTitle
    def storageifySectionTitle(sectionTitle):
        return sectionTitle.replace('.', ';')
