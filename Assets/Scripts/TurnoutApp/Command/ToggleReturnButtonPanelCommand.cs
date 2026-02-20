using QFramework;
using UnityEngine;

public class ToggleReturnButtonPanelCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        this.SendEvent<ToggleReturnButtonPanelEvent>();
    }
}