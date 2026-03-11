using QFramework;

public class GameRecordModel : AbstractModel
{
    public string UserId;
    public int currentprojectId;
    public bool IsSecordToMainMenu;

    protected override void OnInit()
    {
        UserId = "";
        IsSecordToMainMenu = false;
    }

    public void ResetValues()
    {
        UserId = null;
        currentprojectId = 0;
    }
}