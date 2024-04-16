using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using ServerBackend.Models;
namespace ServerBackend;

public class Github
{
    private static readonly ConcurrentDictionary<string, GithubUser> _userCache = new();
    public static void ClearCache() => _userCache.Clear();

    public static async Task<GithubUser> GetUserAsync(string token)
    {
        if (_userCache.TryGetValue(token, out var cachedUser))
            return cachedUser;

        GithubUser user = await FetchUserAsync(token);
        _userCache[token] = user;
        return user;
    }

    private static async Task<GithubUser> FetchUserAsync(string token)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        httpClient.DefaultRequestHeaders.Add("User-Agent", Server.User_Agent);

        var response = await httpClient.GetAsync("https://api.github.com/user");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var userData = JsonSerializer.Deserialize<GithubUser>(json);
        userData.Access_Token = token;
        return userData;
    }
}

public class GithubRepo
{
    public async Task<string[]> GetContributorsAsync(string token, string repoName)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        httpClient.DefaultRequestHeaders.Add("User-Agent", Server.User_Agent);

        var response = await httpClient.GetAsync($"https://api.github.com/repos/{repoName}/contributors");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var contributors = JsonSerializer.Deserialize<List<Contributor>>(json);
        return contributors.Select(c => c.Login).ToArray();
    }

    public async Task<string[]> AddContributorsAsync(string token, string repoName, params string[] newUsers)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        httpClient.DefaultRequestHeaders.Add("User-Agent", Server.User_Agent);

        var result = new List<string>();
        foreach (var user in newUsers)
        {
            var response = await httpClient.PutAsync($"https://api.github.com/repos/{repoName}/collaborators/{user}", null);
            if (response.IsSuccessStatusCode)
            {
                result.Add(user);
            }
        }
        return result.ToArray();
    }

    private class Contributor
    {
        public string Login
        {
            get; set;
        }
    }
}

public class GithubUser
{
    public string login
    {
        get; set;
    }
    public uint id
    {
        get; set;
    }
    public string name
    {
        get; set;
    }
    public string avatar_url
    {
        get; set;
    }
    public string Access_Token
    {
        get; set;
    }
}

public static class GithubAuthExtensions
{
    public static bool IsSiteOwner(this GithubUser user) => user.login.ToLower() == Server.Database.SiteOwner.ToLower();
    public static bool IsMapMaster(this GithubUser user, string WRD_ID)
    {
        if (user.IsSiteOwner())
            return true;
        string ID = WRD_ID.Trim().ToUpper();
        if (!Server.Database.Worlds.ContainsKey(ID))
            return false;
        return Server.Database.Worlds[ID].WorldCreator.ToLower() == user.login.ToLower();
    }
    public static bool IsMapAdmin(this GithubUser user, string WRD_ID)
    {
        if (user.IsSiteOwner())
            return true;
        string ID = WRD_ID.Trim().ToUpper();
        if (!Server.Database.Worlds.ContainsKey(ID))
            return false;

        return user.IsMapAdmin(Server.Database.Worlds[ID]);
    }
    public static bool IsMapAdmin(this GithubUser user, VRCW World)
    {
        if (user.IsSiteOwner())
            return true;

        if (World.WorldCreator.ToLower() == user.login.ToLower())
            return true;
        foreach (string Admin in World.SiteAdmins)
        {
            if (Admin.ToLower() == user.login.ToLower())
                return true;
        }
        return false;
    }
    public static bool IsMapMaster(this GithubUser user, VRCW WRD)
    {
        if (user.IsSiteOwner())
            return true;
        return WRD.WorldCreator.ToLower() == user.login.ToLower().Trim();
    }

}

public struct GithubAuth
{
    public static GithubUser? GetUser(string Access_Token)
    {
        try
        {
            GithubUser User = Github.GetUserAsync(Access_Token).GetAwaiter().GetResult();
            return User;
        }
        catch { return null; }
    }
}