import re
from pymongo import MongoClient
import sys

# without this, encoding issues cause errors
reload(sys)
sys.setdefaultencoding('utf-8')

# windows compat
if sys.platform == "win32":
    try:
        import uniconsole
    except ImportError:
        sys.exc_clear()  # could be just pass, of course
    else:
        del uniconsole  # reduce pollution, not needed anymore

# connect with mongodb
client = MongoClient()
db = client.app
pages = db.pages

# enums declared with this fantastic code found here https://stackoverflow.com/a/1695250
def enum(*sequential, **named):
    enums = dict(zip(sequential, range(len(sequential))), **named)
    reverse = dict((value, key) for key, value in enums.iteritems())
    enums['reverse_mapping'] = reverse
    return type('Enum', (), enums)

def analyze(title):
    text = pages.find_one({ 'title': title })['text']

    # first, we determine what kind of article this is
    # we regex to find the article status template and then parse it
    statusRegex = re.compile(r'{{(outline|usable|guide|star|disamb|stub)(district|city|airport|park|diveguide|region|country|continent|itinerary|topic|phrasebook)?}}', re.I)
    statusTemplateMatch = statusRegex.search(text)
    if statusTemplateMatch is None:
        print "Article " + title + " has a malformed or no status template."
        pages.update_one( {'title': title}, { '$set': {'status': 'none', 'type': 'none'} })
    else:
        # our regex has the advantage of having two capture groups which correspond to status and type
        articleStatus = statusTemplateMatch.group(1).lower()
        # we want to remove all disambiguation articles from the database
        if articleStatus == "disamb":
            print "Article " + title + " is a disambiguation article. Deleting from MongoDB..."
            pages.delete_one({'title': title})
            return
        # next we find stub articles -- stubs don't have an article type, so they get marked as 'stub' for both
        elif articleStatus == "stub":
            print "Article " + title + " is a stub."
            pages.update_one( {'title': title}, { '$set': {'status': 'stub', 'type': 'stub'} })
        # if the template is malformed...
        elif statusTemplateMatch.group(2) is None:
            print "Article " + title + " has a malformed status template"
            pages.update_one( {'title': title}, { '$set': {'status': 'none', 'type': 'none'} })
        # finally, if it's not a stub or a disambiguation article, it's a real article! so let's note its status
        else:
            articleType = statusTemplateMatch.group(2).lower()
            if (articleType) == "diveguide": # correct dive guide to make it prettier
                articleType = "dive guide"
            if articleType == "city":
                articleType = determine_city_type(text)
            pages.update_one( {'title': title}, { '$set': {'status': articleStatus, 'type': articleType} })
            print "Article " + title + " is a(n) " + articleType + " article of " + articleStatus + " status"

            # now the fun begins
            # first, we build a model of the article
            if '==' in text:
                lead_section = text[0:text.index('==')]
                sectionsRe = re.findall(r'(={2,})([^=]+)={2,}', text)
                article_model = []
                index = 1
                for section in sectionsRe:
                    startIndex = text.index(section[1] + "==") + len(section[1]) + len(section[0])
                    endIndex = len(text) - 1
                    if index < len(sectionsRe):
                        endIndex = text.index("==", startIndex)
                    article_model.append({ "title": section[1], "text": text[startIndex:endIndex], "depth": len(section[0]) - 2 })
                    index += 1
                pages.update_one( { 'title': title }, { '$set': { 'model': article_model } } )
            else:
                lead_section = text
            article_model.insert(0, { "title": "Lead", "text": lead_section, "depth": 0})


# just loop through and analyze all articles in the database
def analyze_all():
    cursor = pages.find(no_cursor_timeout=True)
    for page in cursor:
        analyze(page['title'])
    cursor.close()

def determine_city_type(text):
    if "==Districts==" in text: # only huge cities have districts
        return "huge city"
    elif "==Learn==" in text or "==Work==" in text or "==Cope==" in text: # these are unique to the big city template
        return "big city"
    else:
        return "small city"

if __name__ == '__main__':
    if len(sys.argv) == 1:
        analyze_all()
    elif len(sys.argv) == 2:
        analyze(sys.argv[1])
    else:
        print "Invalid arguments."
