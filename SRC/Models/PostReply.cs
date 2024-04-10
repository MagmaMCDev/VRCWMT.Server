namespace ServerBackend;

public class PostReply
{
    public string Username { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime PostTime { get; set; } = DateTime.Now;
}
