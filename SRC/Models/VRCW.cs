using System.Collections.Concurrent;
using MagmaMC.Unity.PSC;

namespace VRCWMT.Models;
public class VRCW
{
    public string WorldName { get; set; } = "";
    public string WorldDescription { get; set; } = "";
    public string WorldCreator { get; set; } = "";
    public ThreadList<string> SiteAdmins { get; set; } = new();
    public ConcurrentDictionary<string, ConcurrentDictionary<string, PlayerItem>> PermissionsData { get; set; } = new();
    public ConcurrentDictionary<string, string> GroupPermissions { get; set; } = new();
    public ThreadList<Post> Posts { get; set; } = new();

    public bool PermissionsUpdated { get; set; } = false;
}
public class PlayerItem
{
    public string displayName { get; set; } = "";
    public string PlayerID { get; set; } = "";
    public DateTime Added { get; set; } = DateTime.Now;
    public string AddedBy { get; set; } = "";
}
