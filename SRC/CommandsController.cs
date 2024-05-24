using System.Text.Json;
using VRCWMT.Filters;
using OpenVRChatAPI.Models;
using OpenVRChatAPI;
using static VRCWMT.Server;
using System.Diagnostics;
using VRCWMT.Models;
using Microsoft.AspNetCore.Mvc;
using MagmaMC.SharedLibrary;
using VRCWMT.Services;

namespace VRCWMT
{
    [Route("api/v2/Commands")]
    [ApiController]
    [SiteOwner]
    public class CommandsController : ControllerBase
    {
        public static ConsoleLogger _logger;

        [HttpGet("Console")]
        public IActionResult GetConsoleOutput()
        {
            var logs = _logger.GetLogs();
            return Ok(logs);
        }
        [HttpGet("Help")]
        public IActionResult Help()
        {
            var helpMessages = new List<string>
            {
                "> ADD DUMMYWORLD - Creates A New Dummy VRC World In The Database",
                "> DELETE DUMMYWORLDS - Deletes All The Dummy Worlds From The Database",
                "> LOGIN - Login To The VRChat API",
                "> LOGOUT - Logout Of The VRChat API",
                "> CHECK AUTH - Checks If The Current AuthToken Is Valid",
                "> CHECK ACCOUNT - Displays The Currently Logged In VRChat User",
                "> SET OWNER - Set The Site Owner",
                "> SET IP - Set The Sites Web IP",
                "> SET PORT - Set The Sites Web Port",
                "> SAVE DATABASE - Saves The Database To The Disk",
                "> CLEAR DATABASE - Clears The Database And Initializes A New One",
                "> CLEAR CONFIG - Clears The Server Config And Initializes A New One",
                "> CLEAR - Clears The Screen",
                "> HELP - Lists All Terminal Commands",
                "> RESTART - Restarts The Server's Components",
                "> EXIT - Saves The Database And Exits"
            };

            return Ok(helpMessages);
        }

        [HttpPost("Login")]
        public IActionResult Login()
        {
            Commands.Login();
            return Ok("Logged In");
        }

        [HttpPost("Stats")]
        public IActionResult Stats()
        {
            Commands.Stats();
            return Ok("Stats Displayed");
        }

        [HttpPost("Exit")]
        public IActionResult Exit()
        {
            Commands.Exit();
            return Ok("Server Exited");
        }

        [HttpPost("Restart")]
        public IActionResult Restart()
        {
            Commands.Restart();
            return Ok("Server Restarted");
        }

        [HttpPost("SaveDB")]
        public IActionResult SaveDB()
        {
            Commands.SaveDB();
            return Ok("Database Saved");
        }

        [HttpPost("ClearConfig")]
        public IActionResult ClearConfig()
        {
            Commands.ClearConfig();
            return Ok("Config Cleared");
        }

        [HttpPost("AddDummyWorld")]
        public IActionResult AddDummyWorld()
        {
            VRCW world = new VRCW() { worldName = "examplename", worldCreator = "examplemaster", worldDescription = "A great World Description" };
            world.Posts.Add(new Post() { description = "test description", headerName = "example header", originalPoster = "MagmaMC" });
            world.Posts.Add(new Post() { description = "test description 2", headerName = "example header 2", originalPoster = "NotMagmaMC", replies = new List<PostReply>() { new() { username = "MagmaMC", text = "You Are A Copy" } } });
            string id = Database.NewID;
            Server.Database.Worlds.TryAdd(id, world);
            Server.Debugger.Info($"Added New Dummy World, `{id}`");
            return Ok($"Added New Dummy World, `{id}`");
        }

        [HttpDelete("DeleteDummyWorlds")]
        public IActionResult DeleteDummyWorlds()
        {
            List<string> _Worlds = new List<string>();
            foreach (var pair in Server.Database.Worlds)
                if (pair.Value.worldName.Equals("examplename", StringComparison.OrdinalIgnoreCase))
                    _Worlds.Add(pair.Key);

            foreach (string WorldID in _Worlds)
            {
                Server.Debugger.Debug($"Deleted `{WorldID}`");
                Server.Database.Worlds.TryRemove(WorldID, out _);
            }

            Server.Debugger.Info($"Deleted All Dummy Worlds");
            return Ok("Deleted All Dummy Worlds");
        }

        [HttpPost("SetOwner")]
        public IActionResult SetOwner([FromQuery] string ownerName)
        {
            CFG.SetValue("SiteOwner", ownerName.Trim(), "VRCWMT");
            return Ok($"Set Site Owner to {ownerName}");
        }

        [HttpPost("SetIP")]
        public IActionResult SetIP([FromQuery] string ip)
        {
            CFG.SetValue("IP", ip.Trim(), "VRCWMT");
            return Ok($"Set IP to {ip}");
        }

        [HttpPost("SetPort")]
        public IActionResult SetPort([FromQuery] string port)
        {
            CFG.SetValue("PORT", port.Trim(), "VRCWMT");
            return Ok($"Set PORT to {port}");
        }

        [HttpPost("ClearDatabase")]
        public IActionResult ClearDatabase()
        {
            Directory.Delete("VRCWorldImages", true);
            Directory.CreateDirectory("VRCWorldImages");
            Server.Database = new Database();
            Database.SaveContents(Server.Database);
            return Ok("Database Cleared");
        }

        [HttpPost("CheckAuth")]
        [HttpGet("CheckAuth")]
        public IActionResult CheckAuth()
        {
            bool isLoggedIn = VRChat.CheckAuth();
            return Ok(isLoggedIn ? "Logged In" : "Not Logged In");
        }

        [HttpPost("CheckAccount")]
        [HttpGet("CheckAccount")]
        public IActionResult CheckAccount()
        {
            if (!VRChat.CheckAuth())
            {
                return Unauthorized("Not Logged In");
            }

            VRCUser user = JsonSerializer.Deserialize<VRCUser>(VRChat.VRCHTTPClient.GetAsync("auth/user").GetHTTPString());
            return Ok(user);
        }
    }
}
