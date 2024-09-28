import socket
import cv2
import numpy as np
import struct
import torch
import threading
import warnings
import logging
import json

warnings.filterwarnings("ignore", category=FutureWarning)
logging.basicConfig(filename='server.log', level=logging.DEBUG, format='%(asctime)s - %(levelname)s - %(message)s')

# Load YOLOv5 model
model = torch.hub.load('./yolov5', 'custom', path='yolov5s.pt', source='local')

# Constants for TCP server
TCP_HOST = '0.0.0.0'
TCP_PORT = 16

# TCP Server initialization
tcp_server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
tcp_server_socket.bind((TCP_HOST, TCP_PORT))
tcp_server_socket.listen(5)
print(f"TCP Server listening on {TCP_HOST}:{TCP_PORT}")

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
        # Log each detected object
        logging.info(f'Detected {model.names[int(label.item())]} at X: {int(x_center)}, Y: {int(y_center)}')
    return detected_objects

def handle_tcp_client(client_socket):
    data = b""
    payload_size = struct.calcsize("Q")  # 8 bytes for unsigned long long

    try:
        while True:
            while len(data) < payload_size:
                packet = client_socket.recv(4096)
                if not packet:
                    return
                data += packet
            packed_msg_size = data[:payload_size]
            data = data[payload_size:]
            msg_size = struct.unpack("Q", packed_msg_size)[0]

            while len(data) < msg_size:
                data += client_socket.recv(4096)
            frame_data = data[:msg_size]
            data = data[msg_size:]

            # Decode image data using OpenCV
            nparr = np.frombuffer(frame_data, np.uint8)
            frame = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
            if frame is None:
                logging.error("Failed to decode image.")
                continue
            logging.debug("Received frame for processing.")

            # Perform object detection
            objects_detected = detect_objects(frame)
            logging.debug("Object detection completed.")

            # Send results back to the client as JSON
            response = json.dumps({"objects": objects_detected}).encode('utf-8')
            response_length = struct.pack("Q", len(response))
            client_socket.sendall(response_length + response)
            logging.debug(f"Sent detection results of size {len(response)} bytes.")

    except Exception as e:
        logging.error(f"Error handling client: {e}")
    finally:
        client_socket.close()
        logging.debug("Closed connection with client.")

def start_tcp_server():
    while True:
        client_socket, addr = tcp_server_socket.accept()
        print('TCP Connection from:', addr)
        client_handler = threading.Thread(target=handle_tcp_client, args=(client_socket,))
        client_handler.start()

if __name__ == "__main__":
    # Start the TCP server
    start_tcp_server()
    logging.info("TCP server started.")