import json
import time
import socket
import asyncio
import bluetooth_scan

def convert_to_json(devices):
    device_json = {
        "devices": [
            {"name": device.name, "mac_address": device.address} for device in devices
        ]
    }
    return device_json

async def handle_client(c, addr):
    try:
        print(f'Handling connection from {addr}')
        
        # Perform Bluetooth scanning
        # Convert the scanned devices to JSON and add EOF marker
        device_json = bluetooth_scan.get_connected_bluetooth_devices()
        #response = json.dumps(device_json)
        print(device_json)
        
        # Send the response to the client
        c.sendall(device_json.encode())
        print(f"Sent Bluetooth devices to {addr}")
        
        # Introduce a slight delay for testing purposes
        time.sleep(0.1)  # Adjust if necessary
    except Exception as e:
        print(f"Error handling client {addr}: {e}")
    finally:
        # Ensure the socket connection is closed
        c.close()
        print(f"Connection with {addr} closed")

def server():
    try:
        while True:
            print("Waiting for a new connection...")
            c, addr = s.accept()
            
            # Use asyncio to manage asynchronous scanning and handling
            asyncio.run(handle_client(c, addr))
    except KeyboardInterrupt:
        print("Stopped Server: KeyboardInterrupt")
    finally:
        s.close()
        print("Socket closed")

if __name__ == "__main__":
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    port = 3000
    s.bind(('127.0.0.1', port))
    s.listen(1)
    print(f"Socket successfully created and listening on port {port}")
    
    try:
        server()
    except Exception as e:
        print(f"Server error: {e}")