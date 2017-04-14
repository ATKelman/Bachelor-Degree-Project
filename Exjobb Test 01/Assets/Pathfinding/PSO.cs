using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

public class PSO : MonoBehaviour
{
    public GameObject gridManager;
    private MapGridGenerator grid;

    public int numberOfParticles = 10;
    [Range(0, 1)]
    public float c1 = 0.5f;
    [Range(0, 1)]
    public float c2 = 0.5f;
    public int epochCount = 10;
    public int speedCap = 5;
    public int startingVelocityRange = 3;
    public float diversityPenaltyMultiplier = 1.0f;

    public int dimensions = 2;

    private Node globalBestNode;
    private SwarmParticle globalBestParticle;
    private Node endNode;

    private int[] globalBestPosition;
    private float globalBestFitness;

    private SwarmParticle[] swarm;

    public bool shouldPrintTime = false;
    public Stopwatch timer;

    private bool firstCalculation = true;

    void Awake()
    {
        grid = gridManager.GetComponent<MapGridGenerator>();
        timer = new Stopwatch();     
    }

    public List<Node> FindPath(Vector3 startPos, Vector3 endPos)
    {
        timer.Reset();
        timer.Start();
        // Reset global best values
        globalBestPosition  = new int[dimensions];
        globalBestFitness   = float.MaxValue;

        // Get start and end node
        Node startNode  = grid.NodeFromWorldPoint(startPos);
        endNode         = grid.NodeFromWorldPoint(endPos);

        if (!startNode.walkable)
            print("ERROR - start node not walkabe");

        //Swarm initialization
        swarm = new SwarmParticle[numberOfParticles];
        for (int i = 0; i < swarm.Length; i++)
        {
            SwarmParticle instance = new SwarmParticle();
            instance.CreateParticle(startingVelocityRange, dimensions, i);

            // Set Starting Position
            for (int j = 0; j < instance.currentPos.Length; j++)
                instance.currentPos[j] = startNode.gridPosition[j];
            instance.currentPos.CopyTo(instance.previousPos, 0);
            instance.currentPos.CopyTo(instance.personalBestPos, 0);

            // Set Starting Node
            instance.currentNode = grid.GetNodeFromGridCoordinates(instance.currentPos);
            instance.AddPath(instance.currentNode);
            instance.previousNode = instance.currentNode;

            // Calculate Fitness
            float fitness = CalculateFitness(instance.currentNode, endNode);
            instance.currentFitness      = fitness;
            instance.totalFitness       += fitness;
            instance.personalBestFitness = fitness;

            // Check if best
            if(instance.currentFitness < globalBestFitness)
            {
                globalBestFitness = instance.currentFitness;
                instance.currentPos.CopyTo(globalBestPosition, 0);
            }

            swarm[i] = instance;
        }

        float[] newVel  = new float[dimensions];
        int[] newPos    = new int[dimensions];

        for(int epoch = 0; epoch < epochCount; epoch++)
        {
            for(int particle = 0; particle < swarm.Length; particle++)
            {
                SwarmParticle p = swarm[particle];

                // Velocity
                for(int i = 0; i < p.velocity.Length; i++)
                {
                    newVel[i] = p.velocity[i] +
                        c1 * (p.personalBestPos[i] - p.currentPos[i]) +
                        c2 * (globalBestPosition[i] - p.currentPos[i]);

                    if (newVel[i] > speedCap)
                        newVel[i] = speedCap;
                    else if (newVel[i] < -speedCap)
                        newVel[i] = -speedCap;
                }
                newVel.CopyTo(p.velocity, 0);

                // Position
                for (int j = 0; j < p.currentPos.Length; j++)
                    newPos[j] = p.currentPos[j] + (int)newVel[j];

                newPos.CopyTo(p.desiredPos, 0);

                MoveParticle(p);
            }      
        }

        float best          = float.MaxValue;
        List<Node> bestPath = new List<Node>();

        for(int i = 0; i < swarm.Length; i++)
        {
            if(swarm[i].totalFitness < best)
            {
                best = swarm[i].totalFitness;
                bestPath = swarm[i].path;
            }
        }
        timer.Stop();
        if (shouldPrintTime)
            print(timer.ElapsedMilliseconds);

        if (!firstCalculation)
            TextHandler.subsequentPathCalcTimes.Add(timer.ElapsedMilliseconds);
        else if(firstCalculation)
        {
            firstCalculation = false;
            TextHandler.initialPathCalcTime = timer.ElapsedMilliseconds;
        }

        return bestPath;
    }

    void MoveParticle(SwarmParticle p)
    {
        Node desiredNode = grid.GetNodeFromGridCoordinates(p.desiredPos);
        int randMoveChance = Random.Range(0, 11);
        if (!desiredNode.walkable )
        {
            List<Node> walkableNeighbours = new List<Node>();
            walkableNeighbours = grid.GetWalkableNeighbours(p.previousPos);

            if(walkableNeighbours.Count == 0)
            {
                print(p.particleID + " FALSE ");
                desiredNode = grid.GetNodeFromGridCoordinates(p.previousPos);
            }
            else
            {
                int rand = Random.Range(0, walkableNeighbours.Count);
                desiredNode = walkableNeighbours[rand];
                if (!desiredNode.walkable)
                    print("ERROR - walkable still false");
            }

            p.totalFitness += 100;  // pentalty 
        }

        int[] newVel = new int[dimensions];
        for (int i = 0; i < p.velocity.Length; i++)
            newVel[i] = desiredNode.gridPosition[i] - p.currentPos[i];

        newVel.CopyTo(p.velocity, 0);

        p.SetCurrentNode(desiredNode);
        p.AddPath(p.currentNode);

        p.currentPos.CopyTo(p.previousPos, 0);
        p.desiredPos.CopyTo(p.currentPos, 0);
        if (!p.currentNode.walkable)
            print("ERROR - current node not walkable");

        bool isDiverse = DetermineDiversity(p);

        ControlFitnessStatus(p, isDiverse);
    }

    bool DetermineDiversity(SwarmParticle currentParticle)
    {
        int xMin = currentParticle.currentPos[0] - 1;
        int xMax = currentParticle.currentPos[0] + 1;

        int yMin = currentParticle.currentPos[1] - 1;
        int yMax = currentParticle.currentPos[1] + 1;

        for (int i = 0; i < swarm.Length; i++)
        {
            SwarmParticle p = swarm[i];
            if (p.particleID == currentParticle.particleID)
                continue;

            if ((p.currentPos[0] >= xMin && p.currentPos[0] <= xMax)
                && (p.currentPos[1] >= yMin && p.currentPos[1] <= yMax))
            {
                return false;
            }
        }
        return true;
    }

    void MoveOtherNearbyParticles(SwarmParticle currentParticle)
    {
        int xMin = currentParticle.currentPos[0] - 2;
        int xMax = currentParticle.currentPos[0] + 2;

        int yMin = currentParticle.currentPos[1] - 2;
        int yMax = currentParticle.currentPos[1] + 2;

        for (int i = 0; i < swarm.Length; i++)
        {
            SwarmParticle p = swarm[i];
            if (p.particleID == currentParticle.particleID)
                continue;

            if ((p.currentPos[0] >= xMin && p.currentPos[0] <= xMax)
                && (p.currentPos[1] >= yMin && p.currentPos[1] <= yMax))
            {


                int[] newPos = new int[2];
                for (int j = 0; j < p.currentPos.Length; j++)
                {
                    newPos[j] = p.currentPos[j] + (int)currentParticle.velocity[j];
                }
                newPos.CopyTo(p.desiredPos, 0);
                MoveParticle(p);
            }
        }
    }

    void ControlFitnessStatus(SwarmParticle p)
    {
        float newFitness = CalculateFitness(p.currentNode, endNode);
        p.currentFitness = newFitness;
        p.totalFitness  += newFitness;

        if(newFitness < p.personalBestFitness)
        {
            p.currentPos.CopyTo(p.personalBestPos, 0);
            p.personalBestFitness = newFitness;
        }

        if(newFitness < globalBestFitness)
        {
            p.currentPos.CopyTo(globalBestPosition, 0);
            globalBestFitness = newFitness;
        }
    }
    void ControlFitnessStatus(SwarmParticle p, bool diverse)
    {
        float newFitness = CalculateFitness(p.currentNode, endNode);
        if (!diverse)
            newFitness *= diversityPenaltyMultiplier;
        p.currentFitness = newFitness;
        p.totalFitness  += newFitness;

        if (newFitness < p.personalBestFitness)
        {
            p.currentPos.CopyTo(p.personalBestPos, 0);
            p.personalBestFitness = newFitness;
        }

        if (newFitness < globalBestFitness)
        {
            p.currentPos.CopyTo(globalBestPosition, 0);
            globalBestFitness = newFitness;
        }
    }
    float CalculateFitness(Node current, Node target)
    {
        if(current.walkable)
        {
            float fitness = GetDistance(current, target);
            return fitness;
        }
        else
        {
            return 1000f;
        }

    }
    float GetDistance(Node current, Node target)
    {
        int distX = Mathf.Abs(current.gridX - target.gridX);
        int distY = Mathf.Abs(current.gridY - target.gridY);

        if (distX > distY)
            return 1.4f * distY + 1.0f * (distX - distY);

        return 1.4f * distX + 1.0f * (distY - distX);
    }

    public struct SwarmParticle
    {
        public List<Node> path;
        public Node currentNode;
        public Node previousNode;
        public Node personalBestNode;

        public int[] currentPos;
        public int[] previousPos;
        public int[] desiredPos;
        public int[] personalBestPos;

        public float[] velocity;

        public float currentFitness;        // Current Fitness for Position
        public float totalFitness;          // Total Combined Fitness for Entire Run
        public float personalBestFitness;

        public int particleID;

        public void CreateParticle(int velocityRange, int dimensions, int id)
        {
            path = new List<Node>();

            particleID = id;

            currentPos      = new int[dimensions];
            previousPos     = new int[dimensions];
            desiredPos      = new int[dimensions];
            personalBestPos = new int[dimensions];

            velocity = new float[dimensions];
            for (int i = 0; i < velocity.Length; i++)
                velocity[i] = Random.Range(-velocityRange, (velocityRange + 1));
        }

        public void AddPath(Node n)
        {
            path.Add(n);
        }

        public void SetCurrentNode(Node n)
        {
            previousNode = currentNode;
            currentNode = n;
        }
    }
}
