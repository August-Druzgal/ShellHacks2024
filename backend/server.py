import socket
import cv2
import numpy as np
import struct
import torch
import threading
import warnings
import logging
import json
import mss
import time
from Quartz import CGWindowListCopyWindowInfo, kCGWindowListOptionOnScreenOnly, kCGNullWindowID

warnings.filterwarnings("ignore", category=FutureWarning)
logging.basicConfig(filename='server.log', level=logging.DEBUG, format='%(asctime)s - %(levelname)s - %(message)s')

# Load YOLOv5 model
model = torch.hub.load('./yolov5', 'custom', path='yolov5s.pt', source='local')

# Constants for TCP server
TCP_HOST = '0.0.0.0'
TCP_PORT = 16

# Global interval variable (in seconds)
SEND_INTERVAL = 1.0  # Adjust this value as needed

# Global variable to store detected objects
detected_objects = []
detected_objects_lock = threading.Lock()

# TCP Server initialization
tcp_server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
tcp_server_socket.bind((TCP_HOST, TCP_PORT))
tcp_server_socket.listen(5)
print(f"TCP Server listening on {TCP_HOST}:{TCP_PORT}")

def get_chrome_window_bounds():
    # Retrieve all on-screen windows
    window_list = CGWindowListCopyWindowInfo(kCGWindowListOptionOnScreenOnly, kCGNullWindowID)
    # Iterate through windows to find Google Chrome
    for window in window_list:
        # The window owner name could be 'Google Chrome'
        owner_name = window.get('kCGWindowOwnerName', '')
        if owner_name == 'Google Chrome':
            bounds = window.get('kCGWindowBounds', {})
            x = int(bounds.get('X', 0))
            y = int(bounds.get('Y', 0))
            width = int(bounds.get('Width', 0))
            height = int(bounds.get('Height', 0))
            return {'left': x, 'top': y, 'width': width, 'height': height}
    return None

def handle_tcp_client(client_socket):
    try:
        while True:
            # Get the latest detected_objects
            with detected_objects_lock:
                current_objects = detected_objects.copy()

            # Send results back to the client as JSON
            response = json.dumps({"objects": current_objects}).encode('utf-8')
            response_length = struct.pack("!I", len(response))  # Use network byte order for consistency
            client_socket.sendall(response_length + response)
            logging.debug(f"Sent detection results of size {len(response)} bytes.")

            # Wait for the specified interval
            time.sleep(SEND_INTERVAL)
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

def image_processing_loop():
    global detected_objects
    # Get the Google Chrome window coordinates
    bounds = get_chrome_window_bounds()
    if bounds is None:
        logging.error('Google Chrome window not found.')
        return

    monitor = {
        'left': bounds['left'],
        'top': bounds['top'],
        'width': bounds['width'],
        'height': bounds['height']
    }

    with mss.mss() as sct:
        while True:
            # Capture the screen region corresponding to Chrome window
            screenshot = sct.grab(monitor)
            frame = np.array(screenshot)

            # Convert from BGRA to BGR
            frame = cv2.cvtColor(frame, cv2.COLOR_BGRA2BGR)

            # Perform object detection
            results = model(frame)

            # Extract detected objects information
            objects = []
            labels, coords = results.xyxyn[0][:, -1], results.xyxyn[0][:, :-1]
            h, w, _ = frame.shape

            for label, coord in zip(labels, coords):
                x_min, y_min, x_max, y_max, confidence = coord
                x_min = int(x_min * w)
                y_min = int(y_min * h)
                x_max = int(x_max * w)
                y_max = int(y_max * h)
                label_name = model.names[int(label.item())]

                # Draw bounding box
                cv2.rectangle(frame, (x_min, y_min), (x_max, y_max), (0, 255, 0), 2)

                # Put label text
                cv2.putText(frame, f"{label_name} {confidence:.2f}", (x_min, y_min - 10),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 2)

                # Prepare object data
                x_center = (x_min + x_max) // 2
                y_center = (y_min + y_max) // 2
                objects.append({
                    "label": label_name,
                    "x": x_center,
                    "y": y_center
                })

                # Log each detected object
                logging.info(f'Detected {label_name} at X: {x_center}, Y: {y_center}')

            logging.debug("Object detection completed.")

            # Update the global detected_objects variable
            with detected_objects_lock:
                detected_objects = objects.copy()

            # Display the frame with bounding boxes
            cv2.imshow("Object Detection", frame)
            cv2.waitKey(1)  # Display the frame for 1ms, allows window to update

            # Wait for the specified interval
            time.sleep(SEND_INTERVAL)

if __name__ == "__main__":
    # Start the TCP server in a separate thread
    tcp_server_thread = threading.Thread(target=start_tcp_server)
    tcp_server_thread.start()
    logging.info("TCP server started.")

    # Start the image processing loop in the main thread
    image_processing_loop()
    logging.info("Image processing loop started.")
