using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

public class Astar : MonoBehaviour
{
    public Transform seeker, target;
    public GameObject gridManager;

    MapGridGenerator grid;

    public bool shouldPrintTimer;
    public Stopwatch timer;

    public List<Node> aStarPath;

    private bool firstCalcuation = true;

    void Awake()
    {
        grid = gridManager.GetComponent<MapGridGenerator>();
        timer = new Stopwatch();
        aStarPath = new List<Node>();
    }

    public List<Node> FindPath(Vector3 startpos, Vector3 targetpos)
    {
        aStarPath.Clear();
        timer.Reset();
        timer.Start();
        Node startNode = grid.NodeFromWorldPoint(startpos);
        Node targetNode = grid.NodeFromWorldPoint(targetpos);

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while(openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
                    currentNode = openSet[i];
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                TracePath(startNode, targetNode);
                break;
            }


            foreach(Node neighbour in grid.GetNeighbours(currentNode))
            {
                if (!neighbour.walkable || closedSet.Contains(neighbour))
                    continue;

                int newMovementCost = currentNode.gCost + GetDistance(currentNode, neighbour);
                if(newMovementCost < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newMovementCost;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);
                }
            }
        }
        timer.Stop();
        if(shouldPrintTimer)
            print(timer.Elapsed);
        //print("Astar complete");

        if (!firstCalcuation)
            TextHandler.subsequentPathCalcTimes.Add(timer.ElapsedMilliseconds);
        else if(firstCalcuation)
        {
            firstCalcuation = false;
            TextHandler.initialPathCalcTime = timer.ElapsedMilliseconds;
        }
        return aStarPath;
    }

    void TracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while(currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        aStarPath = path;
        //grid.playerPath = path;
        //grid.OnComplete();
    }

    int GetDistance(Node nodeA, Node nodeB)
    {
        int distX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int distY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if (distX > distY)
            return 14 * distY + 10 * (distX - distY);

        return 14 * distX + 10 * (distY - distX);
    }

}
