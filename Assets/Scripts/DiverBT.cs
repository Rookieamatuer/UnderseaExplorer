using NPBehave;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class DiverBT : MonoBehaviour
{
    enum Directions
    {
        Forward,
        Right,
        Back,
        Left,
        ForwardLeft,
        ForwardRight,
        BackRight,
        BackLeft
    }
    private Root tree;                  
    private Blackboard blackboard;

    private int[,] map;
    private int mapWidth;
    private int mapHeight;

    // mermaid
    private GameObject[] mermaidList;
    private int targetMermaidIndex;
    private float detectMermaidDistance;


    // Utility
    private const int Explore = 0;
    private const int CHASE = 1;
    private int currentAction;
    private Directions directionIndex;
    List<int> utilityScores;


    // Movement
    private CharacterController characterController;
    private Vector3 currentDirection;
    private bool randomTurn = false;
    private int timer = 0;

    [Header("Debugging")]
    public bool isDebugging = false;
    public int actionNum;
    public float speed = 10;
    public GameObject diver;

    private void Awake()
    {
        map = UnderseaReefGenerator.instance.GetMapGrid();
        mapWidth = map.GetLength(0);
        mapHeight = map.GetLength(1);
    }

    private void Start()
    {
        characterController = gameObject.GetComponent<CharacterController>();

        directionIndex = Directions.Forward;
        ChangeDirection();

        detectMermaidDistance = DetectNearestMermaid();

        currentAction = Explore;
        SwitchTree(SelectBehaviourTree(currentAction));

        utilityScores = new List<int>();
        utilityScores.Add(0); // Explore
        utilityScores.Add(0); // Chase
    }

    private void Update()
    {
        if (isDebugging)
        {
            
            SetColor(Color.white);
            isDebugging = false;
        }

        updateScores();
        int maxValue = utilityScores.Max(t => t);
        int maxIndex = utilityScores.IndexOf(maxValue);

        if (currentAction != maxIndex)
        {
            currentAction = maxIndex;
            SwitchTree(SelectBehaviourTree(currentAction));
        }
    }

    private void OnDestroy()
    {
        // prevent memory leak
        tree.Stop();
        tree = null;
        blackboard = null;
        characterController = null;
    }

    private void updateScores()
    {
        utilityScores[Explore] = 10;
        detectMermaidDistance = DetectNearestMermaid();
        if (detectMermaidDistance < 30 && detectMermaidDistance > 1)
        {
            utilityScores[CHASE] = (int)(200 / detectMermaidDistance);
        }
        else
            utilityScores[CHASE] = 5;
    }

    private void SwitchTree(Root t)
    {
        if (tree != null) tree.Stop();

        tree = t;
        blackboard = tree.Blackboard;
#if UNITY_EDITOR
        Debugger debugger = (Debugger)this.gameObject.AddComponent(typeof(Debugger));
        debugger.BehaviorTree = tree;
#endif

        tree.Start();
    }

    private Root SelectBehaviourTree(int action)
    {
        switch (action)
        {
            case Explore:
                return ExploreBehaviour();

            case CHASE:
                return ChaseBehaviour();

            default:
                return new Root(new Action(() =>Idle()));
        }
    }

    /**
     * Behaviour Tree
     */


    private Root ExploreBehaviour()
    {
        Node seq = new NPBehave.Sequence(RandomTurnBehaviour(),
                                MoveBehaviour());
        Node sel = new Selector(BoundaryBehaviour(),
                                seq
                                );
        Node service = new Service(0.2f, EnvironmentDetect, sel);
        return new Root(service);
    }

    private Root ChaseBehaviour()
    {
        return new Root(new Action(() => Chase()));
    }

    private Node BoundaryBehaviour()
    {
        Node turn = new Action(() => ChangeDirection());
        Node bb = new BlackboardCondition("Boundary",
                                            Operator.IS_EQUAL, true,
                                            Stops.IMMEDIATE_RESTART,
                                            turn);
        return bb;
    }
    

    private Node MoveBehaviour()
    {
        Node move = new Action(() => Move(speed));
        return move;
    }

    private Node RandomTurnBehaviour()
    {
        Node randTurn = new Action(() => RandTurn());
        return randTurn;
    }

    /**
     * Actions
     */
    private void Move(float spd)
    {
        characterController.Move(currentDirection * spd * Time.deltaTime);
    }

    private void RandTurn()
    {
        timer++;
        if (timer > 50)
        {
            ChangeDirection();
            timer = 0;
        }
    }

    private void ChangeDirection()
    {
        Move(0);
        
        int direction = UnityEngine.Random.Range(0, 8);
        if (direction == (int)directionIndex)
        {
            direction = UnityEngine.Random.Range(0, 8);
        }
        switch (direction)
        {
            case 0:
                currentDirection = Vector3.forward;
                break;
            case 1:
                currentDirection = Vector3.right;
                break;
            case 2:
                currentDirection = Vector3.back;
                break;
            case 3:
                currentDirection = Vector3.left;
                break;
            case 4:
                currentDirection = Vector3.forward + Vector3.left;
                break;
            case 5:
                currentDirection = Vector3.forward + Vector3.right;
                break;
            case 6:
                currentDirection = Vector3.back + Vector3.right;
                break;
            case 7:
                currentDirection = Vector3.back + Vector3.left;
                break;
            default:
                currentDirection = Vector3.zero;
                break;
        }
        directionIndex = (Directions)direction;
    }

    private void EnvironmentDetect()
    {
        // avoid out of boundary
        int x = Mathf.Clamp(Mathf.RoundToInt(transform.position.x + 2 * currentDirection.x) + mapWidth / 2, 0, mapWidth - 1);
        int z = Mathf.Clamp(Mathf.RoundToInt(transform.position.z + 2 * currentDirection.z) + mapHeight / 2, 0, mapHeight - 1);
        blackboard["Boundary"] = (x == 0 || z == 0 || x == mapWidth - 1 || z == mapHeight - 1);
        blackboard["Reef"] = (map[x, z] == 1);
        blackboard["RandomTurn"] = randomTurn;
    }

    private void Chase()
    {
        if (mermaidList[targetMermaidIndex] != null)
        currentDirection = (mermaidList[targetMermaidIndex].transform.position - transform.position).normalized;
        Move(speed);
    }


    private void Idle()
    {
        SetColor(Color.white);
        Move(0);
        Debug.Log("Idle");
    }

    private void SetColor(Color color)
    {
        GetComponent<MeshRenderer>().material.SetColor("_Color", color);
    }


    private float DetectNearestMermaid()
    {
        float distance = mapWidth * mapHeight;
        
        int index = 0;
        mermaidList = GameObject.FindGameObjectsWithTag("Mermaid");
        foreach (var m in mermaidList)
        {
            float tmp = (m.transform.position - transform.position).magnitude;
            if (distance > tmp)
            {
                distance = tmp;
                targetMermaidIndex = index;
            }
            ++index;
        }

        return distance;
    }

}
