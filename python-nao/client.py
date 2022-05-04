import socket
import os
import struct
from struct import calcsize

########################
# Konfiguration der zu sendenden Datei
########################
dir_path = os.path.dirname(os.path.realpath(__file__))
# file = os.path.join(dir_path, "res", "19_04_2022", "state11.png")

file = os.path.join(dir_path, "res", "game", "step_1.png")
file = os.path.join(dir_path, "res", "game", "step_2.png")
file = os.path.join(dir_path, "res", "game", "step_3.png")

# Nao gewinnt :)
# file = os.path.join(dir_path, "res", "19_04_2022", "161670_camImage.png")




########################
# Konfiguration Endpoint
########################
def get_ip():
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    s.settimeout(0)
    try:
        # doesn't even have to be reachable
        s.connect(('10.255.255.255', 1))
        IP = s.getsockname()[0]
    except Exception:
        IP = '127.0.0.1'
    finally:
        s.close()
    return IP

use_local_ip = True
if use_local_ip:
    TCP_IP = get_ip()
else:
    # Wenn nicht die lokale verwendet werden soll
    TCP_IP = '192.168.56.1'

TCP_PORT = 9001

########################
# Konfiguration Weitere
########################
BUFFER_SIZE = 65536

########################
# Start
########################

print(f"Connect to Endpoint: {TCP_IP}:{TCP_PORT}")

# send image
f = open(file, 'rb')
nBytes = os.path.getsize(file)
recieved_f = [-10,-10]
packed_f = struct.pack('!ii',recieved_f[0],recieved_f[1])
print (calcsize('!ii'))

print ("file size:", nBytes, type(nBytes))


s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.connect((TCP_IP, TCP_PORT))


print(f'CLIENT: nBytes={nBytes}')

l = f.read(BUFFER_SIZE)
while (l):
    s.send(l)
    l = f.read(BUFFER_SIZE)
    if not l:
        f.close()  
        break


s.send(b"<EOF>")

############################################################

print('receiving data...')  
data = s.recv(8)
# Decode received data into UTF-8
data = struct.unpack('!ii',data)
# Convert decoded data into list
recieved_f = data    

magic_number = 16777216
state = int(recieved_f[0] / magic_number)
column = int(recieved_f[1] / magic_number)

column = column + 1

print('state: ',state)
print('column: ',column)
    
print('Successfully get the file')


s.close()
print('connection closed')

