using System.Text;
using Newtonsoft.Json.Linq;
using VRCWMT;

namespace ServerBackend;

public class GithubRepoControl: IDisposable
{
    private readonly HttpClient _client;

    public GithubRepoControl(string repo, string authToken)
    {
        _client = new HttpClient();
        _client.DefaultRequestHeaders.Add("Authorization", $"token {authToken}");
        _client.DefaultRequestHeaders.Add("User-Agent", Server.User_Agent);
        Owner = repo.Split('/')[0];
        Repo = repo.Split('/')[1];
    }

    public string Repo
    {
        get;
    }
    public string Owner
    {
        get;
    }

    public async Task<FileContentResponse?> GetFileContentAsync(string fileName)
    {
        try
        {
            var response = await _client.GetAsync($"https://api.github.com/repos/{Owner}/{Repo}/contents/{fileName}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var fileContent = Newtonsoft.Json.JsonConvert.DeserializeObject<FileContentResponse>(content)!;
            return fileContent;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateFileContentAsync(string fileName, string data, string commitMessage)
    {
        try
        {
            var currentContent = await GetFileContentAsync(fileName) ?? new FileContentResponse();
            var newContent = Convert.ToBase64String(Encoding.UTF8.GetBytes(data));

            var requestBody = new
            {
                committer = new
                {
                    name = "VRCWMT Server",
                    email = "no-reply@magma-mc.net"
                },
                message = commitMessage,
                content = newContent,
                sha = currentContent.sha
            };

            var requestUri = $"https://api.github.com/repos/{Owner}/{Repo}/contents/{fileName}";

            var response = await _client.PutAsync(requestUri, new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"));

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            return false;
        }
    }

    public class FileContentResponse
    {
        public string content
        {
            get; set;
        } = string.Empty;
        public string sha
        {
            get; set;
        } = string.Empty;

        public string DecodeContent()
        {
            var contentBytes = Convert.FromBase64String(content);
            return Encoding.UTF8.GetString(contentBytes);
        }
        public override string ToString() => DecodeContent();
        public static implicit operator string(FileContentResponse content) => content.DecodeContent();
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}