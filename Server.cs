using System.Runtime.Versioning;
using MagmaMc.SharedLibrary;
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
    public static void Main(string[] args)
    {
        program = CreateHostBuilder(args).Build();
        Run();
    }
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.UseUrls("http://0.0.0.0:5151");
            });
    public static void UpdateDataBase()
    {
        while (MainThread.IsAlive)
        {
            Thread.Sleep(30 * 1000);
            Database.SaveContents(Database);
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
        Console.Clear();
        AnsiConsole.Write(new FigletText("VRCMT Server").Color(Color.OrangeRed1).Centered());
        AnsiConsole.MarkupLine("==================================================");
        AnsiConsole.MarkupLine($"IP: 127.0.0.1");
        AnsiConsole.MarkupLine($"Port: 5151");
        AnsiConsole.MarkupLine("==================================================");

        while (MainThread.IsAlive)
        {
            if (Console.KeyAvailable)
            {
                string Key = Console.ReadKey(true).KeyChar.ToString().ToUpper();
                switch(Key)
                {
                    case "A":
                        VRCW world = new VRCW() { WorldName = "examplename", WorldCreator = "examplemaster", WorldDescription = "A great World Description" };
                        world.Posts.Add(new Post() { Description = "test description", HeaderName = "example header", OriginalPoster = "MagmaMC" });
                        world.Posts.Add(new Post() { Description = "test description 2", HeaderName = "example header 2", OriginalPoster = "NotMagmaMC", Replies = new List<PostReply>() { new () { Username = "MagmaMC", Text = "You Are A Copy" } } });
                        Database.Worlds.Add(Database.NewID, world);
                        Debugger.Info("Added New VRCW");
                        break;
                    case "P":
                        Database.SiteOwner = AnsiConsole.Ask<string>("[OrangeRed1]Enter Site Owner Name[/]:");
                        Database.SaveContents(Database);
                        break;
                }
            }
            Thread.Sleep(10);
        }

    }
}
