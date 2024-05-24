namespace VRCWMT.Models;

#pragma warning disable CS8618
public class GithubRelease
{
    public string name
    {
        get; set;
    }
    public string tag_name
    {
        get; set;
    }
    public string body
    {
        get; set;
    }
    public Asset[] assets
    {
        get; set;
    }
}

public class Asset
{
    public string browser_download_url
    {
        get; set;
    }
}
#pragma warning restore CS8618