namespace ServerBackend;

public class Account
{
    public string Username { get; set; } = "";
    public string GithubUsername { get; set; } = "";

    public override string ToString() => Username;
}
