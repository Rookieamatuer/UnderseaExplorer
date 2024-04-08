using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public int width;
    public int height;

    public int smoothTimes = 1;
    public int clickTimes = 0;

    public string seed;
    public bool useRandomSeed;

    [Range(0, 100)]
    public int randomFillPercent;
    int[,] map;

    private void Start()
    {
        GenerateMap();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("click");
            GenerateMap();
        }
        if (Input.GetMouseButtonDown(1))
        {
            clickTimes++;
            SmoothMap();
        }
    }

    void GenerateMap()
    {
        map = new int[width, height];
        RandomFillMap();

        for (int i = 0; i < smoothTimes; ++i)
        {
            SmoothMap();
        }

        MeshGenerator meshGenerator = GetComponent<MeshGenerator>();

        meshGenerator.GenerateMesh(map, 1);
    }

    void RandomFillMap()
    {
        if (useRandomSeed)
        {
            seed = Time.time.ToString();
        }
        System.Random rand = new System.Random(seed.GetHashCode());

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (i == 0 || j == 0 || i == width - 1 || j == height - 1)
                {
                    map[i, j] = 1;
                } else
                {
                    map[i, j] = rand.Next(0, 100) < randomFillPercent ? 1 : 0;
                }
            }
        }
    }

    void SmoothMap()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                int neighbourReefTiles = GetSurroundingReefCount(i, j);

                if (neighbourReefTiles > 4)
                {
                    map[i, j] = 1;
                }
                else if (neighbourReefTiles < 4)
                {
                    map[i, j] = 0;
                }
            }
        }
    }

    int GetSurroundingReefCount(int gridX, int gridY)
    {
        int count = 0;
        for (int  x = gridX - 1; x <= gridX + 1; ++x)
        {
            for (int y =  gridY - 1; y <= gridY + 1; ++y)
            {
                if (x >= 0 && y >= 0 && x < width && y < height)
                {
                    if (x != gridX || y != gridY)
                    {
                        count += map[x, y];
                    }
                } else
                {
                    count++;
                }
            }
        }
        return count;
    }

    //private void OnDrawGizmos()
    //{
    //    if (map !=  null)
    //    {
    //        for (int i = 0; i < width; ++i)
    //        {
    //            for (int j = 0; j < height; ++j)
    //            {
    //                Gizmos.color = (map[i, j] == 1) ? Color.black : Color.white;
    //                Vector3 pos = new Vector3(-width / 2 + i + 0.5f, 0, -height / 2 + j + 0.5f);
    //                Gizmos.DrawCube(pos, Vector3.one);
    //            }
    //        }
    //    }
    //}
}
