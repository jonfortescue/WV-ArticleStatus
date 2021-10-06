# -*- coding: utf-8 -*-
from lxml import etree
from pathlib2 import Path
from pymongo import MongoClient
import bz2
import sys
import urllib

# without this, encoding issues cause errors
reload(sys)
sys.setdefaultencoding('utf-8')

# the file path of the semimonthly dump
# obtained from https://dumps.wikimedia.org/enwikivoyage/latest/enwikivoyage-latest-pages-articles.xml.bz2
DUMP_FILE_PATH = "dump/enwikivoyage-latest-pages-articles.xml"
if not Path(DUMP_FILE_PATH).is_file() or len(sys.argv) > 1 and (sys.argv[1] == "--redownload" or sys.argv[1] == "-r"):
    print "Downloading latest database dump..."
    compressed_dump_path = "dump/enwikivoyage-latest-pages-articles.xml.bz2"
    urllib.urlretrieve("https://dumps.wikimedia.org/enwikivoyage/latest/enwikivoyage-latest-pages-articles.xml.bz2", compressed_dump_path)
    print "Dump downloaded. Extracting..."
    with open(DUMP_FILE_PATH, 'wb') as dump_file, bz2.BZ2File(compressed_dump_path, 'rb') as dump_bz2:
        for data in iter(lambda: dump_bz2.read(100 * 1024), b''):
            dump_file.write(data)
    print "Dump extracted."

# connect with mongodb before doing anything else
print "Connecting to MongoDB..."
client = MongoClient()
db = client.app
pages = db.pages
print "Erasing MongoDB before update..."
pages.remove()  # remove everything before we rebuild
print "Erase complete."

print "Building database..."
# quickly loop through the list of articles, tossing out redirects and special pages
for event, element in etree.iterparse(DUMP_FILE_PATH, tag="{http://www.mediawiki.org/xml/export-0.10/}page"):
    title = element.findtext("{http://www.mediawiki.org/xml/export-0.10/}title")
    if (":" not in title and element.find("{http://www.mediawiki.org/xml/export-0.10/}redirect") is None):
        _id = int(element.findtext("{http://www.mediawiki.org/xml/export-0.10/}id"))
        revision = element.find("{http://www.mediawiki.org/xml/export-0.10/}revision")
        text = revision.findtext("{http://www.mediawiki.org/xml/export-0.10/}text")
        page = {"title": title, "_id": _id, "text": text}
        pages.insert_one(page)
    element.clear()
print "Complete!"
