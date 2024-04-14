using System.Diagnostics;
using System.Runtime.Versioning;
using MagmaMc.SharedLibrary;
using ServerBackend.SRC;
using Spectre.Console;

namespace ServerBackend;
public class Server
{
#pragma warning disable CS8618
    private static IHost program;
    private static Thread MainThread;
#pragma warning restore CS8618
    private static readonly Logger Debugger = new(LoggingLevel.Debug);
    public static Database Database = new();
    public static ServerConfig CFG = new();
        
    public static void Main(string[] args)
    {
        Commands.SetupConfig();
        program = CreateHostBuilder().Build();
        Run();
    }
    public static IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder(new string[0])
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.UseUrls($"http://{CFG.IP}:{CFG.PORT}");
            });
    public static void UpdateDataBase()
    {
        while (MainThread.IsAlive)
        {
            Thread.Sleep(30 * 1000);
            Database.SaveContents(Database);
            if (!System.Diagnostics.Debugger.IsAttached)
                Thread.Sleep(30 * 1000);
        }
    }
    public static void Run()
    {
        if (File.Exists(Database.FileName))
            Database = Database.LoadContents() ?? Database;
        else
            Database.SaveContents(Database);
        MainThread = new Thread(program.Run);
        MainThread.Priority = ThreadPriority.AboveNormal;
        MainThread.Name = "Web Application Thread";
        MainThread.Start();
        new Thread(UpdateDataBase).Start();
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
                    VRCW world = new VRCW() { WorldName = "examplename", WorldCreator = "examplemaster", WorldDescription = "A great World Description" };
                    world.Posts.Add(new Post() { Description = "test description", HeaderName = "example header", OriginalPoster = "MagmaMC" });
                    world.Posts.Add(new Post() { Description = "test description 2", HeaderName = "example header 2", OriginalPoster = "NotMagmaMC", Replies = new List<PostReply>() { new() { Username = "MagmaMC", Text = "You Are A Copy" } } });
                    string id = Database.NewID;
                    Database.Worlds.TryAdd(id, world);
                    Debugger.Info($"Added New Dummy World, `{id}`");
                    break;
                case "DELETEEXAMPLEWORLDS" or "DELETEDUMMYWORLDS":
                    List<string> _Worlds = new List<string>();
                    foreach (var pair in Database.Worlds)
                        if (pair.Value.WorldName.Equals("examplename", StringComparison.OrdinalIgnoreCase))
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
                case "RESTART":
                    Commands.Exit();
                    Thread.Sleep(250);
                    Database.Dispose();
                    AnsiConsole.MarkupLine("[red]Closed Database[/]");
                    AnsiConsole.MarkupLine("[lime]Initializing New Database...[/]");
                    Commands.SetupConfig();
                    Database = new Database();
                    Database = Database.LoadContents() ?? Database;
                    Thread.Sleep(250);
                    AnsiConsole.MarkupLine("[lime]Initialized New Database[/]");
                    AnsiConsole.MarkupLine("[lime]Starting Server...[/]");
                    Thread.Sleep(250);
                    program = CreateHostBuilder().Build();
                    MainThread = new Thread(program.Run);
                    MainThread.Priority = ThreadPriority.AboveNormal;
                    MainThread.Name = "Web Application Thread";
                    MainThread.Start();
                    AnsiConsole.MarkupLine("[lime]Started Server[/]");
                    Thread.Sleep(500);
                    Commands.Stats();
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
            Console.Clear();
            AnsiConsole.MarkupLine("[red]Closing Server...[/]");
            program.StopAsync().Wait();
            program.Dispose();
            Thread.Sleep(250);
            AnsiConsole.MarkupLine("[red]Closed Server[/]");
            SaveDB();
            AnsiConsole.MarkupLine("[red]Closing Database...[/]");
            Thread.Sleep(250);
            Database.Dispose();
            AnsiConsole.MarkupLine("[red]Closed Database[/]");
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
            CFG.SetValue("IP", CFG.IP, "VRCWMT");
            CFG.SetValue("PORT", CFG.PORT, "VRCWMT");
            CFG.SetValue("SiteOwner", CFG.SiteOwner, "VRCWMT");
        }

    }
}
