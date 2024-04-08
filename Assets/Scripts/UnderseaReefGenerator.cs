using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnderseaReefGenerator : MonoBehaviour
{
    [Header("Map Data")]
    public int mapWidth = 60; // 地图宽度
    public int mapHeight = 80; // 地图高度
    public float cubeSize = 1f; // 每个Cube的大小
    public GameObject parent;

    public string seed;
    public bool useRandomSeed;

    [Header("Prefabs")]
    public GameObject tile;
    public GameObject reefCubePrefab; // 礁石
    public GameObject planePrefab;

    [Header("Generator Parameter")]
    [Range(0, 100)]
    public int initialReefChance = 45; // 初始地图中礁石的生成几率

    [Header("Agent")]
    public GameObject diver;
    public GameObject mermaid; 

    private int[,] map;

    private bool active = false;

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
                if (map[x, z] == 1)
                {
                    GenerateReef(x, z, reefCubePrefab);
                } else if (map[x, z] == 0)
                {
                    GenerateReef(x, z, tile);
                }
            }
        }

        active = true;
    }

    void GeneratePlane()
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
    }

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

    void GenerateReef(int x, int z, GameObject t)
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
        Vector3 position = new Vector3(x - mapWidth / 2, 0.5f, z - mapHeight / 2);

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

    public void SpawnAgent()
    {
        int x = Random.Range(0, mapWidth);
        int y = Random.Range(0, mapHeight);
        while (map[x, y] != 0)
        {
            x = Random.Range(0, mapWidth);
            y = Random.Range(0, mapHeight);
        }
        GameObject g = Instantiate(diver, new Vector3(x - mapWidth / 2, 1, y - mapHeight / 2), Quaternion.identity, parent.transform);
    }
}

