using System.Runtime.Versioning;
using Microsoft.AspNetCore.Http;

namespace ServerBackend;
public class VRCW
{
    public string WorldName { get; set; } = "";
    public string WorldDescription { get; set; } = "";
    public string WorldCreator { get; set; } = "";
    public List<string> SiteAdmins { get; set; } = new List<string>();
    public List<string> BannedPlayers { get; set; } = new List<string>();
    public List<Post> Posts { get; set; } = new List<Post>();

}
