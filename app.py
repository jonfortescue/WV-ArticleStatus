from flask import *
from lxml import etree
from flask_pymongo import PyMongo
import pprint

app = Flask(__name__)
mongo = PyMongo(app)

@app.route("/")
def home():
    pages = mongo.db.pages.find()
    return render_template("index.html", title="Wikivoyage Article Status Analyzer", page=pprint.pprint(pages.finde_one()))
