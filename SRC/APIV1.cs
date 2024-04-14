using System.Text.RegularExpressions;
using MagmaMc.UAS;
using Microsoft.AspNetCore.Mvc;

namespace ServerBackend;
[Route("api/v1/")]
[ApiController]
public class APIV1 : ControllerBase
{
    public const string APIRoute = "https://vrc.magmamc.dev/API/V1";
    public const string ClientID = "42c4f36cde15ada6d5db";
    public const string ClientSecret = "78b51f69bb394ef2c1b9c0febde4fed906855648";

    [HttpGet("Logout")]
    [HttpPost("Logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("AuthToken");
        string redirectScript = @"<script>window.location.href='https://vrc.magmamc.dev/';</script>";

        return Content(redirectScript, "text/html");
    }

    [HttpGet("Login")]
    [HttpGet("Github/Login")]
    public IActionResult GithubLogin()
    {
        var githubAuthUrl = $"https://github.com/login/oauth/authorize?client_id={ClientID}&scope=repo,read:user&redirect_uri={APIRoute}/Github/OAuth/";

        return Redirect(githubAuthUrl);
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
        HttpResponseMessage message = await httpClient.PostAsync($"https://github.com/login/oauth/access_token?client_id={ClientID}&client_secret={ClientSecret}&code={authCode}", null);
        GithubOAuth? OAuth = await message.Content.ReadFromJsonAsync<GithubOAuth>();
        Response.Cookies.Append("AuthToken", OAuth.access_token);
        string redirectScript = @"<script>window.location.href='https://vrc.magmamc.dev/';</script>";

        return Content(redirectScript, "text/html");
    }


    [HttpPost("Worlds")]
    public IActionResult AddWorld(IFormFile file)
    {
        try
        {
            string? Token = Request.Cookies["AuthToken"];

            if (string.IsNullOrWhiteSpace(Token))
                return Unauthorized("Unauthorized");
            GithubUser? User = GithubAuth.GetUser(Token);
            Console.WriteLine(Token);
            if (User == null)
                return Unauthorized("Unauthorized");

            string WorldName = Request.Form["WorldName"].ToString();
            string WorldDesc = Request.Form["WorldDescription"].ToString();

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
                WorldName = WorldName,
                WorldDescription = WorldDesc,
                WorldCreator = User.login
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
            string? Token = Request.Cookies["AuthToken"];

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
                HeaderName = HeaderName,
                OriginalPoster = User.login,
                Description = Description
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
            string? Token = Request.Cookies["AuthToken"];

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
            postReply.Username = User.login;
            postReply.Text = Text;

            Post.Replies.Add(postReply);
            World.Posts[PostNumber] = Post;
            Server.Database.Worlds[WorldID] = World;
            return Ok(World);
        }
        catch
        {
            return Problem("Internal Error");
        }

    }

    [HttpGet("Worlds/{WorldID}/BanUser/Add")]
    public IActionResult BanUser(string WorldID)
    {
        string? Token = Request.Cookies["AuthToken"];

        if (string.IsNullOrWhiteSpace(Token))
            return Unauthorized("Unauthorized");
        GithubUser? User = GithubAuth.GetUser(Token);
        if (User == null)
            return Unauthorized("Unauthorized");

        VRCW? World;
        Server.Database.Worlds.TryGetValue(WorldID, out World);
        if (World == null)
            return NotFound("World Not Found");

        if (User.IsMapAdmin(World))
            return Unauthorized("Unauthorized");

        World.BannedPlayers.Add(Request.Form["User"].ToString());
        Server.Database.Worlds[WorldID] = World;
        return Ok("Success");
    }

    [HttpGet("Worlds/{WorldID}/BanUser/Remove")]
    public IActionResult UnbanUser(string WorldID)
    {
        string? Token = Request.Cookies["AuthToken"];

        if (string.IsNullOrWhiteSpace(Token))
            return Unauthorized("Unauthorized");
        GithubUser? User = GithubAuth.GetUser(Token);
        if (User == null)
            return Unauthorized("Unauthorized");

        VRCW? World;
        Server.Database.Worlds.TryGetValue(WorldID, out World);
        if (World == null)
            return NotFound("World Not Found");

        if (User.IsMapAdmin(World))
            return Unauthorized("Unauthorized");

        World.BannedPlayers.Remove(Request.Form["User"].ToString());
        Server.Database.Worlds[WorldID] = World;
        return Ok("Success");
    }

    [HttpGet("Worlds/{WorldID}/SiteAdmin/Add")]
    public IActionResult AddSiteAdmin(string WorldID)
    {
        string? Token = Request.Cookies["AuthToken"];

        if (string.IsNullOrWhiteSpace(Token))
            return Unauthorized("Unauthorized");
        GithubUser? User = GithubAuth.GetUser(Token);
        if (User == null)
            return Unauthorized("Unauthorized");

        VRCW? World;
        Server.Database.Worlds.TryGetValue(WorldID, out World);
        UserData userData = UserData.GetUserData(Token);
        if (World == null)
            return NotFound("World Not Found");
        if (!User.IsMapMaster(WorldID))
            return Unauthorized("Unauthorized");

        World.SiteAdmins.Add(Request.Form["User"].ToString());
        Server.Database.Worlds[WorldID] = World;
        return Ok("Success");

    }


    [HttpGet("Worlds/{WorldID}/{Username}")]
    public IActionResult GetUserPost(string WorldID, string Username)
    {
        try
        {
            VRCW World = Server.Database.Worlds[WorldID];
            SiteUser User = new SiteUser();
            User.Username = Username;
            User.SiteOwner = User.Username.ToLower() == Server.Database.SiteOwner.ToLower();
            User.WorldCreater = User.Username.ToLower() == World.WorldCreator.ToLower();
            User.SiteAdmin = World.SiteAdmins.Select(u => u.ToLower()).Contains(Username.ToLower());
            return Ok(User);

        }
        catch
        {
            return NotFound("Not Found");
        }

    }

    public static bool IsAscii(string value) => Regex.IsMatch(value, @"^[\x00-\x7F\s]+$");
}

