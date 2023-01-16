using UnityEngine;
using System.Collections;

public class RegionManager : MonoBehaviour
{
    public int NumberOfRegions;
    public int MapSize;
    public int UnitInternalBufferSize;

    public GameObject RegionVisualization;

    MapRegion[,] regions;

    private void Awake()
    {
        CreateRegions();

        //4 FPS
        InvokeRepeating("UpdateVisualizations", 0, 1f / 4f);
    }

    bool visualize;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha9))
        {
            visualize = !visualize;
        }

        //TEMP
        if(Input.GetKeyDown(KeyCode.Alpha8))
        {
            var state = !SoldierMove.markerRoot.gameObject.activeSelf;
            SoldierMove.markerRoot.gameObject.SetActive(state);
        }

        //if(Input.GetKeyDown(KeyCode.Space))
        //{
        //    foreach(var x in regions)
        //    {
        //        if (x.Units.GetTakenAmount() > 0)
        //        {
        //            print($"--- [{x.Region.Middle.x}, {x.Region.Middle.z}] ---");
        //            print($"> {x.Units.GetTakenAmount()}");
        //        }
        //    }
        //}
    }

    public MapRegion UpdateUnit(Unit unit)
    {
        MapRegion unitRegion = unit.CurrentRegion;

        foreach (MapRegion newRegion in regions)
        {
            if (newRegion.Region.IsPointContained(unit.Position))
            {
                bool changedRegion = (unitRegion != newRegion && unitRegion != null);
                if (changedRegion)
                {
                    unitRegion.Units.Remove(unit);
                    newRegion.Units.Add(unit);
                }
                else if(unitRegion == null)
                    newRegion.Units.Add(unit);

                return newRegion;
            }
        }

        return null;
    }

    int regionSize;
    void CreateRegions()
    {
        regionSize = MapSize / NumberOfRegions;

        regions = new MapRegion[NumberOfRegions, NumberOfRegions];

        for (int y = 0; y < NumberOfRegions; y++)
        {
            for (int x = 0; x < NumberOfRegions; x++)
            {
                Vector3 middle = new Vector3(x * regionSize, 0, y * regionSize) - //Position 
                                 new Vector3(MapSize / 2f, 0, MapSize / 2f) +  //Center
                                 new Vector3(regionSize / 2f, 0, regionSize / 2f); //Correct

                var mr = new MapRegion()
                {
                    Region = new Region(middle, regionSize),
                    Units = new FixedList<Unit>(UnitInternalBufferSize),
                };
                mr.InitializeVisualizer(RegionVisualization);

                regions[y, x] = mr;
            }
        }
    }

    void UpdateVisualizations()
    {
        foreach(MapRegion region in regions)
        {
            if (visualize)
            {
                int amount = region.Units.GetTakenAmount();
                region.SetVisualizer(amount);
            }
            else
            {
                region.SetVisualizer(0);
            }
        }
    }

    public Unit GetClosestUnit(Vector3 position, float range, Team team)
    {
        var halfSize = MapSize / 2f;
        var pos = position + new Vector3(halfSize, 0, halfSize);

        int regionX = (int) (pos.x / regionSize);
        int regionY = (int) (pos.z / regionSize);

        var r = (int) (range + regionSize /2f); // over compensate

        int minX = regionX - r;
        int maxX = regionX + r;
        int minY = regionY - r;
        int maxY = regionY + r;

        float distance = float.MaxValue;
        Unit closestUnit = null;
        for (int y = minY; y < maxY; y++) 
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (x < 0 || y < 0 || x >= NumberOfRegions || y >= NumberOfRegions)
                    continue;

                var region = regions[y, x];
                foreach (Slot<Unit> slot in region.Units)
                {
                    if (!slot.Taken)
                        continue;

                    var unit = slot.Element;

                    if (!unit.IsAlive || unit.Team != team)
                        continue;

                    if (unit.Team != team)
                        Debug.Log("picovina");

                    var dist = Vector3.Distance(NoY(position), NoY(unit.WorldPosition));
                    if (dist < distance)
                    {
                        distance = dist;
                        closestUnit = unit;
                    }
                }
            }
        }

        return closestUnit;
    }

    Vector3 NoY(Vector3 v3)
    {
        return new Vector3(v3.x, 0, v3.z);
    }
}

public class MapRegion
{
    public FixedList<Unit> Units;
    public Region Region;
    public Transform Visualizer;

    static Transform root;
    static Transform VisualizerRoot
    {
        get
        {
            if(root == null)
            {
                root = new GameObject(" ~*~ Region visualization root ~*~").transform;
            }
            return root;
        }
    }

    public void InitializeVisualizer(GameObject visualizer)
    {
        Visualizer = GameObject.Instantiate(visualizer, VisualizerRoot).transform;
        Visualizer.position = Region.Middle;
        Visualizer.localScale = new Vector3(Region.Size, 0, Region.Size);
    }
    public void SetVisualizer(int sizeY)
    {
        if (sizeY == 0)
            Visualizer.gameObject.SetActive(false);
        else
        {
            Visualizer.gameObject.SetActive(true);

            var scale = Visualizer.localScale;
            scale.y = sizeY / 2f;
            Visualizer.localScale = scale;
        }

    }
}
public class Region
{
    public Vector3 Middle { get; private set; }
    public float Size { get; private set; }
    public Vector4 Borders; // X-left Y-right Z-down W-top

    float halfSize { get { return Size / 2f; } }
    public Region(Vector3 middle, float size)
    {
        Middle = middle;
        Size = size;
        Borders.x = Middle.x - halfSize;
        Borders.y = Middle.x + halfSize;
        Borders.z = Middle.z - halfSize;
        Borders.w = Middle.z + halfSize;
    }
    
    public bool IsPointContained(Vector3 point)
    {
        return Borders.x <= point.x && point.x <= Borders.y &&
               Borders.z <= point.z && point.z <= Borders.w;
    }

}


public class FixedList<T> where T : class
{
    public Slot<T>[] Buffer { get; private set; }
    public int BufferSize { get; private set; }

    int takenAmount;

    public FixedList(int size)
    {
        BufferSize = size;

        Buffer = new Slot<T>[size];
        for (int i = 0; i < size; i++)
        {
            Buffer[i] = new Slot<T>();
        }
    }
    
    public void ClearAll()
    {
        for (int i = 0; i < BufferSize; i++)
        {
            Buffer[i].Element = null;
            Buffer[i].Taken = false;
        }
        takenAmount = 0;
    }
    public bool Contains(T newElement)
    {
        for (int i = 0; i < BufferSize; i++)
        {
            if(Buffer[i].Element == newElement)
                return true;
        }
        return false;
    }
    public bool Add(T newElement)
    {
        for (int i = 0; i < BufferSize; i++)
        {
            if(!Buffer[i].Taken)
            {
                Buffer[i].Element = newElement;
                Buffer[i].Taken = true;
                takenAmount++;
                return true;
            }
        }
        return false;
    }
    public bool Remove(T elementToRemove)
    {
        for (int i = 0; i < BufferSize; i++)
        {
            if (Buffer[i].Element == elementToRemove)
            {
                Buffer[i].Element = null;
                Buffer[i].Taken = false;
                takenAmount--;
                return true;
            }
        }
        return false;
    }
    public int GetEmptyAmount()
    {
        return BufferSize - takenAmount;
    }
    public int GetTakenAmount()
    {
        return takenAmount;
    }
    public IEnumerator GetEnumerator()
    {
        return Buffer.GetEnumerator();
    }
}

public class FixedPool<T> where T : class
{
    public Slot<T>[] Buffer { get; private set; }
    public int BufferSize { get; private set; }

    int available;

    public FixedPool(int size)
    {
        BufferSize = size;

        Buffer = new Slot<T>[size];
        for (int i = 0; i < size; i++)
        {
            Buffer[i] = new Slot<T>();
        }
        available = BufferSize;
    }

    public void SetAtIndex(int i, T value)
    {
        Buffer[i].Element = value;
    }

    public T GetAvailable()
    {
        for (int i = 0; i < BufferSize; i++)
        {
            if(!Buffer[i].Taken)
            {
                Buffer[i].Taken = true;
                available--;
                return Buffer[i].Element;
            }
        }
        return null;
    }

    public void ReturnElement(T element)
    {
        for (int i = 0; i < BufferSize; i++)
        {
            if (Buffer[i].Element == element)
            {
                Buffer[i].Taken = false;
                available++;
                return;
            }
        }

        Debug.LogError($"Couldn't return {element.GetType()}");
    }
    public void ReturnAll()
    {
        for (int i = 0; i < BufferSize; i++)
        {
            Buffer[i].Taken = false;
        }
    }

    public bool Contains(T newElement)
    {
        for (int i = 0; i < BufferSize; i++)
        {
            if (Buffer[i].Element == newElement)
                return true;
        }
        return false;
    }


    public int GetEmptyAmount()
    {
        return available;
    }
    public int GetTakenAmount()
    {
        return BufferSize - available;
    }
    public IEnumerator GetEnumerator()
    {
        return Buffer.GetEnumerator();
    }
}

public class Slot<T> where T : class
{
    public T Element;
    public bool Taken;
}
