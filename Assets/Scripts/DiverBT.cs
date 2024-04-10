using NPBehave;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class DiverBT : MonoBehaviour
{
    private Root tree;                  
    private Blackboard blackboard;

    private int[,] map;
    private int mapWidth;
    private int mapHeight;

    [SerializeField]private Vector3 targetTreasurePos;
    private Vector2Int targetTreasureIndex;
    private bool detectTreasure;

    // Utility
    private const int IDLE = 0;
    private const int Explore = 1;
    private const int CHASE = 2;
    private int currentAction;
    List<int> utilityScores;


    // Movement
    private CharacterController characterController;
    private List<int> unMoveDirection;
    private Vector3 currentDirection;

    [Header("Debugging")]
    public bool isDebugging = false;
    public int actionNum;
    public float speed = 10;

    private void Awake()
    {
        map = UnderseaReefGenerator.instance.GetMapGrid();
        mapWidth = map.GetLength(0);
        mapHeight = map.GetLength(1);
    }

    private void Start()
    {
        characterController = gameObject.GetComponent<CharacterController>();
        currentDirection = ChangeDirection();

        currentAction = Explore;
        SwitchTree(SelectBehaviourTree(currentAction));

        utilityScores = new List<int>();
        utilityScores.Add(0); // Idle
        utilityScores.Add(0); // Explore
        utilityScores.Add(0); // Chase
    }

    private void Update()
    {
        if (isDebugging)
        {
            
            SetColor(Color.black);
            isDebugging = false;
        }

        // credit from lab
        updateScores();
        int maxValue = utilityScores.Max(t => t);
        int maxIndex = utilityScores.IndexOf(maxValue);

        if (currentAction != maxIndex)
        {
            currentAction = maxIndex;
            SwitchTree(SelectBehaviourTree(currentAction));
        }
        if (targetTreasurePos == Vector3.zero)
        {
            Debug.Log("no target");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Treasure"))
        {
            Debug.Log("find");

            //taretTreasure = other.gameObject;
            //detectTreasure = true;
        }
    }

    private void OnDestroy()
    {
        // 清理引用，防止内存泄漏
        tree.Stop();
        tree = null;
        blackboard = null;
        characterController = null;
    }

    private void updateScores()
    {
        utilityScores[Explore] = 10;
        float targetDistance = DetectNearestTreasure();
        if (targetDistance < 30 && targetDistance > 1)
        {
            utilityScores[CHASE] = (int)(200 / targetDistance);
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
        Node sel = new Selector(TurnBehaviour(),
                                MoveBehaviour());
        Node service = new Service(0.2f, EnvironmentDetect, sel);
        return new Root(service);
    }

    private Root ChaseBehaviour()
    {
        return new Root(new Action(() => Chase()));
    }

    private Node TurnBehaviour()
    {
        Node turn = new Action(() => ChangeDirection());
        Node bb = new BlackboardCondition("Reef",
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

    /**
     * Actions
     */
    private void Move(float spd)
    {
        characterController.Move(currentDirection * spd * Time.deltaTime);
    }

    private Vector3 ChangeDirection()
    {
        Move(0);
        int direction = UnityEngine.Random.Range(0, 4);
        switch (direction)
        {
            case 0:
                currentDirection = Vector3.forward;
                return Vector3.forward;
            case 1:
                currentDirection = Vector3.right;
                return Vector3.right;
            case 2:
                currentDirection = Vector3.back;
                return Vector3.back;
            case 3:
                currentDirection = Vector3.left;
                return Vector3.left;
            default:
                currentDirection = Vector3.zero;
                return Vector3.zero;
        }
    }

    private void EnvironmentDetect()
    {
        // avoid out of boundary
        int x = Mathf.Clamp(Mathf.RoundToInt(transform.position.x + 2 * currentDirection.x) + mapWidth / 2, 0, mapWidth - 1);
        int z = Mathf.Clamp(Mathf.RoundToInt(transform.position.z + 2 * currentDirection.z) + mapHeight / 2, 0, mapHeight - 1);
        blackboard["Reef"] = (map[x, z] == 1);
        blackboard["Treasure"] = (map[x, z] == 2);
    }

    private void Chase()
    {
        
        PathFinding();
        currentDirection = (targetTreasurePos - transform.position).normalized;
        Move(speed);
        //Debug.Log("Chase");
    }

    private void PathFinding()
    {
        Vector2Int currentIndex = new Vector2Int(Mathf.RoundToInt(transform.position.x + mapWidth / 2), Mathf.RoundToInt(transform.position.y + mapHeight / 2));
        AStarPathfinder pathFinder = new AStarPathfinder(currentIndex, targetTreasureIndex);
        List<Vector2Int> path = pathFinder.FindPath();
        if (path != null)
        {
            foreach (Vector2Int step in path)
            {
                if (step != currentIndex &&
                        step != targetTreasureIndex)
                {
                    float xPos = step.x - (mapWidth / 2);
                    float zPos = step.y - (mapHeight / 2);
                    characterController.Move(new Vector3(xPos, transform.position.y, zPos) - transform.position);
                }
            }
        }
    }

    

    private void Idle()
    {
        Debug.Log("Idle");
    }

    private void SetColor(Color color)
    {
        GetComponent<MeshRenderer>().material.SetColor("_Color", color);
    }

    private float DetectNearestTreasure()
    {
        float distance = mapWidth * mapHeight;
        foreach (var t in UnderseaReefGenerator.instance.GetTreasurePos())
        {
            Vector3 treasurePos = new Vector3(t.GetX() - mapWidth / 2, 0, t.GetY() - mapHeight / 2);
            float tmp = (treasurePos - transform.position).magnitude;
            if (distance > tmp)
            {
                distance = tmp;
                targetTreasurePos = treasurePos;
                targetTreasureIndex = new Vector2Int(t.GetX(), t.GetY());
            }
        }

        return distance;
    }

}
