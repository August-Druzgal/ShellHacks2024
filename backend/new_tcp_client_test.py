import socket
import struct
import json

# Constants for the TCP client
TCP_HOST = '127.0.0.1'  # Server IP address. Change to the server's IP if not running locally.
TCP_PORT = 16           # Port number should match the server's port

def receive_all(sock, n):
    """ Helper function to ensure we receive exactly n bytes from the socket """
    data = b''
    while len(data) < n:
        packet = sock.recv(n - len(data))
        if not packet:
            return None
        data += packet
    return data

def connect_to_server(host, port):
    """ Connect to the server and continuously receive data """
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as client_socket:
        try:
            client_socket.connect((host, port))
            print(f"Connected to server at {host}:{port}")

            while True:
                # Receive the length of the data (first 4 bytes)
                data_length_bytes = receive_all(client_socket, 4)
                if not data_length_bytes:
                    print("Connection closed by the server.")
                    break

                # Unpack the length to an integer
                data_length = struct.unpack("!I", data_length_bytes)[0]

                # Receive the actual data based on the length received
                data = receive_all(client_socket, data_length)
                if not data:
                    print("Connection closed by the server.")
                    break

                # Decode the data to json
                decoded_data = json.loads(data.decode('utf-8'))
                print("Received:", decoded_data)

        except Exception as e:
            print(f"An error occurred: {e}")

if __name__ == "__main__":
    connect_to_server(TCP_HOST, TCP_PORT)
