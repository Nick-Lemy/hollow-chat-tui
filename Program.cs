using Chat;

Console.Write("Enter your name: ");
var myName = Console.ReadLine() ?? "Anonymous";

Console.Write("Do you want to be the host? (y/n): ");
var answer = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();
var isHost = answer is "y" or "yes" or "h" or "host";

if (isHost)
    await Host.Run(myName);
else
    await Client.Run(myName);
