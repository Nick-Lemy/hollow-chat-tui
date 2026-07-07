using InTheHand.Net.Sockets;

// Both sides must use the same service GUID.
var serviceId = new Guid("b3f5a1c0-1111-2222-3333-444455556666");

Console.Write("[H]ost or [J]oin? ");
var choice = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();

Stream stream;

if (choice.Equals("h", StringComparison.CurrentCultureIgnoreCase))
{
    // HOST: wait for one client
    var listener = new BluetoothListener(serviceId);
    listener.Start();
    Console.WriteLine("Waiting for someone to connect...");
    var client = listener.AcceptBluetoothClient(); // blocks until a client connects
    stream = client.GetStream();
    Console.WriteLine("Connected!");
}
else if(choice.Equals("j", StringComparison.CurrentCultureIgnoreCase))
{
    // CLIENT: find the host and connect
    var bt = new BluetoothClient();
    Console.WriteLine("Scanning for nearby devices...");
    var devices = bt.DiscoverDevices();

    for (int i = 0; i < devices.Count; i++)
        Console.WriteLine($"[{i}] {devices.ElementAt(i).DeviceName} ({devices.ElementAt(i).DeviceAddress})");

    Console.Write("Pick the host number: ");
    var isParsed = int.TryParse(Console.ReadLine() ?? "-1", out int pick);
    if (!isParsed || pick < 0 || pick >= devices.Count)
    {
        Console.WriteLine("Invalid choice.");
        return;
    }

    bt.Connect(devices.ElementAt(pick).DeviceAddress, serviceId);
    stream = bt.GetStream();
    Console.WriteLine("Connected!");
}
else 
{
    Console.WriteLine("Invalid choice.");
    return;
}

// Wrap the raw stream so we can send/receive lines of text.
var reader = new StreamReader(stream);
var writer = new StreamWriter(stream) { AutoFlush = true };

// Print incoming messages in the background.
_ = Task.Run(async () =>
{
    string? line;
    while ((line = await reader.ReadLineAsync()) != null)
        Console.WriteLine($"\nThem: {line}");
    Console.WriteLine("* They disconnected.");
});

// Send whatever you type.
Console.WriteLine("Type a message and press Enter:\n");
string? input;
while ((input = Console.ReadLine()) != null)
    await writer.WriteLineAsync(input);
