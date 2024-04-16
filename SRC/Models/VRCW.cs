using System.Collections.Concurrent;
namespace ServerBackend.Models;
public class VRCW
{
    public string WorldName { get; set; } = "";
    public string WorldDescription { get; set; } = "";
    public string WorldCreator { get; set; } = "";
    public ThreadList<string> SiteAdmins { get; set; } = new();
    public ConcurrentDictionary<string, PlayerItem> BannedPlayers { get; set; } = new();
    public ThreadList<Post> Posts { get; set; } = new();

}
public class PlayerItem
{
    public string PlayerID { get; set; } = "";
    public DateTime Added { get; set; } = DateTime.Now;
    public string AddedBy { get; set; } = "";
}
