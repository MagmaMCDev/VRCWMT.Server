using System.Net;
using System.Text;
using System.Text.Json;
using Spectre.Console;
using VRCWMT.Models;
using System.Collections.Concurrent;
namespace VRCWMT;

public static class VRChat
{
    public static string Username = "";
    public static string Password = "";
    private static readonly ConcurrentDictionary<string, Task<VRChatUser>> _userCache = new();

    public static void ClearCache() => _userCache.Clear();

    public static Task<VRChatUser> GetUserAsync(string User_ID)
    {
        string userid = User_ID.ToLower().Trim();

        return _userCache.GetOrAdd(userid, async (_) =>
        {
            VRChatUser user = await FetchUserAsync(userid);
            user._cache = false;
            return user;
        });
    }

    public static VRChatUser GetUser(string User_ID) => GetUserAsync(User_ID).GetAwaiter().GetResult();

    private static async Task<VRChatUser> FetchUserAsync(string userid)
    {
        var response = await VRCHTTPClient.GetAsync($"users/{userid}");
        VRChatUser FakeUser = new VRChatUser();
        FakeUser.displayName = "[NOTLOADED]";
        FakeUser.id = "usr_af112f0f-e828-49e1-84ca-2acf3dea3c48";
        FakeUser.loaded = false;

        if (!response.IsSuccessStatusCode)
            return FakeUser;

        string json = response.GetHTTPString();
        return JsonSerializer.Deserialize<VRChatUser>(json) ?? FakeUser;
    }

    public static bool LoggedIn => CheckAuth();
    public static HttpClient VRCHTTPClient => GetVRCHTTPClient();

    private static HttpClient? Client = null;
    private static HttpClientHandler? ClientHandler = null;
    public static HttpClient GetVRCHTTPClient()
    {
        if (Client != null)
            return Client;
        ClientHandler = new();
        ClientHandler.CookieContainer = new CookieContainer();
        ClientHandler.CookieContainer.Add(new Uri("https://api.vrchat.cloud"), new Cookie("auth", Server.CFG.VRC_auth.Trim()));
        ClientHandler.CookieContainer.Add(new Uri("https://api.vrchat.cloud"), new Cookie("twoFactorAuth", Server.CFG.VRC_twoFactorAuth.Trim()));
        Client = new(ClientHandler);
        Client.BaseAddress = new Uri("https://api.vrchat.cloud/api/1/");
        Client.DefaultRequestHeaders.Add("User-Agent", Server.User_Agent);
        return Client;
    }
    public static void RenewHTTPClient()
    {
        if (Client != null)
            Client.Dispose();
        if (ClientHandler != null)
            ClientHandler.Dispose();
        ClientHandler = new();
        ClientHandler.CookieContainer = new CookieContainer();
        ClientHandler.CookieContainer.Add(new Uri("https://api.vrchat.cloud"), new Cookie("auth", Server.CFG.VRC_auth.Trim()));
        ClientHandler.CookieContainer.Add(new Uri("https://api.vrchat.cloud"), new Cookie("twoFactorAuth", Server.CFG.VRC_twoFactorAuth.Trim()));
        Client = new(ClientHandler);
        Client.BaseAddress = new Uri("https://api.vrchat.cloud/api/1/");
        Client.DefaultRequestHeaders.Add("User-Agent", Server.User_Agent);
    }


    public static bool CheckAuth()
    {
        if (string.IsNullOrWhiteSpace(Server.CFG.VRC_auth))
            return false;
        if (string.IsNullOrWhiteSpace(Server.CFG.VRC_twoFactorAuth))
            return false;

        HttpResponseMessage content = VRCHTTPClient.GetAsync("auth/user").GetAwaiter().GetResult();
        return content.IsSuccessStatusCode;
    }
    public static bool SemiAutoLogin(uint? Code = null)
    {

        CookieContainer Cookies = new CookieContainer();
        HttpClientHandler handler = new HttpClientHandler();
        handler.CookieContainer = Cookies;
        HttpClient HTTPClient = new HttpClient(handler);
        HTTPClient.BaseAddress = new Uri("https://api.vrchat.cloud/api/1/");
        HTTPClient.DefaultRequestHeaders.Add("User-Agent", Server.User_Agent);
        HTTPClient.DefaultRequestHeaders.Add("Authorization", new VRCAuthHeader(Username, Password));
        HttpResponseMessage content = HTTPClient.GetAsync("auth/user").GetAwaiter().GetResult();
        string Response = content.Content.ReadAsStringAsync().GetAwaiter().GetResult().ToLower();
        if (Response.Contains("missing credentials") || Response.Contains("invalid username/email"))
        {
            AnsiConsole.MarkupLine("[red]Invalid user credentials![/]");
            return false;
        }
        else if (Response.Contains("requirestwofactorauth"))
        {
            uint code;
            if (Code == null)
                code = AnsiConsole.Ask<uint>("[LightSkyBlue1]Enter VRChat Two Factor Auth Code[/]:");
            else
                code = (uint)Code;
            TwoFactorAuth twoFactorAuth = JsonSerializer.Deserialize<TwoFactorAuth>(Response)!;
            TwoFactorAuth Verified = VerifyTwoFA(ref HTTPClient, twoFactorAuth.requirestwofactorauth.FirstOrDefault()!, code);
            if (!Verified.verified)
            {
                AnsiConsole.MarkupLine("[red]Invalid Two Factor Auth Code![/]");
                HTTPClient.Dispose();
                handler.Dispose();
                return false;
            }
        }
        foreach (Cookie cookie in Cookies.GetCookies(new Uri("https://api.vrchat.cloud")))
        {
            switch (cookie.Name.ToLower())
            {
                case "auth":
                    Server.CFG.VRC_auth = cookie.Value;
                    Server.CFG.SetValue("VRCHAT_auth", cookie.Value, "VRCWMT");
                    break;
                case "twofactorauth":
                    Server.CFG.VRC_twoFactorAuth = cookie.Value;
                    Server.CFG.SetValue("VRCHAT_twoFactorAuth", cookie.Value, "VRCWMT");
                    break;
            }
            ClearCache();
            RenewHTTPClient();
        }

        HTTPClient.Dispose();
        handler.Dispose();
        return true;
    }
    public static bool InteractiveLogin()
    {
        if (LoggedIn)
        {
            AnsiConsole.MarkupLine("[red]Already Logged In![/]");
            return true;
        }
        CookieContainer Cookies = new CookieContainer();
        HttpClientHandler handler = new HttpClientHandler();
        handler.CookieContainer = Cookies;

        string name = AnsiConsole.Ask<string>("[LightSkyBlue1]Enter VRChat Account Name/Email[/]:");
        AnsiConsole.Markup("[LightSkyBlue1]Enter VRChat Account Password[/]: ");
        string pass = GetMaskedPassword();
        Console.WriteLine();
        HttpClient HTTPClient = new HttpClient(handler);
        HTTPClient.BaseAddress = new Uri("https://api.vrchat.cloud/api/1/");
        HTTPClient.DefaultRequestHeaders.Add("User-Agent", Server.User_Agent);
        HTTPClient.DefaultRequestHeaders.Add("Authorization", new VRCAuthHeader(name, pass).Value);
        HttpResponseMessage content = HTTPClient.GetAsync("auth/user").GetAwaiter().GetResult();
        string Response = content.Content.ReadAsStringAsync().GetAwaiter().GetResult().ToLower();
        if (Response.Contains("missing credentials") || Response.Contains("invalid username/email"))
        {
            Console.WriteLine(Response);
            AnsiConsole.MarkupLine("[red]Invalid user credentials![/]");
            return false;
        }
        else if (Response.Contains("requirestwofactorauth"))
        {
            uint code = AnsiConsole.Ask<uint>("[LightSkyBlue1]Enter VRChat Two Factor Auth Code[/]:");
            TwoFactorAuth twoFactorAuth = JsonSerializer.Deserialize<TwoFactorAuth>(Response)!;
            TwoFactorAuth Verified = VerifyTwoFA(ref HTTPClient, twoFactorAuth.requirestwofactorauth.FirstOrDefault()!, code);
            if (!Verified.verified)
            {
                AnsiConsole.MarkupLine("[red]Invalid Two Factor Auth Code![/]");
                HTTPClient.Dispose();
                handler.Dispose();
                return false;
            }
        }
        AnsiConsole.MarkupLine("[lime]Successfully Logged In[/]");
        foreach (Cookie cookie in Cookies.GetCookies(new Uri("https://api.vrchat.cloud")))
        {
            switch(cookie.Name.ToLower())
            {
                case "auth":
                    Server.CFG.VRC_auth = cookie.Value;
                    Server.CFG.SetValue("VRCHAT_auth", cookie.Value, "VRCWMT");
                    break;
                case "twofactorauth":
                    Server.CFG.VRC_twoFactorAuth = cookie.Value;
                    Server.CFG.SetValue("VRCHAT_twoFactorAuth", cookie.Value, "VRCWMT");
                    break;
            }
            VRChat.ClearCache();
            VRChat.RenewHTTPClient();
        }

        HTTPClient.Dispose();
        handler.Dispose();
        Username = name;
        Password = pass;
        Server.CFG.SetValue("VRCHAT_name", Convert.ToBase64String(Encoding.UTF8.GetBytes(Username)), "VRCWMT");
        Server.CFG.SetValue("VRCHAT_pass", Convert.ToBase64String(Encoding.UTF8.GetBytes(Password)), "VRCWMT");
        return true;
    }
    private static TwoFactorAuth VerifyTwoFA(ref HttpClient HTTPClient, string TwoFAType, uint Code)
    {
        StringContent Parsed_Code = new StringContent(JsonSerializer.Serialize(new _2FACode(Code.ToString())), Encoding.UTF8, "application/json");
        string response = GetHTTPString(HTTPClient.PostAsync($"auth/twofactorauth/{TwoFAType.ToLower()}/verify", Parsed_Code));
        return JsonSerializer.Deserialize<TwoFactorAuth>(response)!;
    }


    public static string? GetHTTPString(this Task<HttpRequestMessage> message) => message.GetAwaiter().GetResult().Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    public static string GetHTTPString(this Task<HttpResponseMessage> message) => message.GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
    public static HttpContent? GetHTTPContent(this Task<HttpRequestMessage> message) => message.GetAwaiter().GetResult().Content;
    public static HttpContent GetHTTPContent(this Task<HttpResponseMessage> message) => message.GetAwaiter().GetResult().Content;
    public static bool IsSuccessful(this Task<HttpResponseMessage> message) => message.GetAwaiter().GetResult().IsSuccessStatusCode;
    public static string? GetHTTPString(this HttpRequestMessage message) => message.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    public static string GetHTTPString(this HttpResponseMessage message) => message.Content.ReadAsStringAsync().GetAwaiter().GetResult();

    static string GetMaskedPassword()
    {
        string password = "";
        ConsoleKeyInfo key;
        do
        {
            key = Console.ReadKey(true);

            if (char.IsControl(key.KeyChar) && key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Backspace)
                continue;

            if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length <= 0)
                    continue;
                password = password.Substring(0, password.Length - 1);
                Console.Write("\b \b");
            }
            else if (key.Key != ConsoleKey.Enter)
            {
                password += key.KeyChar;
                Console.Write("*");
            }
        } while (key.Key != ConsoleKey.Enter);

        return password;
    }
}


public class VRCAuthHeader
{
    public string Name
    {
        private get; set;
    }
    public string Password
    {
        private get; set;
    }

    public VRCAuthHeader(string name, string password)
    {
        Name = name;
        Password = password;
    }
    public string Value => $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Uri.EscapeDataString(Name)}:{Uri.EscapeDataString(Password)}"))}";
    public override string ToString() => Value;

    public static implicit operator string(VRCAuthHeader header) => header.Value;
}