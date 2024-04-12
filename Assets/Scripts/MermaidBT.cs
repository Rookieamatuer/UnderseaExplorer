using NPBehave;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using UnityEngine.UIElements;

public class MermaidBT : MonoBehaviour
{
    private Root tree;
    private Blackboard blackboard;

    private int[,] map;
    private int mapWidth;
    private int mapHeight;

    // Movement
    private CharacterController characterController;
    private bool escape;
    [SerializeField]private GameObject diver;
    private Vector3 currentDirection;
    private int timer = 0;

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
        currentDirection = Vector3.forward;
        CreateTree(MermaidBehaviour());
    }

    private void Update()
    {
        if (isDebugging)
        {
            escape = true;
            isDebugging = false;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Diver"))
        {
            SetColor(Color.red);
            diver = other.gameObject;
            escape = true;
        }
    }

    private void OnDestroy()
    {
        // delete reference
        tree.Stop();
        tree = null;
        blackboard = null;
        characterController = null;
        diver = null;
    }

    private void CreateTree(Root t)
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

    private Root MermaidBehaviour()
    {
        Node idle = WanderBehaviour();
        Node escape = EscapeFromDiver();
        Node bb = new BlackboardCondition("Escape",
                                           Operator.IS_EQUAL, true,
                                           Stops.IMMEDIATE_RESTART,
                                           escape);
        Node selector = new Selector(bb, idle);
        Node service = new Service(0.2f, EnvironmentDetect, selector);
        return new Root(service);
    }

    private Node EscapeFromDiver()
    {
        //Debug.Log("escaping");
        Node move = new Action(() => Move(speed));
        Node detect = new Action(() => FleeDirection());
        Node bb = new BlackboardCondition("Reef",
                                            Operator.IS_EQUAL, false,
                                            Stops.IMMEDIATE_RESTART,
                                            move);
        Node disappear = new Action(() => Disappear());
        Node selector = new Selector(bb, disappear);
        Node service = new Service(0.2f, EnvironmentDetect, selector);
        Node seq = new Sequence(detect, service);
        return seq;
    }

    private Node WanderBehaviour()
    {
        Node turn = new Action(() => ChangeDirection());
        Node bb = new BlackboardCondition("Reef",
                                            Operator.IS_EQUAL, true,
                                            Stops.IMMEDIATE_RESTART,
                                            turn);
        Node wander = new Action(() => Wander());
        Node selector = new Selector(bb, wander);
        return selector;
    }

    private void Wander()
    {
        timer++;
        if (timer > 10)
        {
            currentDirection = Quaternion.Euler(0, 90, 0) * currentDirection;
            timer = 0;
        }
        Move(speed / 5);
        //flag = !flag;

    }

    private void Move(float spd)
    {
        characterController.Move(currentDirection * spd * Time.deltaTime);
    }

    private void FleeDirection()
    {
        currentDirection = new Vector3(transform.position.x - diver.transform.position.x, 0, transform.position.z - diver.transform.position.z).normalized;
    }

    private void ChangeDirection()
    {
        Move(0);

        int direction = UnityEngine.Random.Range(0, 8);
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
    }

    private void Disappear()
    {
        escape = false;
        gameObject.SetActive(false);
        Debug.Log("disappear");
        UnderseaReefGenerator.instance.RespawnMermaid();
        Destroy(gameObject);

        
    }


    private void EnvironmentDetect()
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(transform.position.x + currentDirection.x) + mapWidth / 2, 0, mapWidth - 1);
        int z = Mathf.Clamp(Mathf.RoundToInt(transform.position.z + currentDirection.z) + mapHeight / 2, 0, mapHeight - 1);
        blackboard["Reef"] = (map[x, z] == 1);
        blackboard["Escape"] = escape;
    }

    private void SetColor(Color color)
    {
        GetComponent<MeshRenderer>().material.SetColor("_Color", color);
    }
}
