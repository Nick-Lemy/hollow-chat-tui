using Chat;

Console.Write("Enter your name: ");
var myName = Console.ReadLine() ?? "Anonymous";
await Client.Run(myName);
