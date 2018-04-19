# -*- coding: utf-8 -*-
from lxml import etree
from pymongo import MongoClient
import sys

# without this, encoding issues cause errors
reload(sys)
sys.setdefaultencoding('utf-8')

 # the file path of the semimonthly dump
 # obtained from https://dumps.wikimedia.org/enwikivoyage/latest/enwikivoyage-latest-pages-articles.xml.bz2
 # TODO: automate getting this dump file
DUMP_FILE_PATH = "dump/enwikivoyage-latest-pages-articles.xml"

# connect with mongodb before doing anything else
client = MongoClient()
db = client.app
pages = db.pages
pages.remove() # remove everything before we rebuild

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
