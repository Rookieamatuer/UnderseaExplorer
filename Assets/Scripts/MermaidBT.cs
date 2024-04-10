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
    private int treasure;

    // Movement
    private CharacterController characterController;
    private bool escape;
    [SerializeField]private GameObject diver;
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
        CheckRespawnPos();
        SwitchTree(SelectBehaviourTree(actionNum));
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
            case 0:
                return EscapeBehaviour();

            default:
                return new Root(new Action(() => Idle()));
        }
    }

    private Root EscapeBehaviour()
    {
        Node idle = new Action(() => Idle());
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
        Debug.Log("escaping");
        Node move = new Action(() => Move(speed));
        Node detect = new Action(() => ChangeDirection());
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

    private void Idle()
    {
        Move(0);
    }

    private void Move(float spd)
    {
        characterController.Move(currentDirection * spd * Time.deltaTime);
    }

    private void ChangeDirection()
    {
        currentDirection = new Vector3(transform.position.x - diver.transform.position.x, 0, transform.position.z - diver.transform.position.z).normalized;
    }

    private void Disappear()
    {
        escape = false;
        gameObject.SetActive(false);
        Debug.Log("disappear");
        UnderseaReefGenerator.instance.SpawnMermaid(true, treasure);
        Destroy(gameObject);
    }

    private void CheckRespawnPos()
    {
        if (UnderseaReefGenerator.instance.GetTreasurePos() != null)
        {
            float distance = mapWidth * mapHeight;
            int i = 0;
            Vector3 pos = Vector3.zero;
            foreach(var t in UnderseaReefGenerator.instance.GetTreasurePos())
            {
                Vector3 treasurePos = new Vector3(t.GetX() - mapWidth / 2, 0, t.GetY() - mapHeight / 2);
                float tmp = (treasurePos - transform.position).magnitude;
                if (distance > tmp)
                {
                    treasure = i;
                    distance = tmp;
                }
                ++i;
            }
        }
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
