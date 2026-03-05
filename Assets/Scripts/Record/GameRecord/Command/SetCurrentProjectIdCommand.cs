using QFramework;
using UnityEngine;

namespace Record.GameRecord.Command
{
    public class SetCurrentProjectIdCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            var projectId = this.GetUtility<TurnoutDatabaseManager>().GetProjectId(ProjectName.人工准备进路.ToString());
            Debug.Log("projectId:" + projectId);
            if (projectId.HasValue)
            {
                this.GetModel<GameRecordModel>().currentprojectId = projectId.Value;
            }
        }
    }
}