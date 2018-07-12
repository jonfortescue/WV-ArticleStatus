from pymongo import MongoClient
from models import Article
import sys

# without this, encoding issues cause errors
reload(sys)
sys.setdefaultencoding('utf-8')

# connect with mongodb
client = MongoClient()
db = client.app
pages = db.pages

def analyze(title):
    page = pages.find_one({'title': title})
    Article(pages, page['title'], page['text'])

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
