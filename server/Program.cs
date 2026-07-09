using Chat.Handlers;
using Chat.Networking;
using Chat.Rooms;

var rooms = new RoomRegistry();
var connections = new ConnectionRegistry();
var router = new EnvelopeRouter(rooms, connections);

await new ChatServer(router, connections).RunAsync();
