import socket
import cv2
import struct
import numpy as np
import json

def recvall(sock, n):
    """Helper function to receive n bytes or return None if EOF is hit."""
    data = b''
    while len(data) < n:
        packet = sock.recv(n - len(data))
        if not packet:
            return None
        data += packet
    return data

def main():
    HOST = 'localhost'  # Server's IP address (localhost)
    PORT = 10           # Server's port

    # Create a TCP/IP socket
    client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    client_socket.connect((HOST, PORT))

    # Open video file
    cap = cv2.VideoCapture('test.mp4')

    try:
        while True:
            ret, frame = cap.read()
            if not ret:
                print("End of video or cannot read the frame.")
                break

            # Encode frame as JPEG
            ret, buffer = cv2.imencode('.jpg', frame)
            if not ret:
                print("Failed to encode frame.")
                continue

            frame_data = buffer.tobytes()
            msg_size = len(frame_data)

            # Pack the size of the frame data
            packed_msg_size = struct.pack('!I', msg_size)

            # Send the size and frame data
            client_socket.sendall(packed_msg_size + frame_data)

            # Receive the response size
            response_size_packed = recvall(client_socket, 4)
            if not response_size_packed:
                print("Server closed the connection.")
                break

            response_size = struct.unpack('!I', response_size_packed)[0]

            # Receive the response data
            response_data = recvall(client_socket, response_size)
            if not response_data:
                print("Failed to receive response data.")
                break

            # Decode and print the JSON response
            response_json = response_data.decode('utf-8')
            response = json.loads(response_json)
            print("Received response:", response)

    except Exception as e:
        print("An exception occurred:", e)
    finally:
        client_socket.close()
        cap.release()
        print("Connection closed.")

if __name__ == "__main__":
    main()