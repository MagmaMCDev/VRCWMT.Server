namespace VRCWMT.Models;

public class SiteUser
{
    public string username { get; set; } = "";
    public bool siteAdmin { get; set; } = false;
    public bool worldCreator { get; set; } = false;
    public bool siteOwner { get; set; } = false;

    public override string ToString() => username;
}
