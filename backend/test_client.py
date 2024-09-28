import socket
import requests
import cv2
import pickle
import struct
import time

def test_http_get():
    try:
        response = requests.get("https://shellhacks2024.ngrok.dev")
        if response.status_code == 200:
            print("HTTP GET Response:", response.json())
        else:
            print(f"Failed to get response from HTTP endpoint, status code: {response.status_code}")
    except Exception as e:
        print(f"Error while making HTTP GET request: {e}")

def test_tcp_stream(video_path):
    client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    try:
        client_socket.connect(('localhost', 16))
        print("Connected to TCP server at localhost:16")

        cap = cv2.VideoCapture(video_path)
        if not cap.isOpened():
            print(f"Error: Could not open video file {video_path}")
            return

        while cap.isOpened():
            ret, frame = cap.read()
            if not ret:
                print("End of video stream")
                break

            data = pickle.dumps(frame)
            message_size = struct.pack("L", len(data))

            client_socket.sendall(message_size + data)

            data = b""
            while len(data) < struct.calcsize("L"):
                data += client_socket.recv(4096)
            packed_msg_size = data[:struct.calcsize("L")]
            data = data[struct.calcsize("L"):]
            msg_size = struct.unpack("L", packed_msg_size)[0]

            while len(data) < msg_size:
                data += client_socket.recv(4096)

            response = pickle.loads(data)
            print(f"Detected Objects: {response}")

            time.sleep(0.1)
        
        cap.release()
    except Exception as e:
        print(f"Error during TCP connection or data transmission: {e}")
    finally:
        client_socket.close()
        print("TCP connection closed")

if __name__ == "__main__":
    test_http_get()
    test_tcp_stream("test.mp4")
