from flask import Flask, render_template
from flask_pymongo import PyMongo
import sys
from models import Article, bsonLoadSections

# without this, encoding issues cause errors
reload(sys)
sys.setdefaultencoding('utf-8')

app = Flask(__name__)
mongo = PyMongo(app)  # establish MongoDB connection

def urlizeTitle(title):  # switch title from human readble to wiki url style
    return title.replace(' ', '_').replace('/', ';')
def titleToUrl(title):  # generate a link to the Wikivoyage article
    return "https://en.wikivoyage.org/wiki/" + title.replace(' ', '_')
def unUrlizeTitle(urlizedTitle):  # switch a title from wiki url style to human readable
    return urlizedTitle.replace('_', ' ').replace(';', '/')
def formatAsPercentage(floatingPt):
    return '{0:.0f}%'.format(floatingPt * 100)
def requiredSections():
    return ["Get in", "Get around", "See", "Eat", "Sleep"]

# index/home page
@app.route("/")
def home():
    allPages = mongo.db.pages.find()  # need all page titles to do autocomplete
    malformed = mongo.db.pages.find({"malformed": True})
    return render_template("index.html", title="Wikivoyage Article Status Analyzer", pages=allPages, malformed=malformed, urlizeTitle=urlizeTitle)

# individual article analysis pages
@app.route("/article/<pagetitle>")
def pageDisplay(pagetitle):
    title = unUrlizeTitle(pagetitle)  # we extract the article title from the url
    page = mongo.db.pages.find_one_or_404({"title": title})  # and then find the database entry or 404 if it doesn't exist
    sections = bsonLoadSections(page['sections'])
    return render_template("page.html", page=page, url=titleToUrl(title), sections=sections,
                           formatAsPercentage=formatAsPercentage, requiredSections=requiredSections())
