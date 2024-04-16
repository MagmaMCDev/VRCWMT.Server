namespace ServerBackend.Models;

public class _2FACode
{
    public string code { get; set; }

    public _2FACode(string code) => this.code = code;
}