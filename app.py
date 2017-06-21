from flask import Flask
from lxml import etree
from flask_pymongo import PyMongo

app = Flask(__name__)
mongo = PyMongo(app)

@app.route("/")

@app.route("/init")
def init():
    return "uh oh"
