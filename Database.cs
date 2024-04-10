using Newtonsoft.Json;
using MagmaMc.MagmaSimpleConfig.Utils;
using System.Text;
using Logger = MagmaMc.SharedLibrary.Logger;
using NanoidDotNet;
using MagmaMc.SharedLibrary;
namespace ServerBackend;

public class Database
{
    public const string FileName = "VRCW-Worlds.db";
    public const string SecurityKey = "VRCWMT";
    public static string NewID => "WRD_"+Nanoid.Generate("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ", 32);
    public string SiteOwner { get; set; } = "";
    public Dictionary<string, VRCW> Worlds { get; set; } = [];


    private static readonly Logger Debug = new(LoggingLevel.Debug);
    private static bool FileWriteTimeout = false;
    public static void SaveContents(Database DB)
    {
        if (FileWriteTimeout)
        {
            Debug.Warn("File Already Being Written Please Wait");
            return;
        }
        FileWriteTimeout = true;
        Task.Run(() =>
        {
            try
            {
                string JsonString = JsonConvert.SerializeObject(DB);
                byte[] EncryptedData = AES.EncryptData(Encoding.UTF8.GetBytes(JsonString), SecurityKey);
                File.WriteAllBytes(FileName, EncryptedData);
            }
            catch
            {
                Debug.Error("Failed To Write Config File");
            }
            finally { FileWriteTimeout = false; }
        });
    }
    public static Database? LoadContents()
    {
        try
        {
            byte[] DecryptedData = AES.DecryptData(File.ReadAllBytes(FileName), SecurityKey);
            Database? DB = JsonConvert.DeserializeObject<Database>(Encoding.UTF8.GetString(DecryptedData));
            if (DB == null)
                throw new IOException("Failed To Read Config File");
            return DB;
        }
        catch
        {
            Debug.Error("Failed To Read Config File");
            return null;
        }
        
    }

}
