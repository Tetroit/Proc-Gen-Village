using Generation;
using System;
using UnityEngine;

public class SideBuildModule : MonoBehaviour, IModuleGenerator
{
    RectInt schematicBounds;
    PrefabList prefabs;
    int height = 0;
    int orientation = 0;
    public void Generate(ref ModuleInfo info, ref HouseGrid grid)
    {
        schematicBounds = info.area;
        prefabs = info.prefabs;
        orientation = info.orientation;

        height = (info.orientation == 0) ?
            schematicBounds.height / 2 :
            schematicBounds.width / 2;

        for (int i=0; i<schematicBounds.width; i++)
        {
            for (int j=0; j < schematicBounds.height; j++)
            {
                //transforms
                Vector2Int orientedPos = GenerationUtils.GetOrientedPosition(orientation, schematicBounds, new Vector2Int(i, j));
                int l = orientedPos.x; //long side
                int w = orientedPos.y; //wide side
                int totalLength = (orientation % 2 == 0) ? schematicBounds.width : schematicBounds.height;
                int totalWidth = (orientation % 2 == 0) ? schematicBounds.height : schematicBounds.width;

                //build walls (not final yet)
                int h = Math.Min(totalWidth - w - 1, w);
                for (int k = 0; k <= h; k++)
                    grid[k, i + schematicBounds.x, j + schematicBounds.y] = 1;

                //build roof
                string suffix = "straight";
                bool mirrorZ = false;
                if (l == 0)
                {
                    //suffix = "end";
                    mirrorZ = true;
                }
                else if (l == totalLength - 1)
                {
                    suffix = "end";
                }

                int facing = totalWidth - w - 1 < w ? 1 : -1;
                h++;
                GenerateRoof(i, j);
                if (l == 0)
                {
                    Vector2Int pos = new Vector2Int(i, j) - GenerationUtils.GetDirection(orientation);
                    GenerateRoof(pos.x, pos.y);
                }

                void GenerateRoof(int posX, int posY)
                {
                    var roof = Instantiate(prefabs.GetPrefab($"roof_{suffix}"), transform);
                    roof.transform.localPosition = new Vector3(posX + 0.5f, height, posY + 0.5f);
                    roof.transform.localRotation = Quaternion.Euler(0, (orientation + 3) * 90, 0);
                    roof.transform.localScale = new Vector3(facing, 1, mirrorZ ? -1 : 1);
                }

                grid[h, i + schematicBounds.x, j + schematicBounds.y] = 2;
            }
        }
    }

    public RectInt GetSchematicBounds()
    {
        return schematicBounds;
    }
    public int GetHeight()
    {
        return height;
    }
    public int GetFacing()
    {
        return orientation;
    }
}
