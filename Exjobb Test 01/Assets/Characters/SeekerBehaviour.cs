using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

public enum Pathfinder
{
    ACO,
    PSO,
    AStar
}

public class SeekerBehaviour : MonoBehaviour
{
    public float movementSpeed = 3f;

    public GameObject[] pathfinders;
    public GameObject grid;
    public GameObject target;

    public Pathfinder pathType;

    private Vector3 pos;
    private Transform form;

    private Node nextNode;

    private bool inTransition = false;
    private bool pathCalculationInProgress = false;
    private bool simulationRunning = false;

    public int currentNodeIndex = 0;

    public List<Node> path;
    private List<Node> travelledPath;

    public Stopwatch gameTimer;

    public float timeBetweenUpdates     = 0.5f;
    private float timeSinceLastUpdate   = 0f;

    public Color pathColour = Color.blue;

    void Start()
    {
        pos     = transform.position;
        form    = transform;

        gameTimer = new Stopwatch();
        path = new List<Node>();
        travelledPath = new List<Node>();
    }

    void Update()
    {
        //if(Input.GetKeyDown(KeyCode.A))
        //{
        //    WriteResults();
        //    print("writing");
        //}
        if (Input.GetKeyDown(KeyCode.Space) && !simulationRunning)
        {
            simulationRunning = true;
            gameTimer.Reset();
            gameTimer.Start();
            PathCalculate();
        }

        if(simulationRunning)
        {
            if(!pathCalculationInProgress && timeSinceLastUpdate > timeBetweenUpdates && !inTransition)
            {
                PathCalculate();          
            }
            else 
            {
                // movement code
                MoveToDestination();
            }
            timeSinceLastUpdate += Time.deltaTime;
        }
        if(Input.GetKeyDown(KeyCode.B))
        {
            OnGameComplete();
        }
    }

    void PathCalculate()
    {
        pathCalculationInProgress = true;
        path.Clear();
        // calculate path
        switch (pathType)
        {
            case Pathfinder.ACO:
                path = pathfinders[0].GetComponent<ACO>().FindPath(travelledPath);
                TextHandler.algorithmUsed = "ACO";
                break;
            case Pathfinder.PSO:
                path = pathfinders[1].GetComponent<PSO>().FindPath(transform.position, target.transform.position);
                TextHandler.algorithmUsed = "PSO";
                break;
            case Pathfinder.AStar:
                path = pathfinders[2].GetComponent<Astar>().FindPath(transform.position, target.transform.position);
                TextHandler.algorithmUsed = "Astar";
                break;
        }
        currentNodeIndex = 0;
        timeSinceLastUpdate = 0;
        pathCalculationInProgress = false;
    }

    void MoveToDestination()
    {
        if (path != null)
        {
            // If not currently in transition then determine next node
            if (!inTransition)
            {
                if(currentNodeIndex < path.Count)
                {
                    nextNode = path[currentNodeIndex];
                    nextNode.pheromone = nextNode.initialPhermone;
                    pos = nextNode.worldPosition;
                    travelledPath.Add(nextNode);
                    inTransition = true;
                }
                else
                {
                    PathCalculate();
                }
            }
            else
            {
                if(form.position == pos)
                {
                    currentNodeIndex++;
                    inTransition = false;
                }

                transform.position = Vector3.MoveTowards(transform.position, pos, Time.deltaTime * movementSpeed);
            }
        }
    }

    public void OnGameComplete()
    {
        simulationRunning = false;
        target.GetComponent<TargetBehaviour>().ChangeSimulationStatus(false);
        gameTimer.Stop();
        grid.GetComponent<MapGridGenerator>().IllustratePath(travelledPath, pathColour);
        print("Simulation Complete: Time - " + gameTimer.ElapsedMilliseconds);
        TextHandler.gameCompleteTime = gameTimer.ElapsedMilliseconds;
        TextHandler.seekerPathLength = travelledPath.Count;
        TextHandler.CreateText();
        TextHandler.WriteToTextFile(@"E:\School\Exjobb\Exjobb Test 01\Assets\Resources\TextPrintTest.txt");
    }

    //void WriteResults()
    //{
    //    string[] readLines = File.ReadAllLines(@"E:\School\Exjobb\Exjobb Test 01\Assets\Resources\TextPrintTest.txt");
    //    string[] lines = { "asdf ", "second line ", "asfd hello there" };
    //    string[] combinedLines = new string[readLines.Length + lines.Length];
    //    readLines.CopyTo(combinedLines, 0);
    //    lines.CopyTo(combinedLines, readLines.Length);
    //    File.WriteAllLines(@"E:\School\Exjobb\Exjobb Test 01\Assets\Resources\TextPrintTest.txt", combinedLines);
    //    print("done writing");
    //}
}
