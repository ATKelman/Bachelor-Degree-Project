using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

public class ParticleSwarm : MonoBehaviour
{
    public Transform target;
    public GameObject gridManager;
    public int numberOfParticles = 10;

    [Range(0, 1)]
    public float c1 = 0.5f;
    [Range(0, 1)]
    public float c2 = 0.5f;
    public int epochCount = 10;
    public int speedCap = 5;
    public int startingVelocityRange = 3;

    public Node gbNode;
    public Par gbPar;

    public int[] globalB;
    public float globalBFitness;

    MapGridGenerator grid;

    private Node targetNode;
    private Par[] swarm;

    public Stopwatch timer;

    void Awake()
    {
        grid = gridManager.GetComponent<MapGridGenerator>();
        timer = new Stopwatch();

        // Initialize swarm
        swarm = new Par[numberOfParticles];
        for (int i = 0; i < swarm.Length; i++)
        {
            Par instance = new Par();
            instance.CreatePart(startingVelocityRange);
            instance.id = i;

            swarm[i] = instance;
        }
    }

    //void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Space))
    //        ParticleSwarmOptimization(seeker.position, target.position);
    //}

    public List<Node> FindPath(Vector3 startPos, Vector3 endPos)
    {
        timer.Reset();
        timer.Start();
        globalB = new int[2];

        globalBFitness = float.MaxValue;

        Node startNode = grid.NodeFromWorldPoint(startPos);
        targetNode = grid.NodeFromWorldPoint(endPos);

        int[] targetPos = new int[2];
        targetPos[0] = targetNode.gridX;
        targetPos[1] = targetNode.gridY;

        // Swarm set up
        for(int i = 0; i < swarm.Length; i++)
        {
            Par instance = swarm[i];

            // Set starting position
            instance.currentPos[0] = startNode.gridX;
            instance.currentPos[1] = startNode.gridY;

            int[] desiredStartPos = new int[instance.currentPos.Length];
            for (int j = 0; j < instance.currentPos.Length; j++)
                desiredStartPos[j] = instance.currentPos[j] + (int)instance.velocity[j];

            int[] actualStartPos = PathForwards(instance.currentPos, desiredStartPos, ref instance);
            actualStartPos.CopyTo(instance.currentPos, 0);
            instance.currentPos.CopyTo(instance.previousPos, 0);

            // Set starting Node
            instance.current = grid.LazyMove(instance.currentPos[0], instance.currentPos[1]);
            instance.path.Add(instance.current);
            instance.previous = instance.current;

            // Calculate fitness
            float fitness = CalculateFitness(instance);
            instance.fitness = fitness;
            instance.personalBestFitness = fitness;

            // check if best
            if (instance.fitness < globalBFitness)
            {
                globalBFitness = instance.fitness;
                instance.currentPos.CopyTo(globalB, 0);
            }
        }

        float[] newVel = new float[2];
        int[] newPos = new int[2];

        for (int epoch = 0; epoch < epochCount; epoch++)
        {
            for (int i = 0; i < swarm.Length; i++)
            {
                Par p = swarm[i];

                // Velocity
                for (int j = 0; j < p.velocity.Length; j++)
                {
                    //if (p.crashedWall)
                    //{
                        
                    //    //newVel[j] = p.velocity[j];
                    //    //newVel[0] = Random.Range(-2, 3);
                    //    //newVel[1] = Random.Range(-2, 3);
                    //}
                    //else
                    //{
                    //    newVel[j] = p.velocity[j] +
                    //         c1 * (p.personalBestPos[j] - p.currentPos[j]) +
                    //         c2 * (globalB[j] - p.currentPos[j]);
                    //}

                    newVel[j] = p.velocity[j] +
                         c1 * (p.personalBestPos[j] - p.currentPos[j]) +
                         c2 * (globalB[j] - p.currentPos[j]);

                    if (newVel[j] > speedCap)
                        newVel[j] = speedCap;
                    else if (newVel[j] < -speedCap)
                        newVel[j] = -speedCap;
                }
                //if(p.crashedWall)
                //{
                //    Vector2 vel = Reflect(p);
                //    newVel[0] = vel.x;
                //    newVel[1] = vel.y;
                //    print("crash true reflecting " + p.id);
                //}

                //if(newVel[0] == 0 && newVel[1] == 0)
                //{
                //    newVel[0] = Random.Range(-2, 3);
                //    newVel[1] = Random.Range(-2, 3);
                //}
                newVel.CopyTo(p.velocity, 0);
                p.crashedWall = false;

                // Position
                for (int j = 0; j < p.currentPos.Length; j++)
                {
                    newPos[j] = p.currentPos[j] + (int)newVel[j];
                }
                newPos.CopyTo(p.desiredPos, 0);

                // Move others
                //MoveOther(p);              
            }
            // Loop to actually move particles
            for(int i = 0; i < swarm.Length; i++)
            {
                Par p = swarm[i];

                // Check path
                int[] pathForward = new int[2];
                pathForward = PathForwards(p.currentPos, p.desiredPos, ref p);

                // Change to pathforward to current
                pathForward.CopyTo(p.currentPos, 0);

                Node newNode = grid.LazyMove(p.currentPos[0], p.currentPos[1]);
                p.current = newNode;

                //if (p.crashedWall)
                //{
                //    Vector2 reflect = Reflect(p);
                //    p.velocity[0] = reflect.x;
                //    p.velocity[1] = reflect.y;
                //}

                // Fitness
                BestPBandGB(p, p.currentPos);

                p.currentPos.CopyTo(p.previousPos, 0);
            }
        }

        float best = float.MaxValue;
        List<Node> bestPath = new List<Node>();
        for(int i = 0; i < swarm.Length; i++)
        {
            if(swarm[i].fitness < best)
            {
                best = swarm[i].fitness;
                bestPath = swarm[i].path;
            }
        }
        timer.Stop();

        print("done " + timer.Elapsed);

        return bestPath;
    }

    Vector2 Reflect(Par p)
    {
        Vector2 inDirection   = new Vector2(p.velocity[0], p.velocity[1]);
        Vector2 inNormal      = new Vector2(p.velocity[1], -p.velocity[0]);
        Vector2 reflection    =  2.0f * Vector2.Dot(inDirection, inNormal) * inNormal - inDirection;

        return reflection;
    }

    void MoveOther(Par currentP)
    {
        int xMin = currentP.desiredPos[0] - 3;
        int xMax = currentP.desiredPos[0] + 3;

        int yMin = currentP.desiredPos[1] - 3;
        int yMax = currentP.desiredPos[1] + 3;

        for (int i = 0; i < swarm.Length; i++)
        {
            Par p = swarm[i];
            if (p.id == currentP.id)
                continue;

            if ((p.desiredPos[0] >= xMin && p.desiredPos[0] <= xMax)
                && (p.desiredPos[1] >= yMin && p.desiredPos[1] <= yMax))
            {
                int[] newPos = new int[2];
                for (int j = 0; j < p.desiredPos.Length; j++)
                {
                    newPos[j] = p.desiredPos[j] + (int)currentP.velocity[j];
                }
                newPos.CopyTo(p.desiredPos, 0);
            }
        }
    }

    int[] PathForwards(int[] startPos, int[] endPos, ref Par p)
    {
        int[] posChecker = new int[startPos.Length];
        int[] newEndingPos = new int[startPos.Length];
        startPos.CopyTo(posChecker, 0);
        startPos.CopyTo(newEndingPos, 0);

        while(true)
        {
            if (posChecker[0] == endPos[0] && posChecker[1] == endPos[1])
                break;

            for(int i = 0; i < posChecker.Length; i++)
            {
                if (posChecker[i] > endPos[i])
                    posChecker[i]--;
                else if (posChecker[i] < endPos[i])
                    posChecker[i]++;
            }

            //Node desiredNode = grid.LazyMove(posChecker[0], posChecker[1]);
            //if(travelled.Contains(desiredNode))
            //{
            //    p.crashedWall = true;
            //    break;
            //}

            // If walkable, move to new position + add to path
            if (grid.IsWalkable(posChecker[0], posChecker[1]))
            {
                posChecker.CopyTo(newEndingPos, 0);
                p.AddPath(grid.LazyMove(posChecker[0], posChecker[1]));
            }
            else
            {
                p.crashedWall = true;
                break;
            }
        }
        return newEndingPos;
    }

    void BestPBandGB(Par p, int[] newPos)
    {
        float newFitness = CalculateFitness(p);
        p.fitness = newFitness;

        if (newFitness < p.personalBestFitness)
        {
            newPos.CopyTo(p.personalBestPos, 0);
            p.personalBestFitness = newFitness;
        }

        if (newFitness < globalBFitness)
        {
            newPos.CopyTo(globalB, 0);
            globalBFitness = newFitness;
        }
    }

    float CalculateFitness(Par p)
    {
        float fitness =  GetDistance(p.current, targetNode);
        if(p.crashedWall)
            fitness *= 2.0f;

        return fitness;
    }

    float GetDistance(Node a, Node b)
    {
        int distX = Mathf.Abs(a.gridX - b.gridX);
        int distY = Mathf.Abs(a.gridY - b.gridY);

        if (distX > distY)
            return 1.4f * distY + 1.0f * (distX - distY);

        return 1.4f * distX + 1.0f * (distY - distX);
    }

    public struct Par
    {
        public List<Node> path;
        public Node current;
        public Node previous;
        public Node pb;

        public int[] currentPos;
        public int[] previousPos;
        public int[] desiredPos;
        public int[] personalBestPos;

        public float[] velocity;

        public float fitness;
        public float personalBestFitness;

        public bool crashedWall;

        public int id;

        public void CreatePart(int velRange)
        {
            id = -1;
            path = new List<Node>();

            crashedWall = false;

            currentPos = new int[2];
            previousPos = new int[2];
            desiredPos = new int[2];
            personalBestPos = new int[2];

            velocity = new float[2];
            for(int i = 0; i < velocity.Length; i++)
                velocity[i] = Random.Range(-velRange, (velRange + 1));
            print("random vel " + velocity[0] + " " + velocity[1]);
        }

        public void AddPath(Node n)
        {
            path.Add(n);
        }
    }
}
