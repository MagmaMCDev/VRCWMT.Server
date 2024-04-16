using Newtonsoft.Json;

namespace ServerBackend.Models;

public class TwoFactorAuth
{
    public List<string> requirestwofactorauth { get; set; } = new List<string>();
    public bool verified { get; set; } = false;
}
