namespace VRCWMT.Models;

public class SiteUser
{
    public string Username { get; set; } = "";
    public bool SiteAdmin { get; set; } = false;
    public bool WorldCreater { get; set; } = false;
    public bool SiteOwner { get; set; } = false;

    public override string ToString() => Username;
}
