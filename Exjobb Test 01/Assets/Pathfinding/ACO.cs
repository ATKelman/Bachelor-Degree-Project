using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

public class ACO : MonoBehaviour
{
    public Transform seeker, target;
    public GameObject gridManager;
    public int NumberOfAnts = 1;

    private float Fgb = 0f;

    [Range(0,1)]
    public float alpha = 1.0f;
    [Range(0, 1)]
    public float beta = 1.0f;
    [Range(0, 1)]
    public float q0 = 0.1f;
    [Range(0, 1)]
    public float pheromoneDecay = 0.1f;
    public bool shouldPrintTime = false;

    //public float pheromoneCap = 1.0f;
    public int epochCount = 1;

    MapGridGenerator grid;

    public Stopwatch timer;
    private bool firstCalculation = true;

    void Awake()
    {
        grid = gridManager.GetComponent<MapGridGenerator>();
        timer = new Stopwatch();
    }

    public List<Node> FindPath(List<Node> alreadyTravelled)
    {
        timer.Reset();
        timer.Start();

        Node startNode = grid.NodeFromWorldPoint(seeker.position);
        Node targetNode = grid.NodeFromWorldPoint(target.position);

        List<Ant> incompletePaths = new List<Ant>();
        List<Ant> completeAnts = new List<Ant>();

        // Create ants
        List<Ant> ants = new List<Ant>();
        for (int ant = 0; ant < NumberOfAnts; ant++)
        {
            Ant currentAnt = new Ant();
            ants.Add(currentAnt);
        }

        // While criteria not met
        for (int epoch = 0; epoch < epochCount; epoch++)
        {
            // Position at start
            foreach (Ant a in ants)
            {
                a.ClearAnt(seeker.position, startNode.gridX, startNode.gridY, startNode, startNode);

                incompletePaths.Add(a);
                a.reachedTarget = false;
                a.path.Add(startNode);
            }

            // While each ant has yet to compelte a path
            while (incompletePaths.Count > 0)
            {
                foreach(Ant a in incompletePaths)
                {
                    // Choose next position 
                    a.previousNode = a.currentNode;
                    Node temp = ChooseNextNode(a.previousNode, targetNode, a, alreadyTravelled);
                    if (temp == null)
                    {
                        a.fitness = float.MaxValue;
                        a.pathCompelte = true;
                        completeAnts.Add(a);
                        continue;
                    }

                    a.currentNode = temp;
                    a.fitness += GetDistance(a.previousNode, a.currentNode);
                    a.path.Add(a.currentNode);
                    
                    if(a.currentNode == targetNode)
                    {
                        a.pathCompelte = true;
                        a.fitness *= 0.5f;
                        completeAnts.Add(a);
                    }

                    // Apply step by step pheromone update
                    LocateUpdate(a.currentNode);

                }
                // Remove complete ants from incompetePaths
                if(completeAnts.Count > 0)
                {
                    foreach (Ant cAnt in completeAnts)
                    {
                        incompletePaths.Remove(cAnt);
                    }
                    completeAnts.Clear();
                }

            }

            // Apply offline pheromone update
            Ant bestAnt = null;
            foreach (Ant a in ants)
            {
                if (bestAnt == null || a.fitness < bestAnt.fitness)
                    bestAnt = a;
            }
            Fgb = bestAnt.fitness;

            foreach (Node n in bestAnt.path)
                GlobalUpdate(n);
        }
        timer.Stop();
        if(shouldPrintTime)
            print(timer.Elapsed);

        if (!firstCalculation)
            TextHandler.subsequentPathCalcTimes.Add(timer.ElapsedMilliseconds);
        else if(firstCalculation)
        {
            firstCalculation = false;
            TextHandler.initialPathCalcTime = timer.ElapsedMilliseconds;
        }

        Ant bestPath = null;
        foreach (Ant a in ants)
        {
            if (bestPath == null || a.fitness < bestPath.fitness)
                bestPath = a;
        }

        ants.Clear();
        incompletePaths.Clear();
        completeAnts.Clear();

        return bestPath.path;
    }

    Node ChooseNextNode(Node currentNode, Node targetNode, Ant a, List<Node> travelled)
    {
        Node mostAttractive = null;
        float maxPheromone = 0;

        foreach(Node neighbour in grid.GetNeighbours(currentNode))
        {
            if (neighbour == null)
                continue;
            // ensure neighbour is walkable
            if (!neighbour.walkable)
                continue;
            if (a.path.Contains(neighbour))
                continue;
            //if (travelled.Contains(neighbour))
            //    continue;
            if (neighbour == targetNode)
            {
                mostAttractive = neighbour;
                //a.reachedTarget = true;
                break;
            }
            if (neighbour.pheromone > maxPheromone)
                maxPheromone = neighbour.pheromone;

            float dist  = GetDistance(neighbour, targetNode);
            float nIJ   = 1 / dist;
            float tIJ   = neighbour.pheromone;
            float tImax = tIJ / maxPheromone;

            // attractiveness == q
            float attractiveness = 
               Mathf.Pow(nIJ, beta) * Mathf.Pow(tImax, alpha);

            // Exploration vs Explotation
            if (mostAttractive == null || 
                (mostAttractive.attractiveness < attractiveness && attractiveness > q0))
            {
                mostAttractive = neighbour;
                mostAttractive.attractiveness = attractiveness;
            }
            else if(attractiveness < q0 && mostAttractive.attractiveness < attractiveness)
            {
                mostAttractive = grid.GetRandomNeighbour(currentNode);
                mostAttractive.attractiveness = attractiveness;
            }

        }
        return mostAttractive;
    }

    float GetDistance(Node nodeA, Node nodeB)
    {
        if(nodeA == null || nodeB == null)
            return float.MaxValue;
        int distX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int distY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if (distX > distY)
            return 1.4f * distY + 1.0f * (distX - distY);

        return 1.4f * distX + 1.0f * (distY - distX);
    }

    void LocateUpdate(Node node)
    {
        if (node == null)
            return;
        // tij = (1 - decay) * tij + decay * initial 
        float newPheromone = 1 - pheromoneDecay;
        newPheromone *= node.pheromone;
        float initialStep = node.initialPhermone * pheromoneDecay;
        float pheromoneUpdate = newPheromone + initialStep;
        node.pheromone = pheromoneUpdate;
    }

    void GlobalUpdate(Node node)
    {
        if (node == null)
            return;
        // tij = (1 - decay) * tij + decay * delta tij
        // delta tij = 1 / Fgb, Fgb = fitness value
        float pheromoneUpdate = 1 - pheromoneDecay;
        pheromoneUpdate *= node.pheromone;
        float detlaTij = 1 / Fgb;
        float pheromoneUpdate2 = pheromoneDecay * detlaTij;
        float pheromoneUpdate3 = pheromoneUpdate + pheromoneUpdate2;
        node.pheromone += pheromoneUpdate3;
    }

    class Ant
    {
        public Vector3 worldPosition;
        public int gridX;
        public int gridY;
        public List<Node> path;
        public Node previousNode = null;
        public Node currentNode = null;

        public float fitness;

        public bool pathCompelte = false;
        public bool reachedTarget = false;

        public Ant()
        {
            path = new List<Node>();
            fitness = 0f;
        }

        public void ClearAnt(Vector3 pos, int x, int y, Node start, Node prev)
        {
            worldPosition = pos;
            gridX = x;
            gridY = y;
            path.Clear();
            pathCompelte = false;
            currentNode = start;
            previousNode = prev;
            fitness = 0f;
        }
    }
}
