import re
from pymongo import MongoClient
from models import Article, Section
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

# just loop through and analyze all articles in the database
def analyze_all():
    cursor = pages.find(no_cursor_timeout=True)
    for page in cursor:
        Article(pages, page['title'], page['text'])
    cursor.close()

if __name__ == '__main__':
    if len(sys.argv) == 1:
        analyze_all()
    elif len(sys.argv) == 2:
        analyze(sys.argv[1])
    else:
        print "Invalid arguments."
