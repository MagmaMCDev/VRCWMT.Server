using MagmaMc.MagmaSimpleConfig;

namespace ServerBackend.SRC;

public class ServerConfig: SimpleConfig
{
    public const string Filename = "Config.MSC";

    public string IP = "";
    public string PORT = "";
    public string SiteOwner = "";

    public const string VERSION = "0.2.0";

    public ServerConfig(): base(Filename)
    {
        if (!File.Exists(Filename))
        {
            File.WriteAllBytes(Filename, Array.Empty<byte>());
        }
    }
}
