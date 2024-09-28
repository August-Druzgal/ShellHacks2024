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

# Load YOLOv5 model
model = torch.hub.load('./yolov5', 'custom', path='yolov5s.pt', source='local')

TCP_HOST = '0.0.0.0'
TCP_PORT = 10  # Changed to a non-privileged port

tcp_server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
tcp_server_socket.bind((TCP_HOST, TCP_PORT))
tcp_server_socket.listen(5)
print(f"TCP Server listening on {TCP_HOST}:{TCP_PORT}")

@app.route('/', methods=['GET'])
def get_fingers():
    pinky = 1000
    ring = random.randint(500, 2000)
    middle = random.randint(500, 2000)
    index = random.randint(500, 2000)
    thumb = random.randint(500, 2000)
    return jsonify([pinky, ring, middle, index, thumb])

def recvall(sock, n):
    """Helper function to receive n bytes or return None if EOF is hit."""
    data = b''
    while len(data) < n:
        packet = sock.recv(n - len(data))
        if not packet:
            return None
        data += packet
    return data

def detect_objects(frame):
    results = model(frame)
    labels, coords = results.xyxyn[0][:, -1], results.xyxyn[0][:, :-1]

    h, w, _ = frame.shape
    detected_objects = []

    for label, coord in zip(labels, coords):
        x_center = (coord[0].item() + coord[2].item()) / 2 * w
        y_center = (coord[1].item() + coord[3].item()) / 2 * h
        detected_objects.append({
            "label": model.names[int(label.item())],
            "x": int(x_center),
            "y": int(y_center)
        })

    return detected_objects

def handle_tcp_client(client_socket):
    payload_size = 4  # Size of unsigned int in bytes

    try:
        while True:
            # Receive message size
            packed_msg_size = recvall(client_socket, payload_size)
            if not packed_msg_size:
                print("Client disconnected.")
                break
            msg_size = struct.unpack('!I', packed_msg_size)[0]

            # Receive frame data
            frame_data = recvall(client_socket, msg_size)
            if not frame_data:
                print("Client disconnected during frame data reception.")
                break

            # Deserialize frame
            frame = cv2.imdecode(np.frombuffer(frame_data, np.uint8), cv2.IMREAD_COLOR)

            # Log receiving frames intermittently
            if random.randint(1, 10) == 1:
                print(f"[{datetime.datetime.now()}] Received frame of size {msg_size} bytes.")

            # Process frame through the model
            print(f"[{datetime.datetime.now()}] Processing frame.")
            objects_detected = detect_objects(frame)

            # Log the detected objects
            for obj in objects_detected:
                print(f"[{datetime.datetime.now()}] Detected {obj['label']} at ({obj['x']}, {obj['y']}).")

            # Serialize response as JSON
            response = json.dumps(objects_detected).encode('utf-8')
            response_length = struct.pack('!I', len(response))
            client_socket.sendall(response_length + response)

    except Exception as e:
        print(f"Exception in handle_tcp_client: {e}")
    finally:
        client_socket.close()
        print("Closed connection with client.")

def start_tcp_server():
    while True:
        client_socket, addr = tcp_server_socket.accept()
        now = datetime.datetime.now()
        print(f"[{now}] TCP Connection from: {addr}")
        client_handler = threading.Thread(target=handle_tcp_client, args=(client_socket,))
        client_handler.start()

if __name__ == "__main__":
    tcp_thread = threading.Thread(target=start_tcp_server)
    tcp_thread.start()

    app.run(host="0.0.0.0", port=11)
