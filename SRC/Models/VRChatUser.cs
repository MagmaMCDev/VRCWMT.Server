namespace VRCWMT.Models;

public class VRChatUser
{
    public string id { get; set; } = "";
    public string displayName { get; set; } = "";
    public string statusDescription { get; set; } = "";
    public string userIcon { get; set; } = "";
    public string currentAvatarThumbnailImageUrl { get; set; } = "";
    public bool _cache { get; set; } = true;
    public bool loaded = true;
}