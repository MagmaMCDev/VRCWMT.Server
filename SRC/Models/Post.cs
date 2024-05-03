namespace VRCWMT.Models;

public class Post
{
    public string OriginalPoster { get; set; } = "";
    public string HeaderName { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime PostTime { get; set; } = DateTime.Now;
    public List<PostReply> Replies { get; set; } = new List<PostReply>();
}
