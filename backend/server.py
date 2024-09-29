import socket
import cv2
import json
import struct
import torch
from flask import Flask, jsonify
import threading
import random
import datetime
import numpy as np

app = Flask(__name__)

@app.route('/', methods=['GET'])
def get_fingers():
    pinky = 1000
    ring = random.randint(500, 2000)
    middle = random.randint(500, 2000)
    index = random.randint(500, 2000)
    thumb = random.randint(500, 2000)
    return jsonify([pinky, ring, middle, index, thumb])

if __name__ == "__main__":

    app.run(host="0.0.0.0", port=11)
