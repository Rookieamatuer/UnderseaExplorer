using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * Computes the least cost path between two tiles on a grid.
 *
 * Based on an A* implementation by RedBlob Games.
 * 
 * Note: this code uses both tile positions (Vector2Int) and tile
 * indices (int). Tile indices are a convenient representation when
 * using tiles as Dictonary keys. See TileIndex method below.
 */
public class AStarPathfinder
{
    // The grid
    // public TileGrid grid;
    int mapWidth;
    int mapHeight;
    int[,] map;

    // Start and target tile
    public Vector2Int start;
    public Vector2Int target;

    /* 
     * The frontier records the tiles we could explore next.
     *   + Tiles are stored with their priorities (as a FrontierTile)
     *   + In A*, the priority is the estimated total path cost of taking
     *     a path through that tile.
     *   + Cheaper costs (smaller ints) are considered "higher" priority.
     *   + frontier.Dequeue will always return the highest priority tile first,
     *     i.e. the one with the cheapest estimated total path cost.
     */
    PriorityQueue<FrontierTile> frontier;

    /*
     * Record how the search algorith arrived at an explored tile. This will
     * allow us to reconstruct the path once we've found the target.
     * 
     * For example, if we explored from tile A directly to neighbour B:
     *    arrivedFrom[index of B] = position of A
     */
    Dictionary<int, Vector2Int> arrivedFrom;

    /*
     * Record the lowest known cost to reach an explored tile.
     * 
     * For example, if we can get to tile A with a path of length N:
     *    costSoFar[index of A] = N
     */
    Dictionary<int, int> costSoFar;

    // Create a pathfinder for a grid
    //public AStarPathfinder(TileGrid g)
    //{
    //    //grid = g;
    //}

    public AStarPathfinder(Vector2Int _start, Vector2Int _target)
    {
        mapWidth = UnderseaReefGenerator.instance.mapWidth;
        mapHeight = UnderseaReefGenerator.instance.mapHeight;
        map = UnderseaReefGenerator.instance.GetMapGrid();
        start = _start;
        target = _target;
    }

    // Index is an integer representation for a tile position.
    // (convenient to use as a dictionary key)
    public int TileIndex(Vector2Int tile)
    {
        // return tile.x + (tile.y * grid.width);
        return tile.x + (tile.y * mapWidth);
    }

    // Are these tile positions equal?
    protected static bool SamePosition(Vector2Int a, Vector2Int b)
    {
        return (a.x == b.x) && (a.y == b.y);
    }

    // Manhatten distance between two tiles
    // A* uses this as a heuristic estimate of cost to target
    protected static int ManhattenDistance(Vector2Int a, Vector2Int b)
    {
        return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
    }


    // Pathfind from a to b
    public List<Vector2Int> FindPath()
    {
        return FindPath(start, target);
    }

    //public List<Vector2Int> FindPath(int[,] start, int[,] goal)
    //{
    //    return FindPath(start, goal);
    //}

    protected void InitialiseAStar()
    {
        // Reset the data structures for this search
        frontier = new PriorityQueue<FrontierTile>();
        arrivedFrom = new Dictionary<int, Vector2Int>();
        costSoFar = new Dictionary<int, int>();

        // Initialise the frontier with the start tile
        frontier.Enqueue(new FrontierTile(start, 0));

        // Initialise the book-keeping data structures
        int startIndex = TileIndex(start);
        // We arrived at the start from 'nowhere'
        arrivedFrom[startIndex] = new Vector2Int(-1, -1);
        // Cost us nothing to get here
        costSoFar[startIndex] = 0;
    }

    /*
     * The A* pathfinding algorithm.
     */
    protected List<Vector2Int> FindPath(Vector2Int a, Vector2Int b)
    {
        start = a;
        target = b;
        InitialiseAStar();
        
        // Success flag
        bool foundPath = false;
        int i = 0;
        // The A* loop
        // Continue while the frontier is non-empty
        while (frontier.Count() > 0)
        {

            // Select a tile to explore (this will be the highest priority
            // tile = lowest estimated total path cost)
            FrontierTile current = frontier.Dequeue();

            // Are we there yet? If yes, then terminate main loop.
            if (SamePosition(current.position, target))
            {
                //Debug.Log("find path");
                foundPath = true;
                break;
            }

            // Not there yet! Find the index and neighbours of our tile.
            int currentIndex = TileIndex(current.position);
            List<Vector2Int> neighbours = NeighbouringTiles(current.position);

            // Add some of the neighbours to the frontier
            foreach (Vector2Int tile in neighbours)
            {
                // Find the tile's index and cost
                int index = TileIndex(tile);
                int knownCost = costSoFar[currentIndex] + 1;

                // Has this tile been encountered already?
                bool encountered = costSoFar.ContainsKey(index);

                // Is this a cheaper path to an already encountered tile?
                bool cheaper = encountered && (knownCost < costSoFar[index]);

                // Add this tile to the frontier if EITHER:
                // 1) we've not encountered it before;
                // 2) the tile is already in the frontier but we've found
                //    a new lowest cost path to this tile.
                if (!encountered || cheaper)
                {
                    // Compute neighbour's total path cost (priority)
                    int estimatedCost = ManhattenDistance(tile, target);
                    int totalCost = knownCost + estimatedCost;

                    FrontierTile item = new FrontierTile(tile, totalCost);
                    frontier.Enqueue(item);

                    arrivedFrom[index] = current.position;
                    costSoFar[index] = knownCost;
                    i++;
                }
            } // Neighbour loop
        } // A* loop
        if (!foundPath) Debug.Log("i:"+i);
        return ReconstructPath(foundPath);
    }

    protected List<Vector2Int> ReconstructPath(bool foundPath)
    {
        List<Vector2Int> path = null;

        // Reconstruct the path
        // Use arrivedFrom to track back from target to start.
        if (foundPath)
        {
            path = new List<Vector2Int>();
            Vector2Int tile = target;
            bool pathComplete = false;

            while (!pathComplete)
            {
                path.Add(tile);
                if (SamePosition(start, tile)) pathComplete = true;
                int tileIndex = TileIndex(tile);
                tile = arrivedFrom[tileIndex];
            }

            path.Reverse();
        }

        return path;
    }

    protected List<Vector2Int> NeighbouringTiles(Vector2Int tile)
    {
        List<Vector2Int> neighbours = new List<Vector2Int>();

        // Western neighbour?
        if (tile.x > 0)
            AddNonWall(ref neighbours, tile.x - 1, tile.y);

        // Eastern neighbour?
        if (tile.x < (mapWidth - 1))
            AddNonWall(ref neighbours, tile.x + 1, tile.y);

        // Southern neighbour?
        if (tile.y > 0)
            AddNonWall(ref neighbours, tile.x, tile.y - 1);

        // Northern neighbour?
        if (tile.y < (mapHeight - 1))
            AddNonWall(ref neighbours, tile.x, tile.y + 1);

        return neighbours;
    }

    protected void AddNonWall(ref List<Vector2Int> t, int x, int y)
    {
        //if (!grid.IsWall(x, y))
        if (map[x, y] == 0)
        {
            t.Add(new Vector2Int(x, y));
        }
    }

    class FrontierTile : IComparable<FrontierTile>
    {
        public Vector2Int position;
        public int priority;

        public FrontierTile(Vector2Int pn, int py)
        {
            position = pn;
            priority = py;
        }

        /*
         * Indicate whether tile has higher, equal or lower priority.
         *   + Lower integers are considered higher priority.
         *   + Returns -1, 0, 1 respectively.
         */
        public int CompareTo(FrontierTile tile)
        {
            if (priority < tile.priority) return -1;
            else if (priority == tile.priority) return 0;
            else return 1;
        }
    } // FrontierTile
}