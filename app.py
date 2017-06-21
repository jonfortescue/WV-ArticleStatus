from flask import *
from lxml import etree
from flask_pymongo import PyMongo

app = Flask(__name__)
mongo = PyMongo(app)

@app.route("/")
def home():
    return render_template("index.html", title="Wikivoyage Article Status Analyzer", tagline=app.name)
