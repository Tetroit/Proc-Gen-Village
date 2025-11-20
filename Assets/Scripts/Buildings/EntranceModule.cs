using Generation;
using TetraUtils;
using System.Collections.Generic;
using UnityEngine;
using JetBrains.Annotations;
using Unity.VisualScripting;

public class EntranceModule : MonoBehaviour, IModuleGenerator
{
    public class NodeTree
    {
        public List<Node> nodes;
        public Node root => nodes[0];
        public int Count => nodes.Count;
        public NodeTree(Node start = null)
        {
            nodes = new List<Node>();
            if (start != null) 
                nodes.Add(start);
        }
        public Node this[Node n]
        {
            get => n.previousNode;
        }
        public Node Branch(Node node)
        {
            Node newNode = new Node(node);
            nodes.Add(newNode);
            return newNode;
        }

    }
    public class Node
    {
        public Node previousNode;
        public int iteration { private set; get; } = 0;
        public Vector2Int schematicPos = Vector2Int.zero;
        public int dir = 0;
        public bool stairs = false;
        public bool covered = false;
        public Node(Node node)
        {
            previousNode = node;
            iteration = node.iteration+1;
        }
        public Node()
        {
        }
    }
    RectInt schematicBounds;
    PrefabList prefabs;
    public float chanceReduction = 0.01f;
    public float chance = 1f;
    public float queueStackFac = 0.5f;
    public void Generate(ref ModuleInfo info, ref HouseGrid grid)
    {
        Vector2Int entrance = info.area.position;
        schematicBounds = info.area;
        prefabs = info.prefabs;
        NodeTree tree = new NodeTree(new Node());
        tree.root.schematicPos = info.area.position;
        tree.root.dir = info.orientation;
        grid[0, schematicBounds.position] = 3;

        List<Node> toVisit = new List<Node>();
        toVisit.Add(tree.root);

        //I have absolutely no control over this, but unity succs at
        //StackOverflow detection, so I will make some breaks for this
        short antiOverflow = 0;
        while (toVisit.Count > 0 && antiOverflow < 1000)
        {
            antiOverflow++;
            //pick node
            int id = (int)(toVisit.Count * queueStackFac);
            Node node = toVisit[id];
            toVisit.RemoveAt(id);
            float spawnChance = chance - chanceReduction * tree.Count;

            int children = 0;
            //for each direction try branching
            for (int dir = 0; dir < 4; dir++)
            {
                //if cell is taken return
                if (grid.GetNeighbourInDir(dir, node.schematicPos) != 0)
                    continue;

                //if unlucky return
                float rand = Random.Range(0f, 1f);
                if (rand > spawnChance)
                    continue;

                //create new node in that direction
                var newNode = tree.Branch(node);
                toVisit.Add(newNode);
                spawnChance -= chanceReduction;
                newNode.schematicPos = node.schematicPos + GenerationUtils.GetDirection(dir);
                newNode.dir = dir;
                grid[0, newNode.schematicPos] = 3;
                children++;
            }

            //spawn stairs if this is bare end
            string prefabName = "floor";
            if (children == 0 && grid.GetNeighbourInDir(node.dir, node.schematicPos) <= 0) //I can cheat here because 0 is empty and -1 is border 
                prefabName = "stairs";

            //actually spawn the tile
            var offset = node.schematicPos - schematicBounds.position;
            var newItem = Instantiate(prefabs.GetPrefab(prefabName), transform);
            newItem.transform.localPosition = new Vector3(offset.x + 0.5f, 0, 0.5f + offset.y);
            newItem.transform.localRotation = Quaternion.Euler(0, node.dir * 90, 0);
        }

    }

    public RectInt GetSchematicBounds()
    {
        return schematicBounds;
    }
    public int GetHeight()
    {
        return 1;
    }
    public int GetFacing()
    {
        return 0;
    }
}
