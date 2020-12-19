Example scene that demonstrates the use of the NobleNetworkManager component to connect and send data
using the Mirror networking system.

The buttons and text boxes can be used to host a server or connect as a client. When running as a host, the 
host IP and port are displayed. When running as a client, the host IP and port can be entered in the text boxes 
to connect to the host.

When a client connects, a player will be spawned that can be moved around with the arrow keys.

The connection type will be displayed on the client:
DIRECT - The connection was made directly to the host's IP.
PUNCHTHROUGH - The connection was made to an address on the host's router discovered via punchthrough.
RELAY - The connection is using the Noble Connect relays.