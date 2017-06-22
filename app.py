from flask import *
from flask_pymongo import PyMongo
import sys

# without this, encoding issues cause errors
reload(sys)
sys.setdefaultencoding('utf-8')

app = Flask(__name__)
mongo = PyMongo(app) # establish MongoDB connection

def urlizeTitle(title): # switch title to Wikivoyage style title
    return title.replace(' ','_')
def titleToUrl(title):
    return "https://en.wikivoyage.org/wiki/" + urlizeTitle(title)
def unUrlizeTitle(urlizedTitle):
    return urlizedTitle.replace('_',' ')

@app.route("/")
def home():
    allPages = mongo.db.pages.find()
    return render_template("index.html", title="Wikivoyage Article Status Analyzer", pages=allPages)

@app.route("/<pagetitle>")
def pageDisplay(pagetitle):
    title = unUrlizeTitle(pagetitle)
    page = mongo.db.pages.find_one_or_404({"title": title})
    return render_template("page.html", page=page, url=titleToUrl(title))
