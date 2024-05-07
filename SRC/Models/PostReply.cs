namespace VRCWMT.Models;

public class PostReply
{
    public string username { get; set; } = "";
    public string text { get; set; } = "";
    public DateTime postTime { get; set; } = DateTime.Now;
}
