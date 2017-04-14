using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GizmoDrawer : MonoBehaviour
{
    public int gridX;
    public int gridY;
    public float nodeDiameter;
    public Node[,] grid;
    public List<Node> path;

    public void SetValues(int x, int y, Node[,] g, List<Node> p, float d)
    {
        gridX = x;
        gridY = y;
        grid = g;
        path = p;
        nodeDiameter = d;
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridX, gridY, 1));
        if (grid != null)
        {
            foreach (Node n in grid)
            {
                Gizmos.color = (n.walkable) ? Color.white : Color.red;
                if (path != null)
                {
                    if (path.Contains(n))
                    {
                        Gizmos.color = Color.black;
                    }
                }
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - 0.1f));
            }
        }
    }
}
