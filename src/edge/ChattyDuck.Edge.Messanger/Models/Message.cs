namespace ChattyDuck.Edge.Messanger;

public class Message
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string GroupId { get; set; }
    public string UserId { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}