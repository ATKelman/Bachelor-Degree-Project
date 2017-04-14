using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Grid : MonoBehaviour
{
    public LayerMask unwalkableMask;
    public Vector2 gridWorldSize;
    public float nodeRadius;

    Node[,] grid;

    float nodeDiameter;
    int gridSizeX, gridSizeY;

    void Start()
    {
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        CreateGrid();
    }

    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.up * gridWorldSize.y/2;

        for(int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.up * (y * nodeDiameter + nodeRadius);
                bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask));
                grid[x, y] = new Node(walkable, worldPoint, x, y);
            }
        }
    }

    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();
        // Check nodes in 3x3 grid around node
        for(int x = -1; x <= 1; x++)
        {
            for(int y = -1; y <= 1; y++)
            {
                // Skip current node
                if (x == 0 && y == 0)
                    continue;

                // Check if inside grid
                int checkX = node.gridX + x;
                int checkY = node.gridY + y;
                if( checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbours.Add(grid[checkX, checkY]);
                }
            }
        }
        return neighbours;
    }

    // PSO function
    public Node MoveToNode(Node node, int velX, int velY)
    {

        int checkX = node.gridX + velX;
        int checkY = node.gridY + velY;

        if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
        {
            return grid[checkX, checkY];
        }
        else
        {
            return GetRandomNeighbour(node);
        }
    }

    public Node LazyUpdatePSO(Node n, int velX, int velY)
    {
        int checkX = n.gridX + velX;
        int checkY = n.gridY + velY;

        if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
        {
            return grid[checkX, checkY];
        }
        else
        {
            print("Lazy update out of range");
            return n;
        }
    }

    public Node LazyMove(int gridX, int gridY)
    {
        int x = gridX;
        int y = gridY;

        if (gridX > gridSizeX)
            x = gridSizeX - 1;
        else if (gridX < 0)
            x = 0;

        if (gridY > gridSizeY)
            y = gridSizeY - 1;
        else if (gridY < 0)
            y = 0;

        return grid[x, y];
    }

    // ACO function
    public Node GetRandomNeighbour(Node node)
    {
        Node randomNeighbour = null;

        while(randomNeighbour == null)
        {
            int randX = Random.Range(-1, 1);
            int randY = Random.Range(-1, 1);

            if (randX == 0 && randY == 0)
                continue;

            int checkX = node.gridX + randX;
            int checkY = node.gridY + randY;
            if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
            {
                randomNeighbour = grid[checkX, checkY];
            }
        }
        return randomNeighbour;
    }

    public Node NodeFromWorldPoint(Vector3 pWorldPosition)
    {
        float percentX = (pWorldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (pWorldPosition.y + gridWorldSize.y / 2) / gridWorldSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        return grid[x,y];
    }

    public List<Node> path;
    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, gridWorldSize.y, 1));

        if (grid != null)     
        {
            foreach (Node n in grid)
            {
                Gizmos.color = (n.walkable) ? Color.white : Color.red;
                if(path != null)
                {
                    if(path.Contains(n))
                    {
                        Gizmos.color = Color.black; 
                    }
                }

                Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - 0.1f));
            }
        }
    }
}
