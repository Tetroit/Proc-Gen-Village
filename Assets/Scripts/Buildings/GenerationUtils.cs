using Generation;
using System.Collections.Generic;
using TetraUtils;
using UnityEngine;


[System.Serializable]
public struct HouseArea
{
    public OBB obb;
    public float rotation;
    public float elevation;
    public float blockSize;

    public HouseArea(OBB obb, CityGenerator generator)
    {
        this.obb = obb;
        rotation = Mathf.Atan2(obb.a1.y, obb.a2.x);
        elevation = 0;
        blockSize = generator.blockSize;
    }
}
[System.Serializable]
public struct ModuleInfo
{
    public PrefabList prefabs;
    public int orientation;
    public RectInt area;
    public Rect totalBounds;
    public float height;

    public object[] other;

    public ModuleInfo(PrefabList prefabs, int orientation, RectInt area, Rect totalBounds, float height = 0, params object[] other)
    {
        this.orientation = orientation;
        this.prefabs = prefabs;
        this.area = area;
        this.totalBounds = totalBounds;
        this.height = height;

        this.other = other;
    }
}

public class HouseGrid
{
    //-1 - border/invalid
    //0 - free
    //1 - wall
    //2 - roof
    //3 - open
    //4 - open-roofed
    //5 - entrance
    //6 - stairs

    List<int[,]> freeSpaces;
    Vector2Int size;
    private static int FLOOR_LIMIT = 32;
    private static int DIMENTIONS_LIMIT = 128;
    public Vector2Int Size => size;
    public List<int[,]> grid => freeSpaces;
    public int floors => freeSpaces.Count;
    public HouseGrid(Vector2Int size)
    {
        if (size.x > DIMENTIONS_LIMIT || size.y > DIMENTIONS_LIMIT)
            throw new System.Exception("Too large size");
        this.size = size;
        freeSpaces = new List<int[,]>(FLOOR_LIMIT);
    }
    public void AddFloor()
    {
        if (floors == FLOOR_LIMIT)
            throw new System.Exception("Maximum floor limit reached!");

        var newFloor = new int[size.x, size.y];
        DataUtils.Nullify(ref newFloor, 0);
        freeSpaces.Add(newFloor);
    }

    public int[] GetNeighbours(Vector2Int pos, int floor = 0)
    {
        var res = new int[4];
        for (int i=0; i<4; i++)
            res[i] = GetNeighbourInDir(i,pos);
        return res;
    }
    public void Reset()
    {
        freeSpaces.Clear();
    }
    public int GetNeighbourInDir(int dir, Vector2Int pos, int floor = 0)
    {
        switch (dir)
        {
            case 0:
                if (pos.x == 0) return -1;
                pos.x--;
                return this[floor, pos];
            case 1:
                if (pos.y == size.y - 1) return -1;
                pos.y++;
                return this[floor, pos];
            case 2:
                if (pos.x == size.x - 1) return -1;
                pos.x++;
                return this[floor, pos];
            case 3:
                if (pos.y == 0) return -1;
                pos.y--;
                return this[floor, pos];
        }
        return -1;
    }
    public bool IsTaken(Vector2Int pos, int f)
    {
        if (pos.x < 0 || pos.x >= size.x || pos.y < 0 || pos.y >= size.y)
            return true;
        if (this[f,pos] == 0)
            return false;
        return true;
    }
    public bool IsTaken(RectInt rect, int f)
    {
        for (int i = rect.xMin; i < rect.xMax; i++)
        {
            for (int j = rect.yMin; j < rect.yMax; j++)
            {
                if (this[f, i, j] != 0)
                    return true;
            }
        }
        return false;
    }
    public int this[int f, Vector2Int pos]
    {
        get => this[f, pos.x, pos.y];
        set => this[f, pos.x, pos.y] = value;
    }
    public int this[int f, int x, int y]
    {
        get 
        {
            while (floors <= f)
                AddFloor();
            return grid[f][x, y];
        }
        set
        {
            while (floors <= f)
                AddFloor();
            grid[f][x, y] = value;
        }
    }
}

public interface IHouseGenerator 
{
    void Generate(HouseArea area);
}
public interface IModuleGenerator
{
    /// <summary>
    /// Generates the module and records its parameters to <paramref name="args"/>
    /// </summary>
    /// <param name="args"></param>
    void Generate(ref ModuleInfo args, ref HouseGrid grid);
    RectInt GetSchematicBounds();
    int GetHeight();
    int GetFacing();

}

public static class GenerationUtils
{

    /*
     * Corner IDS:
     * 
     * 0 - min,min
     * 1 - min,max
     * 2 - max,max
     * 3 - max,min
     * 
     * Facing IDS:
     * 
     * 0 - -x (width)
     * 1 - +y (height)
     * 2 - +x (width)
     * 3 - -y (height)
     * 
     * 
     * Orientation IDS:
     * 0 - width
     * 1 - length
     */

    public static Vector2Int GetCornerDir(int corner)
    {
        return GetCornerPos(corner, -1, -1, 1, 1);
    }
    public static Vector2Int GetCornerPos(int corner, int maxX, int maxY, int minX = 0, int minY = 0)
    {
        switch (corner)
        {
            case 0:
                return new Vector2Int(minX, minY);
            case 1:
                return new Vector2Int(minX, maxY);
            case 2:
                return new Vector2Int(maxX, maxY);
            case 3:
                return new Vector2Int(maxX, minY);
        }
        return Vector2Int.zero;
    }
    public static Vector2Int GetDirection(int direction)
    {
        return GetDirection(direction, 1, 1);
    }
    public static Vector2Int GetDirection(int direction, int x, int y)
    {
        switch (direction)
        {
            case 0:
                return new Vector2Int(-x, 0);
            case 1:
                return new Vector2Int(0, y);
            case 2:
                return new Vector2Int(x, 0);
            case 3:
                return new Vector2Int(0, -y);
        }
        return Vector2Int.zero;
    }
    public static int GetDirectionValue(int direction, int x, int y)
    {
        switch (direction)
        {
            case 0:
                return -x;
            case 1:
                return y;
            case 2:
                return x;
            case 3:
                return -y;
        }
        return 0;
    }
    public static RangeInt GetRectWall(int direction, RectInt rect)
    {

        switch (direction)
        {
            case 0:
            case 2:
                return new RangeInt(rect.yMin, rect.height);
            case 1:
            case 3:
                return new RangeInt(rect.xMin, rect.width);
        }
        return new RangeInt(0, 0);
    }
    /// <summary>
    /// Returns the rotated position of the given point in the rectangle
    /// </summary>
    /// <param name="direction"> The direction of the rotated x axis of rectangle</param>
    /// <param name="rect">Rect to look into</param>
    /// <param name="pos">In Rect coords</param>
    /// <returns></returns>
    public static Vector2Int GetOrientedPosition(int direction, RectInt rect, Vector2Int pos)
    {
        int[] dirs =
        {
            pos.x,
            rect.height - pos.y - 1,
            rect.width - pos.x - 1,
            pos.y,
        };
        return new Vector2Int(dirs[(direction + 2) % 4], dirs[(direction + 1) % 4]);
    }
    public static int GetDirectionLimit(int direction, RectInt rect)
    {

        switch (direction)
        {
            case 0:
                return rect.xMin;
            case 1:
                return rect.yMax - 1;
            case 2:
                return rect.xMax - 1;
            case 3:
                return rect.yMin;
        }
        return 0;
    }
}
