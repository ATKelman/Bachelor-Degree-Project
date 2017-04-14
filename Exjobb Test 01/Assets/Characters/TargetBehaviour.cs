using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TargetBehaviour : MonoBehaviour
{
    public float movementSpeed = 1f;

    public GameObject[] waypoints;

    private Vector3 pos;
    private Transform form;

    private Node nextNode;

    private bool inTransition = false;
    private bool pathCalculated = false;
    private bool pathCalculationInProgress = false;
    private bool isSimulationRunning = false;

    private int currentNodeIndex = 0;
    private int currentWaypointIndex = 0;

    private List<Node> travelledPath;

    private List<Node> path;

    public GameObject gridManager;
    MapGridGenerator grid;

    void Start()
    {
        pos = transform.position;
        form = transform;

        grid = gridManager.GetComponent<MapGridGenerator>();
        travelledPath = new List<Node>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            isSimulationRunning = true;

        if(isSimulationRunning)
        {
            if (pathCalculated)
            {
                MoveToDestination();
            }
            else if(!pathCalculated && !pathCalculationInProgress)
            {
                pathCalculationInProgress = true;
                path = GetComponent<PathingTarget>().FindPath(transform.position, waypoints[currentWaypointIndex].transform.position);
                //grid.GetComponent<MapGridGenerator>().IllustratePath(path, Color.green);
                currentNodeIndex = 0;
                pathCalculated = true;
            }
        }
    }

    void MoveToDestination()
    {
        if(path != null)
        {
            if (!inTransition)
            {
                // Update movement based on path to current waypoint
                if (currentNodeIndex < path.Count)
                {
                    nextNode = path[currentNodeIndex];
                    pos = nextNode.worldPosition;
                    travelledPath.Add(nextNode);
                    inTransition = true;
                }
                // Update movement based on new path to next waypoint
                else if (currentWaypointIndex < waypoints.Length)
                {
                    currentWaypointIndex++;
                    currentWaypointIndex = currentWaypointIndex % waypoints.Length;
                    path = GetComponent<PathingTarget>().FindPath(transform.position, waypoints[currentWaypointIndex].transform.position);
                    //grid.GetComponent<MapGridGenerator>().IllustratePath(path, Color.blue);
                    currentNodeIndex = 0;
                }
            }
            else
            {
                // Check if node reached
                if (form.position == pos)
                {
                    currentNodeIndex++;
                    inTransition = false;
                }

                transform.position = Vector3.MoveTowards(transform.position, pos, Time.deltaTime * movementSpeed);
            }
        }
    }

    public void ChangeSimulationStatus(bool status)
    {
        isSimulationRunning = status;
        if(!status)
            grid.IllustratePath(travelledPath, Color.white);
    }

    public void OnTriggerEnter(Collider coll)
    {
        if (coll.tag == "Seeker")
        {
            coll.GetComponent<SeekerBehaviour>().OnGameComplete();
            print("collided game complete");
        }
    }
}
