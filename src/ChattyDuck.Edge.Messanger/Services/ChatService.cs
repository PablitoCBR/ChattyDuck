using System.Collections.Concurrent;

namespace ChattyDuck.Edge.Messanger;

public class ChatService
{
    // This has to be replaced with a proper database keepint the information about the user groups to fetch upon connection
    private readonly ConcurrentDictionary<string, ChatGroup> _groups = new();
    private readonly ConcurrentDictionary<string, List<Message>> _messages = new();

    public async Task<ChatGroup> CreateGroupAsync(string name, string creatorId)
    {
        var group = new ChatGroup { Name = name };
        group.Members.Add(creatorId);
        _groups[group.Id] = group;
        _messages[group.Id] = new List<Message>();
        return group;
    }

    public async Task<ChatGroup?> GetGroupAsync(string groupId)
    {
        _groups.TryGetValue(groupId, out var group);
        return group;
    }

    public async Task<Message> AddMessageAsync(string groupId, string userId, string content)
    {
        var message = new Message { GroupId = groupId, UserId = userId, Content = content };
        _messages[groupId].Add(message);
        return message;
    }

    public async Task<List<Message>> GetMessagesAsync(string groupId)
    {
        return _messages.TryGetValue(groupId, out var msgs) ? msgs : new List<Message>();
    }
}