﻿using MagmaMc.MagmaSimpleConfig;

namespace VRCWMT;

public class ServerConfig: SimpleConfig
{
    public const string Filename = "ServerConfig.MSC";

    public string IP = "";
    public string PORT = "";
    public string SiteOwner = "";

    public string VRC_auth = "";
    public string VRC_twoFactorAuth = "";

    public const string VERSION = "1.3.0";

    public ServerConfig(): base(Filename)
    {
        if (!File.Exists(Filename))
            File.WriteAllBytes(Filename, Array.Empty<byte>());
    }
}
