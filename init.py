from lxml import etree
from pymongo import MongoClient

DUMP_FILE_PATH = "dump/enwikivoyage-latest-pages-articles.xml"

print "didn't fuck this up"

client = MongoClient()
db = client.app

print "made it"

for event, element in etree.iterparse(DUMP_FILE_PATH, encoding='unicode', tag="{http://www.mediawiki.org/xml/export-0.10/}page"):
    for child in list(element):
        print str(child.tag) + ":\t\t" + str(child.text)
    element.clear()
