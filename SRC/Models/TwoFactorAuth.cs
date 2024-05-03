using Newtonsoft.Json;

namespace VRCWMT.Models;

public class TwoFactorAuth
{
    public List<string> requirestwofactorauth { get; set; } = new List<string>();
    public bool verified { get; set; } = false;
}
