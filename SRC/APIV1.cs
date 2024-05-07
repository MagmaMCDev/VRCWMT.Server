using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using VRCWMT.Models;
using VRCWMT.ENV;
using System.Collections.Generic;
using System.Collections.Concurrent;
using ServerBackend;
using OpenVRChatAPI;
using OpenVRChatAPI.Models;
using System.Numerics;

namespace VRCWMT;
[Route("api/v1/")]
[ApiController]
public class APIV1 : ControllerBase
{
    public const string APIRoute = "https://vrc.magmamc.dev/API/V1";
    public const string RedirectBase = @"<script>window.location.href='https://vrc.magmamc.dev/';</script>";
    public static string RedirectScript(string URL) => $@"<script>window.location.href='{URL}';</script>";
    public static string GithubOAuthURL => $"https://github.com/login/oauth/authorize?client_id={env.ClientID}&scope=repo,read:user&redirect_uri={APIRoute}/Github/OAuth/";

    #region Github

    [HttpGet("Logout")]
    [HttpPost("Logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("AuthToken");

        return Content(RedirectBase, "text/html");
    }

    [HttpGet("Login")]
    [HttpGet("Github/Login")]
    public IActionResult GithubLogin() => RedirectPermanent(GithubOAuthURL);

    [HttpGet("Client/Login")]
    public IActionResult ClientLogin()
    {
        Response.Cookies.Append("Redirect", "http://localhost:3928/");
        return Content(RedirectScript(GithubOAuthURL), "text/html");
    }

    [HttpGet("Github/OAuth")]
    [HttpPost("Github/OAuth")]
    public async Task<IActionResult> GithubOAuthLogin()
    {
        string? authCode = Request.Query["code"];

        if (string.IsNullOrEmpty(authCode))
            return Redirect("{APIRoute}/Github/Login");

        using HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        HttpResponseMessage message = await httpClient.PostAsync($"https://github.com/login/oauth/access_token?client_id={env.ClientID}&client_secret={env.ClientSecret}&code={authCode}", null);
        GithubOAuth? OAuth = await message.Content.ReadFromJsonAsync<GithubOAuth>();
        if (OAuth == null)
            return Redirect(GithubOAuthURL);

        Response.Cookies.Append("AuthToken", OAuth.access_token);
        if (Request.Cookies.Keys.Contains("Redirect"))
        {
            Request.Cookies.TryGetValue("Redirect", out string? redirect);
            Response.Cookies.Delete("Redirect");
            return Content(RedirectScript(redirect! + "?auth="+OAuth.access_token), "text/html");
        }
        else
            return Content(RedirectBase, "text/html");

    }

    [HttpGet("Github/Users/{AuthToken}")]
    public async Task<IActionResult> GetGithubUser(string AuthToken)
    {
        return Ok(await Github.GetUserAsync(AuthToken));
    }
    [HttpGet("Github/Users/")]
    public async Task<IActionResult> GetGithubUser()
    {
        return Ok(await Github.GetUserAsync(Request.Cookies["AuthToken"]!.ToString()));
    }
    #endregion

    #region Worlds
    [HttpPost("Worlds")]
    public IActionResult AddWorld(IFormFile file)
    {
        try
        {
            string? Token = Request.Cookies["AuthToken"] ?? Request.Headers.Authorization;

            if (string.IsNullOrWhiteSpace(Token))
                return Unauthorized("Unauthorized");
            GithubUser? User = GithubAuth.GetUser(Token);
            Console.WriteLine(Token);
            if (User == null)
                return Unauthorized("Unauthorized");

            string WorldName = Request.Form["WorldName"].ToString();
            string WorldDesc = Request.Form["WorldDescription"].ToString();
            string Repo = Request.Form["GithubRepo"].ToString();
            if (Repo.Split('/').Length != 2)
                return BadRequest("Invalid Github Repo.");

            if (!IsAscii(WorldName))
                return BadRequest("WorldName must contain only ASCII characters.");

            if (!IsAscii(WorldDesc))
                return BadRequest("WorldDescription must contain only ASCII characters.");


            string ID = Database.NewID;
            if (Server.Database.Worlds.ContainsKey(ID))
                return StatusCode(501, "Please Try Again");


            if (file != null)
            {
                if (file.Length > 5 * 1024 * 1024)
                    return BadRequest("File size exceeds the limit (5MB)");

                var directory = Path.Combine(Directory.GetCurrentDirectory(), "VRCWorldImages");
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var fileName = ID + new FileInfo(file.FileName).Extension;

                var filePath = Path.Combine(directory, fileName);

                using FileStream stream = new(filePath, FileMode.Create);
                file.CopyTo(stream);
            }
            else
            {
                return BadRequest("File not provided.");
            }


            VRCW World = new VRCW()
            {
                github_OAuth = Token,
                githubRepo = Repo,
                worldName = WorldName,
                worldDescription = WorldDesc,
                worldCreator = User.login
            };
            Server.Database.Worlds.TryAdd(ID, World);
        }
        catch
        {
            return Problem();
        }
        string redirectScript = @"<script>window.location.href='https://vrc.magmamc.dev/';</script>";

        return Content(redirectScript, "text/html");
    }

    [HttpGet("Worlds/{ID}/Image")]
    public IActionResult GetWorldImage(string ID)
    {

        try
        {
            // Retrieve the directory where the images are stored
            var directory = Path.Combine(Directory.GetCurrentDirectory(), "VRCWorldImages");

            // Get all files in the directory
            var files = Directory.GetFiles(directory);

            // Find the first file that starts with the provided ID
            var matchingFile = files.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).ToLower().Equals(ID.ToLower()));

            if (matchingFile != null)
            {
                var fileBytes = System.IO.File.ReadAllBytes(matchingFile);
                return File(fileBytes, "image/png");
            }
            else
            {
                return NotFound();
            }
        }
        catch
        {
            return StatusCode(500);
        }

    }

    [HttpGet("Worlds")]
    public IActionResult GetWorlds()
    {
        return Ok(Server.Database.Worlds);
    }

    [HttpGet("Worlds/{ID}")]
    public IActionResult GetWorlds(string ID)
    {
        try
        {
            return Ok(Server.Database.Worlds[ID]);

        }
        catch
        {
            return NotFound("Not Found");
        }

    }

    [HttpGet("Worlds/{WorldID}/{Username}")]
    public IActionResult GetUser(string WorldID, string Username)
    {
        try
        {
            VRCW World = Server.Database.Worlds[WorldID];
            SiteUser User = new SiteUser();
            User.username = Username;
            User.siteOwner = User.username.ToLower() == Server.Database.SiteOwner.ToLower();
            User.worldCreator = User.username.ToLower() == World.worldCreator.ToLower();
            User.siteAdmin = World.siteAdmins.Select(u => u.ToLower()).Contains(Username.ToLower());
            return Ok(User);

        }
        catch
        {
            return NotFound("Not Found");
        }
    }
    #endregion

    #region Posts
    [HttpGet("Worlds/{ID}/Posts")]
    public IActionResult GetPosts(string WorldID)
    {
        try
        {

            VRCW? World = null;
            Server.Database.Worlds.TryGetValue(WorldID, out World);
            if (World == null)
                return NotFound("World Not Found");

            return Ok(World.Posts);
            
        }
        catch
        {
            return Problem("Internal Error");
        }
    }

    [HttpGet("Worlds/{WorldID}/Posts/{PostID}")]
    public IActionResult GetPost(string WorldID, string PostID)
    {
        try
        {

            VRCW? World = null;
            Server.Database.Worlds.TryGetValue(WorldID, out World);
            if (World == null)
                return NotFound("World Not Found");

            Post? Post = null;
            int PostNumber = int.MaxValue;

            int.TryParse(PostID, out PostNumber);

            if (World.Posts.Count < PostNumber)
                return NotFound("Post Not Found");

            Post = World.Posts[PostNumber];
            return Ok(Post);

        }
        catch
        {
            return Problem("Internal Error");
        }
    }

    [HttpPost("Worlds/{WorldID}/Posts/")]
    public IActionResult AddPost(string WorldID)
    {
        try
        {
            string? Token = Request.Cookies["AuthToken"] ?? Request.Headers.Authorization;

            if (string.IsNullOrWhiteSpace(Token))
                return Unauthorized("Unauthorized");
            GithubUser? User = GithubAuth.GetUser(Token);
            if (User == null)
                return Unauthorized("Unauthorized");

            string HeaderName = Request.Form["HeaderName"].ToString();
            string Description = Request.Form["Description"].ToString();

            VRCW? World = null;
            Server.Database.Worlds.TryGetValue(WorldID, out World);
            if (World == null)
                return NotFound("World Not Found");
            World.Posts.Add(new Post()
            {
                headerName = HeaderName,
                originalPoster = User.login,
                description = Description
            });
            return Ok(World);


        }
        catch
        {
            return Problem("Internal Error");
        }

    }

    [HttpPost("Worlds/{WorldID}/Posts/{PostID}")]
    public IActionResult AddPostReply(string WorldID, string PostID)
    {
        try
        {
            string? Token = Request.Cookies["AuthToken"] ?? Request.Headers.Authorization;

            if (string.IsNullOrWhiteSpace(Token))
                return Unauthorized("Unauthorized");
            GithubUser? User = GithubAuth.GetUser(Token);
            if (User == null)
                return Unauthorized("Unauthorized");

            string Text = Request.Form["Text"].ToString();


            VRCW? World = null;
            Server.Database.Worlds.TryGetValue(WorldID, out World);
            if (World == null)
                return NotFound("World Not Found");

            Post? Post = null;
            int PostNumber = int.MaxValue;

            int.TryParse(PostID, out PostNumber);

            if (World.Posts.Count < PostNumber)
                return NotFound("Post Not Found");

            Post = World.Posts[PostNumber];

            PostReply postReply = new PostReply();
            postReply.username = User.login;
            postReply.text = Text;

            Post.replies.Add(postReply);
            World.Posts[PostNumber] = Post;
            Server.Database.Worlds[WorldID] = World;
            return Ok(World);
        }
        catch
        {
            return Problem("Internal Error");
        }

    }
    #endregion

    #region Administration

    [HttpGet("Worlds/{WorldID}/Groups/{PermissionGroup}")]
    public IActionResult GetPlayerPermissions(string WorldID, string PermissionGroup)
    {
        string? Token = Request.Cookies["AuthToken"] ?? Request.Headers.Authorization;
        string groupname = PermissionGroup.Trim().ToUpper();

        if (string.IsNullOrWhiteSpace(Token))
            return Unauthorized("Unauthorized");
        GithubUser? User = GithubAuth.GetUser(Token);
        if (User == null)
            return Unauthorized("Unauthorized");

        VRCW? World;
        Server.Database.Worlds.TryGetValue(WorldID, out World);
        if (World == null)
            return NotFound("World Not Found");

        if (!User.IsMapAdmin(World))
            return Unauthorized("Unauthorized");

        if (!World.permissionsData.ContainsKey(groupname))
            return NotFound("Group Not Found");

        PlayerItem[] permissions = ((PlayerItem[])World.permissionsData[groupname].Values.ToArray().Clone());

        unsafe
        {
            fixed (PlayerItem* p = permissions)
            {
                for (int i = 0; i < permissions.Length; i++)
                {
                    PlayerItem* currentItem = p + i;
                    currentItem->displayName = VRChat.GetUser(currentItem->playerID).displayName;
                }
            }
        }

        return Ok(permissions);
    }

    [HttpPost("Worlds/{WorldID}/Groups/{PermissionGroup}")]
    public IActionResult EditPlayerPermissions(string WorldID, string PermissionGroup)
    {
        string? Token = Request.Cookies["AuthToken"] ?? Request.Headers.Authorization;
        string groupname = PermissionGroup.Trim().ToUpper();

        if (string.IsNullOrWhiteSpace(Token))
            return Unauthorized("Unauthorized");
        GithubUser? User = GithubAuth.GetUser(Token);
        if (User == null)
            return Unauthorized("Unauthorized");

        VRCW? World;
        Server.Database.Worlds.TryGetValue(WorldID, out World);
        if (World == null)
            return NotFound("World Not Found");

        if (!User.IsMapAdmin(World))
            return Unauthorized("Unauthorized");

        if (!World.permissionsData.ContainsKey(groupname))
            return NotFound("Group Not Found");

        var Group = World.permissionsData[groupname];
        string PlayerID = Request.Form["PlayerID"].ToString().ToLower().Trim();
        string Message = Request.Form["Message"].ToString().Trim().ToUpper();
        PlayerItem Player = new PlayerItem();
        Player.playerID = PlayerID;
        Player.userAdded = User.login;
        Player.message = Message;
        VRCUser VRCuser = VRChat.GetUser(Player.playerID);
        if (VRCuser == null || VRCuser.displayName == "[NOTLOADED]")
            return BadRequest("Bad UserID");

        switch (Request.Form["FormType"].ToString().ToUpper().Trim())
        {
            case "ADD":

                if (!Group.TryAdd(PlayerID, Player))
                    return StatusCode(501, "Failed");
                else
                {
                    World.Commits.Add($"{User.login}, Added The User: {VRCuser.displayName} To The Group: {groupname}");
                    World.permissionsData[groupname] = Group;
                    Server.Database.Worlds[WorldID] = World;
                    return Ok("Success");
                }
            case "REMOVE" or "DELETE":
                if (!Group.TryRemove(PlayerID, out _))
                    return StatusCode(501, "Failed");
                else
                {
                    World.Commits.Add($"{User.login}, Removed The User: {VRCuser.displayName} To The Group: {groupname}");
                    World.permissionsData[groupname] = Group;
                    Server.Database.Worlds[WorldID] = World;
                    return Ok("Success");
                }
            default:
                return BadRequest("Bad FormType");

        }

    }

    [HttpGet("Worlds/{WorldID}/Groups")]
    public IActionResult GetPlayerGroups(string WorldID)
    {
        string? Token = Request.Cookies["AuthToken"] ?? Request.Headers.Authorization;

        if (string.IsNullOrWhiteSpace(Token))
            return Unauthorized("Unauthorized");
        GithubUser? User = GithubAuth.GetUser(Token);
        if (User == null)
            return Unauthorized("Unauthorized");

        VRCW? World;
        Server.Database.Worlds.TryGetValue(WorldID, out World);
        if (World == null)
            return NotFound("World Not Found");

        if (!User.IsMapAdmin(World))
            return Unauthorized("Unauthorized");

        return Ok(World.groupPermissions);
    }

    [HttpPost("Worlds/{WorldID}/Groups")]
    public IActionResult EditPlayerGroups(string WorldID)
    {
        string? Token = Request.Cookies["AuthToken"] ?? Request.Headers.Authorization;

        if (string.IsNullOrWhiteSpace(Token))
            return Unauthorized("Unauthorized");
        GithubUser? User = GithubAuth.GetUser(Token);
        if (User == null)
            return Unauthorized("Unauthorized");

        VRCW? World;
        Server.Database.Worlds.TryGetValue(WorldID, out World);
        if (World == null)
            return NotFound("World Not Found");

        if (!User.IsMapAdmin(World))
            return Unauthorized("Unauthorized");

        string GroupName = Request.Form["GroupName"].ToString().Trim().ToUpper();
        string[] Permissions = Request.Form["Permissions"].ToString().Trim().Split("+");
        switch (Request.Form["FormType"].ToString().ToUpper().Trim())
        {
            case "ADD":

                if (World.permissionsData.TryGetValue(GroupName, out _))
                {
                    ThreadList<string> NewPermissions = World.groupPermissions.GetOrAdd(GroupName, Permissions);
                    foreach (string Permission in Permissions)
                        NewPermissions.Add(Permission);

                    World.Commits.Add($"{User.login}, Added The Group: {GroupName}");
                    Server.Database.Worlds[WorldID] = World;
                    return Ok("Success");
                }
                if (!World.permissionsData.TryAdd(GroupName, new ConcurrentDictionary<string, PlayerItem>()) || !World.groupPermissions.TryAdd(GroupName, Permissions))
                    return StatusCode(501, "Failed");
                else
                {
                    World.Commits.Add($"{User.login}, Added The Group: {GroupName}");
                    Server.Database.Worlds[WorldID] = World;
                    return Ok("Success");
                }
            case "REMOVE" or "DELETE":
                if (!World.permissionsData.TryRemove(GroupName, out _) || !World.groupPermissions.TryRemove(GroupName, out _))
                    return StatusCode(501, "Failed");
                else
                {
                    World.Commits.Add($"{User.login}, Removed The Group: {GroupName}");
                    Server.Database.Worlds[WorldID] = World;
                    return Ok("Success");
                }
            default:
                return BadRequest("Bad FormType");

        }

    }

    [HttpGet("Worlds/{WorldID}/SiteAdmins")]
    [HttpPost("Worlds/{WorldID}/SiteAdmins")]
    public IActionResult SiteAdmins(string WorldID)
    {
        string? Token = Request.Cookies["AuthToken"] ?? Request.Headers.Authorization;

        if (string.IsNullOrWhiteSpace(Token))
            return Unauthorized("Unauthorized");
        GithubUser? User = GithubAuth.GetUser(Token);
        if (User == null)
            return Unauthorized("Unauthorized");

        VRCW? World;
        Server.Database.Worlds.TryGetValue(WorldID, out World);
        if (World == null)
            return NotFound("World Not Found");

        if (!User.IsMapAdmin(World))
            return Unauthorized("Unauthorized");

        if (!Request.HasFormContentType)
            return Ok(World.siteAdmins);
        switch (Request.Form["FormType"].ToString().ToUpper().Trim())
        {
            case "ADD":
                if (!User.IsMapMaster(WorldID))
                    return Unauthorized("Unauthorized");
                World.siteAdmins.Add(Request.Form["User"].ToString());
                Server.Database.Worlds[WorldID] = World;
                break;
            case "REMOVE" or "DELETE":
                if (!User.IsMapMaster(WorldID))
                    return Unauthorized("Unauthorized");
                World.siteAdmins.Remove(Request.Form["User"].ToString());
                Server.Database.Worlds[WorldID] = World;
                break;
            default:
                return BadRequest("Bad FormType");
        }
        return Ok();
    }

    [HttpGet("Worlds/{WorldID}/GetCommits")]
    public IActionResult GetCommits(string WorldID)
    {
        string? Token = Request.Cookies["AuthToken"] ?? Request.Headers.Authorization;

        if (string.IsNullOrWhiteSpace(Token))
            return Unauthorized("Unauthorized");
        GithubUser? User = GithubAuth.GetUser(Token);
        if (User == null)
            return Unauthorized("Unauthorized");

        VRCW? World;
        Server.Database.Worlds.TryGetValue(WorldID, out World);
        if (World == null)
            return NotFound("World Not Found");

        if (!User.IsMapAdmin(World))
            return Unauthorized("Unauthorized");
        return Ok(World.Commits);
    }

    [HttpPost("Worlds/{WorldID}/PushCommits")]
    public async Task<IActionResult> PublishContent(string WorldID)
    {
        string? Token = Request.Cookies["AuthToken"] ?? Request.Headers.Authorization;

        if (string.IsNullOrWhiteSpace(Token))
            return Unauthorized("Unauthorized");
        GithubUser? User = GithubAuth.GetUser(Token);
        if (User == null)
            return Unauthorized("Unauthorized");

        VRCW? World;
        Server.Database.Worlds.TryGetValue(WorldID, out World);
        if (World == null)
            return NotFound("World Not Found");

        if (!User.IsMapAdmin(World))
            return Unauthorized("Unauthorized");
        GithubRepoControl Repo = new GithubRepoControl(World.githubRepo, World.github_OAuth);
        if (await Repo.UpdateFileContentAsync("ServerPermissions.PSC", World.ConvertPSC(), $"{User.login}, Pushed Commits:\n{string.Join('\n', World.Commits)}"))
        {
            World.Commits.Clear();
            return Ok("Successfully Pushed Commits");
        }
        else
            return StatusCode(500);
    }

    #endregion

    #region VRChat
    [HttpGet("Users/{UserID}")]
    [HttpGet("VRChat/Users/{UserID}")]
    public IActionResult GetVRChatUser(string UserID)
    {
        string? Token = Request.Cookies["AuthToken"] ?? Request.Headers.Authorization;

        if (string.IsNullOrWhiteSpace(Token))
            return Unauthorized("Unauthorized");
        GithubUser? User = GithubAuth.GetUser(Token);
        if (User == null)
            return Unauthorized("Unauthorized");
        if (!User.IsSiteOwner())
            return Unauthorized("Unauthorized");
        VRCUser vrcuser = VRChat.GetUser(UserID);
        return Ok(vrcuser);
    }

    [HttpGet("Users/{UserID}/Validate")]
    [HttpGet("VRChat/Users/{UserID}/Validate")]
    public IActionResult ValidateVRChatUser(string UserID)
    {
        string? Token = Request.Cookies["AuthToken"] ?? Request.Headers.Authorization;

        if (string.IsNullOrWhiteSpace(Token))
            return Unauthorized("Unauthorized");
        GithubUser? User = GithubAuth.GetUser(Token);
        if (User == null)
            return Unauthorized("Unauthorized");
        if (!User.IsSiteOwner())
            return Unauthorized("Unauthorized");
        if (VRChat.GetUser(UserID).displayName != "[NOTLOADED]")
            return Ok(true);
        else
            return NotFound(false);
    }
    #endregion

    public static bool IsAscii(string value) => Regex.IsMatch(value, @"^[\x00-\x7F\s]+$");
}