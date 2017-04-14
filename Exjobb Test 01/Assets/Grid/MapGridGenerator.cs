using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapGridGenerator : MonoBehaviour
{
    public GameObject gizmoManager;
    public GameObject Tile;

    public string txtFile = "Map_Warcraft3_Frostsabre";
    private string txtContents;

    public Vector2 gridSize = new Vector2(512, 512);
    public float nodeRadius = 0.5f;

    public List<Node> playerPath;
    public List<Node> currentPath;
    private Node[,] grid;
    private bool[,] mapData;

    private float nodeDiameter;
    private int gridSizeX, gridSizeY;

    private GameObject[,] tiles;

    void Start()
    {
        TextHandler.initializeNewValuesForHandler();
        playerPath = new List<Node>();
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridSize.y / nodeDiameter);
        ReadMapData();
        CreateGrid();
        TextHandler.mapName = txtFile;
    }

    void ReadMapData()
    {
        mapData = new bool[gridSizeX, gridSizeY];

        TextAsset txtAssets = (TextAsset)Resources.Load(txtFile);
        txtContents = txtAssets.text;

        string[] mapDataArray = new string[(int)gridSize.y];

        for (int y = 0; y < mapDataArray.Length; y++)
        {
            int startingIndex = Mathf.RoundToInt((gridSize.x + 1 ) * y);
            mapDataArray[y] = txtContents.Substring(startingIndex, (int)gridSize.x);
            string entireRow = mapDataArray[y];
            for (int x = 0; x < (int)gridSize.x; x++)
            {
                if (entireRow[x] == '.')
                    mapData[x, y] = true;
                else
                    mapData[x, y] = false;
            }
        }
    }

    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        tiles = new GameObject[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position 
            - Vector3.right * gridSize.x / 2 
            - Vector3.up * gridSize.y / 2;

        for(int x = 0; x < gridSizeX; x++)
        {
            for(int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft 
                    + Vector3.right * (x * nodeDiameter + nodeRadius)
                    + Vector3.up * (y * nodeDiameter + nodeRadius);
                bool walkable = mapData[x, y];
                grid[x, y] = new Node(walkable, worldPoint, x, y);

                GameObject instance = (GameObject)Instantiate(Tile, worldPoint, transform.rotation);
                instance.transform.SetParent(transform, true);
                instance.GetComponent<SpriteRenderer>().color = (walkable) ? Color.white : Color.red;
                tiles[x, y] = instance;
            }
        }
    }

    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();
        // Check nodes in 3x3 grid around node
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                // Skip current node
                if (x == 0 && y == 0)
                    continue;

                // Check if inside grid
                int checkX = node.gridX + x;
                int checkY = node.gridY + y;
                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbours.Add(grid[checkX, checkY]);
                }
            }
        }
        return neighbours;
    }

    public List<Node> GetWalkableNeighbours(int[] xy)
    {
        List<Node> neighbours = new List<Node>();

        for(int x = -1; x <= 1; x++)
        {
            for(int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                int checkX = xy[0] + x;
                int checkY = xy[1] + y;
                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY )
                {
                    if (grid[checkX, checkY].walkable)
                        neighbours.Add(grid[checkX, checkY]);
                }
            }
        }
        if (neighbours.Count == 0)
            print("ERROR - random walkable neighbour count 0 -- POSITION " + xy[0] + " " + xy[1]);
        return neighbours;
    }

    public Node NodeFromWorldPoint(Vector3 pWorldPosition)
    {
        float percentX = (pWorldPosition.x + gridSize.x / 2) / gridSize.x;
        float percentY = (pWorldPosition.y + gridSize.y / 2) / gridSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        return grid[x, y];
    }

    public void OnComplete()
    {
        if (playerPath != null)
        {
            foreach (Node n in playerPath)
            {
                tiles[n.gridX, n.gridY].GetComponent<SpriteRenderer>().color = Color.green;
                tiles[n.gridX, n.gridY].transform.localScale = new Vector3(7, 7, 1);
            }
        }
    }

    public void IllustratePath(List<Node> path, Color colour)
    {
        //// Reset old path
        //if(currentPath != null)
        //{
        //    foreach(Node n in currentPath)
        //    {
        //        if(tiles[n.gridX, n.gridY].GetComponent<SpriteRenderer>().color == Color.white)
        //        tiles[n.gridX, n.gridY].transform.localScale = new Vector3(5, 5, 1);
        //    }
        //}
        if(path != null)
        {
            foreach (Node n in path)
            {
                if (tiles[n.gridX, n.gridY].GetComponent<SpriteRenderer>().color == Color.white)
                    tiles[n.gridX, n.gridY].GetComponent<SpriteRenderer>().color = colour;
                else if(tiles[n.gridX, n.gridY].GetComponent<SpriteRenderer>().color == colour) { }
                    // do nothing
                else
                    tiles[n.gridX, n.gridY].GetComponent<SpriteRenderer>().color = Color.yellow;

                tiles[n.gridX, n.gridY].transform.localScale = new Vector3(7, 7, 1);
            }
            currentPath = path;
        }
    }

    // Gizmo functions
    public void ActivateGizmo(List<Node> path)
    {
        gizmoManager.GetComponent<GizmoDrawer>().SetValues(gridSizeX, gridSizeY, grid, path, nodeDiameter);
        gizmoManager.SetActive(true);
    }

    // ACO functions
    public Node GetRandomNeighbour(Node node)
    {
        Node randomNeighbour = null;

        while (randomNeighbour == null)
        {
            int randX = Random.Range(-1, 1);
            int randY = Random.Range(-1, 1);

            if (randX == 0 && randY == 0)
                continue;

            int checkX = node.gridX + randX;
            int checkY = node.gridY + randY;
            if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
            {
                if(grid[checkX, checkY].walkable)
                    randomNeighbour = grid[checkX, checkY];
            }
        }
        return randomNeighbour;
    }

    public Node GetRandomNeighbour(int[] xy)
    {



        return null;
        //Node randomNeighbour = null;

        //while(true)
        //{
        //    int randX = Random.Range(-1, 2);
        //    int randY = Random.Range(-1, 2);

        //    if (randX == 0 && randY == 0)
        //        continue;

        //    int checkX = xy[0] + randX;
        //    int checkY = xy[1] + randY;
        //    if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
        //    {
        //        if (grid[checkX, checkY].walkable)
        //        {
        //            randomNeighbour = grid[checkX, checkY];
        //            break;
        //        }

        //    }
        //}
        //return randomNeighbour;
    }

    // PSO functions
    public bool IsWalkable(int x, int y)
    {
        if(x >= 0 && x < gridSizeX && y >= 0 && y < gridSizeY)
        {
            if(grid[x,y].walkable)
                return true;
        }
        //print("not walkable " + x + " " + y);
        return false;
    }

    // PSO LAZY functions
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
    public Node LazyMove(int gridX, int gridY) // GetNodeFromGridPosition
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
    public Node GetNodeFromGridCoordinates(int[] xy)
    {
        int x = xy[0];
        int y = xy[1];

        if (xy[0] > gridSizeX)
            x = gridSizeX - 1;
        else if (xy[0] < 0)
            x = 0;

        if (xy[1] > gridSizeY)
            y = gridSizeY - 1;
        else if (xy[1] < 0)
            y = 0;

        return grid[x, y];
    }
}
