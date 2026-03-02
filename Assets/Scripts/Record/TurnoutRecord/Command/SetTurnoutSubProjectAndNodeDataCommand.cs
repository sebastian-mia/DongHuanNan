using System;
using System.Collections.Generic;
using QFramework;

public class SetTurnoutSubProjectAndNodeDataCommand : AbstractCommand
{
    private TurnoutSubProjectData _turnoutSubProjectData;
    private List<TurnoutNodeData> _turnoutNodeDatas;

    public SetTurnoutSubProjectAndNodeDataCommand(TurnoutSubProjectData turnoutSubProjectData,
        List<TurnoutNodeData> turnoutNodeDatas)
    {
        this._turnoutSubProjectData = turnoutSubProjectData;
        this._turnoutNodeDatas = turnoutNodeDatas;
    }

    protected override void OnExecute()
    {
        this.GetUtility<TurnoutDatabaseManager>().InsertTurnoutProject(_turnoutSubProjectData.ProjectId,
            _turnoutSubProjectData.SubProjectName, _turnoutSubProjectData.UserId, _turnoutSubProjectData.StationId,
            this.GetUtility<TimeFormatter>().FormatDateTime(DateTime.Now), _turnoutNodeDatas);
        this.SendEvent<ModifyTurnoutSubProjectDataEvent>();
        this.SendEvent<ModifyTurnoutNodeDataEvent>();
    }
}