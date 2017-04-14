using UnityEngine;
using System.Collections;

public class Node 
{
    public bool walkable;
    public Vector3 worldPosition;
    public int gridX;
    public int gridY;

    public Node parent;
    public int gCost;
    public int hCost;

    public float pheromone;
    public float initialPhermone;
    public float antValue;
    public float attractiveness;

    public int[] gridPosition;

    public Node(bool pWalkable, Vector3 pWorldPos, int pGridX, int pGridY)
    {
        walkable = pWalkable;
        worldPosition = pWorldPos;
        gridX = pGridX;
        gridY = pGridY;

        gridPosition = new int[2];
        gridPosition[0] = pGridX;
        gridPosition[1] = pGridY;

        attractiveness = 0;
        pheromone = 0.25f;
        initialPhermone = 0.20f;
    }

    public int fCost
    {
        get { return gCost + hCost; }
    }

}
