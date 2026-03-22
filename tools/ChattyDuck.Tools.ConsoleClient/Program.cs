using Microsoft.AspNetCore.SignalR.Client;

var hubConnection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/chat")
    .Build();

hubConnection.StartAsync().Wait();

while (true)
{
    Console.WriteLine("Enter a method to ivoke to the chat (or 'exit' to quit):");
    var method = Console.ReadLine();
    
    if (method is null || method.Equals("exit", StringComparison.OrdinalIgnoreCase))  {
        break;
    } 

    Console.WriteLine("Enter the argumeent to ivoke to the chat:");
    var argument = Console.ReadLine() ?? string.Empty;
    
    hubConnection.InvokeAsync(method, argument).Wait();
}