namespace ChattyDuck.Edge.Messanger;

public class ChatGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public List<string> Members { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}