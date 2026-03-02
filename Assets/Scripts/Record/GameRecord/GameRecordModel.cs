using QFramework;

public class GameRecordModel : AbstractModel
{
    public string UserId;
    public bool IsSecordToMainMenu;

    protected override void OnInit()
    {
        UserId = "";
        IsSecordToMainMenu = false;
    }
}