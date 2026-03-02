using System;
using System.Collections.Generic;
using System.Linq;
using QFramework;
using UnityEngine;

public class TurnoutRecordModel : AbstractModel
{
    public int currentprojectId;
    public int currentStationId;
    public List<LineData> Lines;
    public List<StationData> Stations;
    public List<TurnoutSubProjectData> SubProjects;
    public List<TurnoutNodeData> Nodes;
    private TurnoutDatabaseManager turnoutDatabaseManager;

    protected override void OnInit()
    {
        turnoutDatabaseManager = this.GetUtility<TurnoutDatabaseManager>();
        Lines = turnoutDatabaseManager.GetAllLines();
        Debug.Log("LinesCount:" + Lines.Count);
        Stations = turnoutDatabaseManager.GetStationsByLine(1);
        currentprojectId = 1;
        if (Stations != null)
        {
            Debug.Log("StationsCount:" + Stations.Count);
        }

        SubProjects = new List<TurnoutSubProjectData>();
        Nodes = new List<TurnoutNodeData>();
    }
}