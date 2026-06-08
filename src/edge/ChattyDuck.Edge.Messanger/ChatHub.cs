using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using ChattyDuck.Edge.Messanger.Models;

namespace ChattyDuck.Edge.Messanger;

public class ChatHub : Hub
{
    private readonly ChatService _chatService;

    public ChatHub(ChatService chatService)
    {
        _chatService = chatService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        Console.WriteLine($"User connected: {userId}");

        // await Clients.User(userId).SendAsync("ReceiveMessage", "System", $"Welcome {userId}!");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        Console.WriteLine($"User disconnected: {userId}");

        await base.OnDisconnectedAsync(exception);
    }

    public async Task CreateGroup(string groupName)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId)) return;

        var group = await _chatService.CreateGroupAsync(groupName, userId);
        await Groups.AddToGroupAsync(Context.ConnectionId, group.Id);
        await Clients.User(userId).SendAsync("GroupCreated", group);
    }

    public async Task JoinGroup(string groupId)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId)) return;

        var group = await _chatService.GetGroupAsync(groupId);
        if (group == null || !group.Members.Contains(userId)) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        await Clients.User(userId).SendAsync("JoinedGroup", group);
    }

    public async Task SendMessage(string groupId, string message)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId)) return;

        var group = await _chatService.GetGroupAsync(groupId);
        if (group == null || !group.Members.Contains(userId)) return;

        var msg = await _chatService.AddMessageAsync(groupId, userId, message);
        await Clients.Group(groupId).SendAsync("ReceiveMessage", userId, message);
    }

    public async Task<List<Message>> GetGroupMessages(string groupId)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId)) return new List<Message>();

        var group = await _chatService.GetGroupAsync(groupId);
        if (group == null || !group.Members.Contains(userId)) return new List<Message>();

        return await _chatService.GetMessagesAsync(groupId);
    }
}
