namespace VRCWMT.Models;

public class PlayerItem
{
    public string displayName { get; set; } = "";
    public string playerID { get; set; } = "";
    public string message { get; set; } = "";
    public DateTime timeAdded { get; set; } = DateTime.Now;
    public string userAdded { get; set; } = "";
}
