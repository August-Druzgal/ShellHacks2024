import bluetooth

#
#   To use this:
#
#   - Open Bluetooth & Devices in Settings
#   - Connect to August Glove Device
#   - Run program to set vibration
#
#   index 0 is thumb, index 4 is pinky
#

device_mac_address = "A0:DD:6C:77:C6:92"
port = 3

sock = bluetooth.BluetoothSocket(bluetooth.RFCOMM)

try:
    print(f"Connecting to {device_mac_address}...")
    sock.connect((device_mac_address, port))
    print("Connected!")

    # Every time you send data, make sure it's exactly 5 bytes
    # 1 is the fastest, 255 is the slowest, 0 is off

    data_to_send = bytes([0x00, 0x00, 0x00, 0x00, 0x00]) 
    
    sock.send(data_to_send)
    print(f"Sent: {data_to_send}")

except bluetooth.btcommon.BluetoothError as e:
    print(f"Bluetooth error: {e}")

finally:
    sock.close()
    print("Connection closed.")