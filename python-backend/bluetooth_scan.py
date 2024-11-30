import json
import subprocess
import bluetooth

def scan_nearby_devices():     
     nearby_devices = bluetooth.discover_devices(lookup_names=True)
     print("found %d devices" % len(nearby_devices))
     for addr, name in nearby_devices:
          print(" %s - %s" % (addr, name))
     
def get_connected_bluetooth_devices():
    # PowerShell command to get only currently connected Bluetooth devices
    ps_command = (
        "Get-PnpDevice -Class Bluetooth | "
        "Where-Object {$_.Status -eq 'OK'} | "
        "Select-Object Name, Status, InstanceId | "
        "ConvertTo-Json"
    )
    
    try:
        result = subprocess.run(
            [r"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe", "-Command", ps_command],
            capture_output=True,
            text=True,
            check=True
        )
        devices = json.loads(result.stdout)
        
        # Handle single device (dict) vs multiple devices (list)
        if isinstance(devices, dict):
            devices = [devices]
        
        # Build a list of device dictionaries
        device_list = []
        if devices:
            for device in devices:
                name = device.get("Name", "Unknown")
                instance_id = device.get("InstanceId", "")
                
                # Attempt to extract MAC address from InstanceId
                try:
                    mac_part = instance_id.split("_")[1]
                    mac_addr = mac_part[:12]
                    formatted_mac = ":".join(mac_addr[i:i+2] for i in range(0, 12, 2))
                except (IndexError, ValueError):
                    formatted_mac = "N/A"
                
                device_list.append({
                    "name": name,
                    "address": formatted_mac
                })
        else:
            return json.dumps({"devices": []})  # No devices found

        return json.dumps({"devices": device_list})
    
    except subprocess.CalledProcessError as e:
        return json.dumps({"error": f"An error occurred while executing PowerShell command: {e.stderr}"})

if __name__ == "__main__":
    devices_json = get_connected_bluetooth_devices()
    print(devices_json)     