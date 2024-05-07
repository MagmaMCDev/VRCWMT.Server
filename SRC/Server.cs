using System.Diagnostics;
using System.Net;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using MagmaMC.SharedLibrary;
using VRCWMT.Models;
using Spectre.Console;
using ServerBackend;
using OpenVRChatAPI;
using OpenVRChatAPI.Models;

namespace VRCWMT;
public class Server
{
#pragma warning disable CS8618
    private static IHost WebServer;
    private static Thread MainThread;
    private static Thread DBThread;
    private static Thread AuthThread;
#pragma warning restore CS8618
    private static readonly Logger Debugger = new(LoggingLevel.Debug);
    public static Database Database = new();
    public static ServerConfig CFG = new();
    public static string User_Agent = "VRChat-World-Moderation-Tool.Server - " + Environment.OSVersion.Platform + $" - {Environment.OSVersion.VersionString.Replace(" ", "-")}";

    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Commands.SetupConfig();

        if (!VRChat.LoggedIn)
        {
            VRChat.SemiAutoLogin();
            CFG.VRC_auth = VRChat.Auth.auth;
            CFG.VRC_twoFactorAuth = VRChat.Auth.twoFactorAuth;
            CFG.SetValue("VRCHAT_auth", CFG.VRC_auth, "VRCWMT");
            CFG.SetValue("VRCHAT_twoFactorAuth", CFG.VRC_twoFactorAuth, "VRCWMT");
            CFG.SetValue("VRCHAT_name", Convert.ToBase64String(Encoding.UTF8.GetBytes(VRChat.Username)), "VRCWMT");
            CFG.SetValue("VRCHAT_pass", Convert.ToBase64String(Encoding.UTF8.GetBytes(VRChat.Password)), "VRCWMT");
        }
        if (!VRChat.LoggedIn)
            Commands.Login();
        WebServer = CreateHostBuilder().Build();
        Run();
    }
    public static IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder(new string[0])
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.UseUrls($"http://{CFG.IP}:{CFG.PORT}");
            });
    private static void UpdateDataBase()
    {
        while (MainThread.IsAlive)
        {
            for (int i = 0; i < 15 * (System.Diagnostics.Debugger.IsAttached ? 1 : 2); i++)
            {
                Thread.Sleep(2000);
                if (MainThread == null)
                    return;
                if (!MainThread.IsAlive)
                    return;
            }
            Database.SaveContents(Database);
        }
    }
    private static void CheckAuth()
    {
        while (MainThread.IsAlive)
        {
            for (int i = 0; i < 15 * (System.Diagnostics.Debugger.IsAttached ? 1 : 2); i++)
            {
                Thread.Sleep(2000);
                if (MainThread == null)
                    return;
                if (!MainThread.IsAlive)
                    return;
            }
            try
            {
                if (!VRChat.LoggedIn)
                {
                    AnsiConsole.MarkupLine("[red]VRChat Auth Invalid Attempting Auto Login[/]");
                    do
                    {
                        VRChat.SemiAutoLogin();
                    } while (!VRChat.LoggedIn);
                }
            } catch(Exception e) 
            {
                AnsiConsole.MarkupLine($"[red]VRChat Auth Failed To Check Login: {e.Message}[/]");
                Thread.Sleep(1000);
            }
        }
    }



    public static void Run()
    {
        if (File.Exists(Database.FileName))
            Database = Database.LoadContents() ?? Database;
        else
            Database.SaveContents(Database);
        MainThread = new Thread(WebServer.Run);
        MainThread.Priority = ThreadPriority.AboveNormal;
        MainThread.Name = "Web Application Thread";
        MainThread.Start();
        DBThread = new Thread(UpdateDataBase);
        DBThread.Start();
        AuthThread = new Thread(CheckAuth);
        AuthThread.Start();
        Thread.Sleep(500);
        Commands.Stats();

        while (MainThread.IsAlive)
        {
            AnsiConsole.Markup("[cyan]> [/]");
            string? Key = Console.ReadLine();
            if (Key == null)
                continue;
            Key = Key.Trim().ToUpper().Replace(" ", "");
            switch(Key)
            {
                case "ADDEXAMPLEWORLD" or "ADDDUMMYWORLD":
                    VRCW world = new VRCW() { worldName = "examplename", worldCreator = "examplemaster", worldDescription = "A great World Description" };
                    world.Posts.Add(new Post() { description = "test description", headerName = "example header", originalPoster = "MagmaMC" });
                    world.Posts.Add(new Post() { description = "test description 2", headerName = "example header 2", originalPoster = "NotMagmaMC", replies = new List<PostReply>() { new() { username = "MagmaMC", text = "You Are A Copy" } } });
                    string id = Database.NewID;
                    Database.Worlds.TryAdd(id, world);
                    Debugger.Info($"Added New Dummy World, `{id}`");
                    break;
                case "DELETEEXAMPLEWORLDS" or "DELETEDUMMYWORLDS":
                    List<string> _Worlds = new List<string>();
                    foreach (var pair in Database.Worlds)
                        if (pair.Value.worldName.Equals("examplename", StringComparison.OrdinalIgnoreCase))
                            _Worlds.Add(pair.Key);

                    foreach (string WorldID in _Worlds)
                    {
                        Debugger.Debug($"Deleted `{WorldID}`");
                        Database.Worlds.TryRemove(WorldID, out _);
                    }

                    Debugger.Info($"Deleted All Dummy Worlds");
                    break;
                case "SETOWNER" or "SETSITEOWNER":
                    AnsiConsole.Markup("[orangered1]> [/]");
                    CFG.SetValue("SiteOwner", AnsiConsole.Ask<string>("[OrangeRed1]Enter Site Owner Name[/]:").Trim(), "VRCWMT");
                    break;
                case "SETIP":
                    AnsiConsole.Markup("[orangered1]> [/]");
                    CFG.SetValue("IP", AnsiConsole.Ask<string>("[OrangeRed1]Enter New IP [/]([red]Requires Restart[/]):").Trim(), "VRCWMT");
                    break;
                case "SETPORT":
                    AnsiConsole.Markup("[orangered1]> [/]");
                    CFG.SetValue("PORT", AnsiConsole.Ask<string>("[OrangeRed1]Enter New PORT [/]([red]Requires Restart[/]):").Trim(), "VRCWMT");
                    break;
                case "CLEARDATABASE" or "CLEARDB":
                    AnsiConsole.Markup("[orangered1]> [/]");
                    bool Clear = AnsiConsole.Ask("[red]Are You Sure You Want To Clear The Database[/]", false);
                    if (Clear)
                    {
                        AnsiConsole.MarkupLine("[red]Clearing Database...[/]");
                        Thread.Sleep(250);
                        Directory.Delete("VRCWorldImages", true);
                        Directory.CreateDirectory("VRCWorldImages");
                        Database = new Database();
                        Database.SaveContents(Database);
                        AnsiConsole.MarkupLine("[red]Database Cleared[/]");
                    }
                    break;
                case "CLEARCONFIG" or "CLEARCFG":
                    Commands.ClearConfig();
                    break;
                case "CLEAR" or "CLS":
                    Commands.Stats();
                    break;
                case "EXIT" or "QUIT":
                    Commands.Exit();
                    Environment.Exit(0);
                    return;
                case "SAVEDATABASE" or "SAVEDB":
                    Commands.SaveDB();
                    break;
                case "LOGIN":
                    Commands.Login();
                    break;
                case "LOGOUT":
                    AnsiConsole.Markup("[orangered1]> [/]");
                    if (!AnsiConsole.Ask("[red]Are You Sure You Want To Logout (Restart Required!)[/]", false))
                        break;
                    CFG.VRC_auth = "";
                    CFG.VRC_twoFactorAuth = "";
                    CFG.SetValue("VRCHAT_auth", "", "VRCWMT");
                    CFG.SetValue("VRCHAT_twoFactorAuth", "", "VRCWMT");
                    CFG.SetValue("VRCHAT_name", "", "VRCWMT");
                    CFG.SetValue("VRCHAT_pass", "", "VRCWMT");
                    Thread.Sleep(1000);
                    Commands.Restart();
                    break;
                case "CHECKAUTH":
                    if (!VRChat.CheckAuth())
                        AnsiConsole.MarkupLine("[red]Not Logged In![/]");
                    else
                        AnsiConsole.MarkupLine("[lime]Logged In![/]");
                    break;
                case "CHECKACCOUNT" or "ACCOUNT":
                    if (!VRChat.CheckAuth())
                    {
                        AnsiConsole.MarkupLine("[red]Not Logged In![/]");
                        break;
                    }

                    VRCUser User = JsonSerializer.Deserialize<VRCUser>(VRChat.VRCHTTPClient.GetAsync("auth/user").GetHTTPString())!;
                    AnsiConsole.MarkupLine($"Username: {User.displayName}");
                    break;
                case "RESTART":
                    Commands.Restart();
                    break;
                case "HELP":
                    Commands.Help();
                    break;
                default:
                    AnsiConsole.MarkupLine($"[red]Command [/]`{Key}`[red] Not Found![/]");
                    Commands.Help();
                    break;
            }
            Thread.Sleep(10);
        }

    }
    public static class Commands
    {
        public static void Help()
        {
            AnsiConsole.Markup($@"[orangered1]> [/][lime]ADD DUMMYWORLD[/]");
            AnsiConsole.MarkupLine($@"[orangered1] - [/]" + $@"Creates A New Dummy VRC World In The Database");
            AnsiConsole.Markup($@"[orangered1]> [/][lime]DELETE DUMMYWORLDS[/]");
            AnsiConsole.MarkupLine($@"[orangered1] - [/]" + $@"Deletes All The Dummy Worlds From The Database");
            AnsiConsole.Markup($@"[orangered1]> [/][lime]LOGIN[/]");
            AnsiConsole.MarkupLine($@"[orangered1] - [/]" + $@"Login To The VRChat API");
            AnsiConsole.Markup($@"[orangered1]> [/][lime]LOGOUT[/]");
            AnsiConsole.MarkupLine($@"[orangered1] - [/]" + $@"Logout Of The VRChat API");
            AnsiConsole.Markup($@"[orangered1]> [/][lime]CHECK AUTH[/]");
            AnsiConsole.MarkupLine($@"[orangered1] - [/]" + $@"Checks If The Current AuthToken Is Valid");
            AnsiConsole.Markup($@"[orangered1]> [/][lime]CHECK ACCOUNT[/]");
            AnsiConsole.MarkupLine($@"[orangered1] - [/]" + $@"Displays The Currently Logged In VRChat User");
            AnsiConsole.Markup($@"[orangered1]> [/][lime]SET OWNER[/]");
            AnsiConsole.MarkupLine($@"[orangered1] - [/]" + $@"Set The Site Owner");
            AnsiConsole.Markup($@"[orangered1]> [/][lime]SET IP[/]");
            AnsiConsole.MarkupLine($@"[orangered1] - [/]" + $@"Set The Sites Web IP");
            AnsiConsole.Markup($@"[orangered1]> [/][lime]SET PORT[/]");
            AnsiConsole.MarkupLine($@"[orangered1] - [/]" + $@"Set The Sites Web Port");
            AnsiConsole.Markup($@"[orangered1]> [/][lime]SAVE DATABASE[/]");
            AnsiConsole.MarkupLine($@"[orangered1] - [/]" + $@"Saves The Database To The Disk");
            AnsiConsole.Markup($@"[orangered1]> [/][lime]CLEAR DATABASE[/]");
            AnsiConsole.MarkupLine($@"[orangered1] - [/]" + $@"Clears The Database And Initializes A New One");
            AnsiConsole.Markup($@"[orangered1]> [/][lime]CLEAR CONFIG[/]");
            AnsiConsole.MarkupLine($@"[orangered1] - [/]" + $@"Clears The Server Config And Initializes A New One");
            AnsiConsole.Markup($@"[orangered1]> [/][lime]CLEAR[/]");
            AnsiConsole.MarkupLine($@"[orangered1] - [/]" + $@"Clears The Screen");
            AnsiConsole.Markup($@"[orangered1]> [/][lime]HELP[/]");
            AnsiConsole.MarkupLine($@"[orangered1] - [/]" + $@"Lists All Terminal Commands");

            AnsiConsole.Markup($@"[orangered1]> [/][lime]RESTART[/]");
            AnsiConsole.MarkupLine($@"[orangered1] - [/]" + $@"Restarts The Server's Components");
            AnsiConsole.Markup($@"[orangered1]> [/][lime]EXIT[/]");
            AnsiConsole.MarkupLine($@"[orangered1] - [/]" + $@"Saves The Database And Exits");
        }

        public static void Login()
        {
            do
            {
                VRChat.InteractiveLogin();
                CFG.VRC_auth = VRChat.Auth.auth;
                CFG.VRC_twoFactorAuth = VRChat.Auth.twoFactorAuth;
                CFG.SetValue("VRCHAT_auth", CFG.VRC_auth, "VRCWMT");
                CFG.SetValue("VRCHAT_twoFactorAuth", CFG.VRC_twoFactorAuth, "VRCWMT");
                CFG.SetValue("VRCHAT_name", Convert.ToBase64String(Encoding.UTF8.GetBytes(VRChat.Username)), "VRCWMT");
                CFG.SetValue("VRCHAT_pass", Convert.ToBase64String(Encoding.UTF8.GetBytes(VRChat.Password)), "VRCWMT");
            }
            while (!VRChat.LoggedIn);
        }

        public static void Stats()
        {
            Console.Clear();
            AnsiConsole.Write(new FigletText("VRCWMT").Color(Color.OrangeRed1).Centered());
            AnsiConsole.MarkupLine("[red]==================================================[/]");
            AnsiConsole.MarkupLine($"[orangered1]IP[/]: [lime]{CFG.IP}[/]");
            AnsiConsole.MarkupLine($"[orangered1]Port[/]: [lime]{CFG.PORT}[/]");
            AnsiConsole.MarkupLine($"[orangered1]Version[/]: [lime]{ServerConfig.VERSION}[/]");
            AnsiConsole.MarkupLine($"[orangered1]SiteOwner[/]: [lime]{CFG.SiteOwner}[/]");
            AnsiConsole.MarkupLine($"[orangered1]Developer[/]: [lime]MagmaMc.Dev[/]");
            AnsiConsole.MarkupLine("[red]==================================================[/]");
        }
        public static void Exit()
        {
            Thread.Sleep(50);
            Console.Clear();
            Thread.Sleep(150);
            AnsiConsole.MarkupLine("[red]Closing Server...[/]");
            WebServer.StopAsync().Wait();
            WebServer.Dispose();
            Thread.Sleep(150);
            AnsiConsole.MarkupLine("[red]Closed Server[/]");
            Thread.Sleep(50);
            SaveDB();
            Thread.Sleep(50);
            AnsiConsole.MarkupLine("[red]Closing Threads...[/]");
            DBThread.Join();
            AuthThread.Join();
            Thread.Sleep(150);
            AnsiConsole.MarkupLine("[red]Closed Threads[/]");
            Thread.Sleep(50);
            AnsiConsole.MarkupLine("[red]Closing Database...[/]");
            Thread.Sleep(250);
            Database.Dispose();
            AnsiConsole.MarkupLine("[red]Closed Database[/]");
        }
        public static void Restart()
        {
            Commands.Exit();
            Thread.Sleep(250);
            AnsiConsole.MarkupLine("[lime]Clearing Cache...[/]");
            Github.ClearCache();
            VRChat.ClearCache();
            Thread.Sleep(250);
            AnsiConsole.MarkupLine("[lime]Cleared Cache[/]");
            Thread.Sleep(50);
            AnsiConsole.MarkupLine("[lime]Renewing VRChat HTTPClient...[/]");
            Thread.Sleep(50);
            VRChat.RenewHTTPClient();
            Thread.Sleep(250);
            AnsiConsole.MarkupLine("[lime]Renewed VRChat HTTPClient[/]");
            Thread.Sleep(50);
            AnsiConsole.MarkupLine("[lime]Initializing New Database...[/]");
            Commands.SetupConfig();
            Database = new Database();
            Database = Database.LoadContents() ?? Database;
            Thread.Sleep(250);
            AnsiConsole.MarkupLine("[lime]Initialized New Database[/]");
            if (!VRChat.LoggedIn)
                VRChat.InteractiveLogin();
            Thread.Sleep(250);
            AnsiConsole.MarkupLine("[lime]Starting Server...[/]");
            Thread.Sleep(150);
            WebServer = CreateHostBuilder().Build();
            MainThread = new Thread(WebServer.Run);
            MainThread.Priority = ThreadPriority.AboveNormal;
            MainThread.Name = "Web Application Thread";
            MainThread.Start();
            AnsiConsole.MarkupLine("[lime]Initializing Threads...[/]");
            DBThread = new Thread(UpdateDataBase);
            DBThread.Start();
            AuthThread = new Thread(CheckAuth);
            AuthThread.Start();
            Thread.Sleep(150);
            AnsiConsole.MarkupLine("[lime]Started Server[/]");
            Thread.Sleep(500);
            Commands.Stats();
        }
        public static void SaveDB()
        {
            AnsiConsole.MarkupLine("[lime]Saving Database...[/]");
            Database.SaveContents(Database);
            Thread.Sleep(250);
            AnsiConsole.MarkupLine("[lime]Saved Database[/]");
        }
        public static void ClearConfig()
        {
            AnsiConsole.MarkupLine("[red]Clearing Server Config...[/]");
            File.Delete(CFG.FileName);
            AnsiConsole.MarkupLine("[red]Cleared Server Config[/]");
            Thread.Sleep(250);
            AnsiConsole.MarkupLine("[lime]Initializing New Server Config...[/]");
            CFG = new ServerConfig();
            SetupConfig();
            Thread.Sleep(250);
            AnsiConsole.MarkupLine("[red]Restart Is Required To Initialize New Server Config[/]");
        }
        public static void SetupConfig()
        {
            CFG.IP = CFG.GetValue("IP", "VRCWMT", "0.0.0.0");
            CFG.PORT = CFG.GetValue("PORT", "VRCWMT", "5151");
            CFG.SiteOwner = CFG.GetValue("SiteOwner", "VRCWMT", "MagmaMC");
            CFG.VRC_auth = CFG.GetValue("VRCHAT_auth", "VRCWMT", "");
            CFG.VRC_twoFactorAuth = CFG.GetValue("VRCHAT_twoFactorAuth", "VRCWMT", "");
            VRChat.Username = Encoding.UTF8.GetString(Convert.FromBase64String(CFG.GetValue("VRCHAT_name", "VRCWMT", "")));
            VRChat.Password = Encoding.UTF8.GetString(Convert.FromBase64String(CFG.GetValue("VRCHAT_pass", "VRCWMT", "")));
            VRChat.Auth.auth = CFG.VRC_auth;
            VRChat.Auth.twoFactorAuth = CFG.VRC_twoFactorAuth;
            CFG.SetValue("IP", CFG.IP, "VRCWMT");
            CFG.SetValue("PORT", CFG.PORT, "VRCWMT");
            CFG.SetValue("SiteOwner", CFG.SiteOwner, "VRCWMT");
            CFG.SetValue("VRCHAT_auth", CFG.VRC_auth, "VRCWMT");
            CFG.SetValue("VRCHAT_twoFactorAuth", CFG.VRC_twoFactorAuth, "VRCWMT");
            CFG.SetValue("VRCHAT_name", Convert.ToBase64String(Encoding.UTF8.GetBytes(VRChat.Username)), "VRCWMT");
            CFG.SetValue("VRCHAT_pass", Convert.ToBase64String(Encoding.UTF8.GetBytes(VRChat.Password)), "VRCWMT");
        }
    }
}
