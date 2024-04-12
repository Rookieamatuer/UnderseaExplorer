using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq.Expressions;
using UnityEngine;

public class UnderseaReefGenerator : MonoBehaviour
{
    enum Map{
        TRENCH = 0,
        REEF = 1
    }
    public static UnderseaReefGenerator instance;

    [Header("Map Data")]
    public int mapWidth = 60; 
    public int mapHeight = 80; 
    public GameObject parent;

    public int mermaidNum = 8;


    [Header("Prefabs")]
    public GameObject trenchPrefab;
    public GameObject reefPrefab; 

    [Header("Generator Parameter")]
    public string seed;
    public bool useRandomSeed;
    [Range(0, 100)]
    public int initialReefChance = 45; 

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
    }


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
        ProcessMap();

        // Load map 
        if (active)
        {
            ClearMap();
        }
        for (int x = 0; x < mapWidth; x++)
        {
            for (int z = 0; z < mapHeight; z++)
            {
                if (map[x, z] == (int)Map.REEF)
                {
                    GenerateTile(x, z, 0f, reefPrefab);
                } else if (map[x, z] == (int)Map.TRENCH)
                {
                    GenerateTile(x, z, 0, trenchPrefab);
                }
            }
        }

        SpawnMermaid(false);

        active = true;
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
                int neighbourReefTiles = GetSurroundingReefCount(x, z, 1);

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

    void ProcessMap()
    {
        List<List<TilePosition>> reefRegions = GetRegions(1);
        int reefThresholdSize = 10;

        foreach (List<TilePosition> reefRegion in reefRegions)
        {
            if (reefRegion.Count < reefThresholdSize)
            {
                foreach (TilePosition tile in reefRegion)
                {
                    map[tile.x, tile.y] = 0;
                }
            }
        }

        List<List<TilePosition>> trenchRegions = GetRegions(0);
        int trenchThresholdSize = mapHeight * mapWidth / 10;

        foreach (List<TilePosition> trenchRegion in trenchRegions)
        {
            if (trenchRegion.Count < trenchThresholdSize)
            {
                foreach (TilePosition tile in trenchRegion)
                {
                    map[tile.x, tile.y] = 1;
                }
            }
        }
    }

    List<List<TilePosition>> GetRegions(int tileType)
    {
        List<List<TilePosition>> regions = new List<List<TilePosition>>();
        int[,] mapRegions = new int[mapWidth, mapHeight];

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (mapRegions[x, y] == 0 && map[x, y] == tileType)
                {
                    List<TilePosition> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);

                    foreach (TilePosition tile in newRegion)
                    {
                        mapRegions[tile.x, tile.y] = 1;
                    }
                }
            }
        }

        return regions;
    }

    List<TilePosition> GetRegionTiles(int startX, int startY)
    {
        List<TilePosition> tiles = new List<TilePosition>();
        int[,] mapRegions = new int[mapWidth, mapHeight];
        int tileType = map[startX, startY];

        Queue<TilePosition> queue = new Queue<TilePosition>();
        queue.Enqueue(new TilePosition(startX, startY));
        mapRegions[startX, startY] = 1;

        while (queue.Count > 0)
        {
            TilePosition tile = queue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.x - 1; x <= tile.x + 1; x++)
            {
                for (int y = tile.y - 1; y <= tile.y + 1; y++)
                {
                    if (IsInMap(x, y) && (y == tile.y || x == tile.x))
                    {
                        if (mapRegions[x, y] == 0 && map[x, y] == tileType)
                        {
                            mapRegions[x, y] = 1;
                            queue.Enqueue(new TilePosition(x, y));
                        }
                    }
                }
            }
        }

        return tiles;
    }

    bool IsInMap(int x, int y)
    {
        return x >= 0 && x < mapWidth && y >= 0 && y < mapHeight;
    }

    int GetSurroundingReefCount(int gridX, int gridZ, int range)
    {
        int reefCount = 0;
        for (int neighbourX = gridX - range; neighbourX <= gridX + range; neighbourX++)
        {
            for (int neighbourZ = gridZ - range; neighbourZ <= gridZ + range; neighbourZ++)
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
        Vector3 position = new Vector3(x - mapWidth / 2, y, z - mapHeight / 2);

        GameObject tile = Instantiate(t, position, Quaternion.identity, parent.transform);
    }

    void ClearMap()
    {
        foreach(Transform child in parent.transform)
        {
            Destroy(child.gameObject);
        }
    }

    // Spawn diver
    public void SpawnDiver()
    {
        int x = Random.Range(mapWidth / 4, mapWidth / 4 * 3);
        int y = Random.Range(mapHeight / 4, mapHeight / 4 * 3);
        while (map[x, y] != 0)
        {
            x = Random.Range(mapWidth / 4, mapWidth / 4 * 3);
            y = Random.Range(mapHeight / 4, mapHeight / 4 * 3);
        }
        GameObject g = Instantiate(diver, new Vector3(x - mapWidth / 2, 0.5f, y - mapHeight / 2), Quaternion.identity, parent.transform);
    }

    // Spawn mermaid

    public void SpawnMermaid(bool isRespawn)
    {
        int count = mermaidNum;
        if (isRespawn) count = 1;
        for (int i = 0; i < count; ++i)
        {
            int x = Random.Range(0, mapWidth - 1);
            int y = Random.Range(0, mapHeight - 1);
            while (GetSurroundingReefCount(x, y, 3) > 0 && map[x, y] != 0)
            {
                //x = Random.Range(mapWidth / 4, mapWidth / 4 * 3);
                //y = Random.Range(mapHeight / 4, mapHeight / 4 * 3);
                x = Random.Range(0, mapWidth - 1);
                y = Random.Range(0, mapHeight - 1);
            }
            GameObject g = Instantiate(mermaid, new Vector3(x - mapWidth / 2, 0.5f, y - mapHeight / 2), Quaternion.identity, parent.transform);
        }
    }

    public void RespawnMermaid()
    {
        StartCoroutine(SpawnCoolTimeCoroutine());
    }

    public IEnumerator SpawnCoolTimeCoroutine()
    {
        yield return new WaitForSeconds(5);
        SpawnMermaid(true);
    }


    public int[,] GetMapGrid()
    {
        return map;
    }


    public struct TilePosition
    {
        public int x;
        public int y;
        public TilePosition(int _x, int _y)
        {
            x = _x;
            y = _y;
        }
    }
}

