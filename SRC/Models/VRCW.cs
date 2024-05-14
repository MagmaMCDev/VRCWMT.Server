using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json.Serialization;
using MagmaMC.PSC;
using OpenVRChatAPI;
using OpenVRChatAPI.Models;

namespace VRCWMT.Models;
public class VRCW
{
    public string worldName { get; set; } = "";
    public string worldDescription { get; set; } = "";
    public string worldCreator { get; set; } = "";

    [JsonIgnore]
    public ConcurrentDictionary<string, ConcurrentDictionary<string, PlayerItem>> permissionsData { get; set; } = new();
    public ConcurrentDictionary<string, ThreadList<string>> groupPermissions { get; set; } = new();

    [JsonIgnore]
    public string githubRepo { get; set; } = "";
    [JsonIgnore]
    public string github_OAuth { get; set; } = "";
    [JsonIgnore]
    public ThreadList<string> siteAdmins { get; set; } = new();
    [JsonIgnore]
    public ThreadList<string> Commits { get; set; } = new();
    public ThreadList<Post> Posts { get; set; } = new();

    public string ConvertPSC()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("//AUTO GENERATED DO NOT EDIT!".ToUpper());
        sb.AppendLine($"//VRCWMT - {DateTime.UtcNow:UTC d/M/yy h:mm tt} - V{ServerConfig.VERSION}");
        sb.AppendLine($"//WorldName: {worldName}");
        sb.AppendLine($"//WorldDescription: {worldDescription.Replace('\r', '\0').Replace('\n', ' ')}");
        sb.AppendLine($"//WorldCreator: {worldCreator}");
        sb.AppendLine();

        var sortedPermissions = groupPermissions.OrderByDescending(p => p.Value.Count);

        foreach (var groupPermission in sortedPermissions)
        {
            sb.AppendLine($">> {groupPermission.Key} > {string.Join("+", groupPermission.Value)}");
            if (permissionsData.ContainsKey(groupPermission.Key))
                foreach (var playerItem in permissionsData[groupPermission.Key])
                {
                    VRCUser user = VRChat.GetUser(playerItem.Key);
                    if (user.displayName != "[NOTLOADED]")
                        sb.AppendLine(user.displayName);
                }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

}
