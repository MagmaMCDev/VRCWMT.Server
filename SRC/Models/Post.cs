namespace VRCWMT.Models;

public class Post
{
    public string originalPoster { get; set; } = "";
    public string headerName { get; set; } = "";
    public string description { get; set; } = "";
    public DateTime postTime { get; set; } = DateTime.Now;
    public List<PostReply> replies { get; set; } = new List<PostReply>();
}
