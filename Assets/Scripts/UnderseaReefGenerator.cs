using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnderseaReefGenerator : MonoBehaviour
{
    enum Map{
        TRENCH = 0,
        REEF = 1,
        TREASURE = 2,
        SPAWN = 3
    }
    public static UnderseaReefGenerator instance;

    [Header("Map Data")]
    public int mapWidth = 60; // 地图宽度
    public int mapHeight = 80; // 地图高度
    public float cubeSize = 1f; // 每个Cube的大小
    public GameObject parent;

    public int treasureNum = 3;

    private List<TilePosition> treasurePos;

    [Header("Prefabs")]
    public GameObject tile;
    public GameObject reefPrefab; 
    public GameObject planePrefab;

    public GameObject treasure;

    [Header("Generator Parameter")]
    public string seed;
    public bool useRandomSeed;
    [Range(0, 100)]
    public int initialReefChance = 45; // 初始地图中礁石的生成几率

    [Header("Agent")]
    public GameObject diver;
    public GameObject mermaid;

    private int[,] map;

    private bool active = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        } else
        {
            Destroy(gameObject);
        }

        treasurePos = new List<TilePosition>();
    }

    //void Start()
    //{
    //    GenerateMap();
    //}

    public void GenerateMap()
    {
        // stage a:
        map = new int[mapWidth, mapHeight];
        InitializeGridMap();

        // stage b:
        for (int i = 0; i < 5; i++)
        {
            SmoothMap();
        }

        // stage c:
        if (active)
        {
            ClearMap();
        }
        //GeneratePlane(); 
        for (int x = 0; x < mapWidth; x++)
        {
            for (int z = 0; z < mapHeight; z++)
            {
                if (map[x, z] == (int)Map.REEF)
                {
                    GenerateTile(x, z, 0.5f, reefPrefab);
                } else if (map[x, z] == (int)Map.TRENCH)
                {
                    GenerateTile(x, z, 0, tile);
                }
            }
        }

        SpawnTreasure();

        SpawnMermaid(false);

        active = true;
    }

    /*void GeneratePlane()
    {
        //// 创建Plane对象
        //GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        //plane.transform.position = new Vector3((mapWidth - 1) * cubeSize / 2f, 0f, (mapHeight - 1) * cubeSize / 2f);
        //plane.transform.localScale = new Vector3(mapWidth * cubeSize, 1f, mapHeight * cubeSize);
        //Destroy(plane.GetComponent<Collider>()); // 移除Plane上的碰撞器
        GameObject plane = Instantiate(planePrefab, new Vector3(-0.5f, 0, -0.5f), Quaternion.identity, parent.transform);
        //plane.transform.position = new Vector3(-0.5f, 0, -0.5f);
        plane.transform.localScale = new Vector3(0.1f * (mapWidth), 0, 0.1f * (mapHeight));
        //plane.transform.SetParent(parent.transform);
        //Material prefabMaterial = planePrefab.GetComponent<Renderer>().sharedMaterial;
        //Renderer rend = plane.GetComponent<Renderer>();
        //if (rend != null && prefabMaterial != null)
        //{
        //    rend.material = prefabMaterial;
        //}
    }*/

    void InitializeGridMap()
    {
        if (useRandomSeed)
        {
            seed = Time.time.ToString();
        }
        System.Random rand = new System.Random(seed.GetHashCode());

        for (int x = 0; x < mapWidth; x++)
        {
            for (int z = 0; z < mapHeight; z++)
            {
                if (x == 0 || x == mapWidth - 1 || z == 0 || z == mapHeight - 1)
                {
                    map[x, z] = 1; // 边界位置设置为礁石
                }
                else
                {
                    // map[x, z] = Random.Range(0, 100) < initialReefChance ? 1 : 0;
                    map[x, z] = rand.Next(0, 100) < initialReefChance ? 1 : 0;
                }
            }
        }
    }

    void SmoothMap()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int z = 0; z < mapHeight; z++)
            {
                int neighbourReefTiles = GetSurroundingReefCount(x, z);

                if (neighbourReefTiles > 4)
                {
                    map[x, z] = 1;
                }
                else if (neighbourReefTiles < 4)
                {
                    map[x, z] = 0;
                }
            }
        }
    }

    int GetSurroundingReefCount(int gridX, int gridZ)
    {
        int reefCount = 0;
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
        {
            for (int neighbourZ = gridZ - 1; neighbourZ <= gridZ + 1; neighbourZ++)
            {
                if (neighbourX >= 0 && neighbourX < mapWidth && neighbourZ >= 0 && neighbourZ < mapHeight)
                {
                    if (neighbourX != gridX || neighbourZ != gridZ)
                    {
                        reefCount += map[neighbourX, neighbourZ];
                    }
                }
                else
                {
                    reefCount++;
                }
            }
        }
        return reefCount;
    }

    void GenerateTile(int x, int z, float y, GameObject t)
    {
        // 随机生成一个形状随机的礁石
        //int numCubes = Random.Range(4, 11); // 随机确定该礁石由4到10个Cube组成
        //for (int i = 0; i < numCubes; i++)
        //{
        //    // 在当前位置附近随机偏移生成Cube
        //    Vector3 offset = new Vector3(Random.Range(-cubeSize / 2f, cubeSize / 2f), 0f, Random.Range(-cubeSize / 2f, cubeSize / 2f));
        //    // Vector3 position = new Vector3(x * cubeSize, 0f, z * cubeSize) + offset;
        //    Vector3 position = new Vector3(x - (mapWidth / 2), 0.5f, z - (mapHeight / 2)) + offset;

        //    // 实例化Cube对象
        //    GameObject reefCube = Instantiate(reefCubePrefab, position, Quaternion.identity, transform);
        //}
        Vector3 position = new Vector3(x - mapWidth / 2, y, z - mapHeight / 2);

        // 实例化Cube对象
        GameObject reefCube = Instantiate(t, position, Quaternion.identity, parent.transform);
    }

    void ClearMap()
    {
        foreach(Transform child in parent.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void SpawnTreasure()
    {
        int x, z;
        for (int i = 1; i <= treasureNum; ++i)
        {
            x = Mathf.RoundToInt(Random.Range(mapWidth * (i - 1) / treasureNum, mapWidth * i / treasureNum));
            z = Mathf.RoundToInt(Random.Range(0, mapHeight));
            // y = Random.Range((int)(mapHeight * (i - 1) / treasureNum), (int)(mapHeight * i / treasureNum));
            while (map[x, z] != 0)
            {
                x = Mathf.RoundToInt(Random.Range(mapWidth * (i - 1) / treasureNum, mapWidth * i / treasureNum));
                z = Mathf.RoundToInt(Random.Range(0, mapHeight));
            }
            //map[x, z] = (int)Map.TREASURE;
            treasurePos.Add(new TilePosition(x, z));
            GameObject g = Instantiate(treasure, new Vector3(x - mapWidth / 2, 0.5f, z - mapHeight / 2), Quaternion.identity, parent.transform);
        }
    }

    public void SpawnDiver()
    {
        int x = Random.Range(mapWidth / 4, mapWidth / 4 * 3);
        int y = Random.Range(mapHeight / 4, mapHeight / 4 * 3);
        while (map[x, y] != 0)
        {
            x = Random.Range(mapWidth / 4, mapWidth / 4 * 3);
            y = Random.Range(mapHeight / 4, mapHeight / 4 * 3);
        }
        map[x, y] = (int)Map.SPAWN;
        GameObject g = Instantiate(diver, new Vector3(x - mapWidth / 2, 0.5f, y - mapHeight / 2), Quaternion.identity, parent.transform);
    }

    public void SpawnMermaid(bool isRespawn, int num = 0)
    {
        
        if (treasurePos == null)
        {
            return;
        }
        if (isRespawn)
        {
            SpawnMermaidByTreasure(num);
            return;
        }
        for (int i = 0; i < treasureNum; ++i)
        {
            SpawnMermaidByTreasure(i);
        }
        //int x = Random.Range(mapWidth / 4, mapWidth / 4 * 3);
        //int y = Random.Range(mapHeight / 4, mapHeight / 4 * 3);
        //while (map[x, y] != 0)
        //{
        //    x = Random.Range(mapWidth / 4, mapWidth / 4 * 3);
        //    y = Random.Range(mapHeight / 4, mapHeight / 4 * 3);
        //}
        //map[x, y] = (int)Map.SPAWN;
        //GameObject g = Instantiate(mermaid, new Vector3(x - mapWidth / 2, 0.5f, y - mapHeight / 2), Quaternion.identity, parent.transform);
    }

    private void SpawnMermaidByTreasure(int num)
    {
        int minX, maxX, minY, maxY;
        minX = treasurePos[num].GetX() - mapWidth / 4;
        maxX = treasurePos[num].GetX() + mapWidth / 4;
        minY = treasurePos[num].GetY() - mapHeight / 4;
        maxY = treasurePos[num].GetY() + mapHeight / 4;
        minX = minX > 0 ? minX : 0;
        minY = minY > 0 ? minY : 0;
        maxX = maxX < mapWidth ? maxX : mapWidth - 1;
        maxY = maxY < mapHeight ? maxY : mapHeight - 1;
        int x = Random.Range(minX, maxX);
        int y = Random.Range(minY, maxY);
        while (map[x, y] != 0)
        {
            x = Random.Range(minX, maxX);
            y = Random.Range(minY, maxY);
        }
        //map[x, y] = (int)Map.SPAWN;
        GameObject g = Instantiate(mermaid, new Vector3(x - mapWidth / 2, 0.5f, y - mapHeight / 2), Quaternion.identity, parent.transform);
    }

    public int[,] GetMapGrid()
    {
        return map;
    }

    public List<TilePosition> GetTreasurePos()
    {
        return treasurePos;
    }

    public struct TilePosition
    {
        private int x;
        private int y;
        public TilePosition(int _x, int _y)
        {
            x = _x;
            y = _y;
        }

        public int GetX()
        {
            return x;
        }

        public int GetY()
        {
            return y;
        }
    }
}

