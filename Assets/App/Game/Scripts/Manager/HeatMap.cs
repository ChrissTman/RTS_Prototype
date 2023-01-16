using UnityEngine;
using System.Collections;

public class HeatMap : MonoBehaviour
{
    [SerializeField] ManagerContext context;

    [SerializeField] float detectionBias;
    FixedPool<HeatMapRecord> recordsPool;

    [Header("Debug")]
    [SerializeField] Terrain terrain;
    [SerializeField] Material terMat;

    Vector4 defaultVector = new Vector4(0, 0, 0, 0);
    Vector4[] lines = new Vector4[1000];
    Vector4[] properties = new Vector4[1000];
    int dataAdded;

    float mapSize { get { return context.MapManager.MapSize; } }

    bool enableDebugOverlay;
    bool overlayLastFrame;

    void Start()
    {
        recordsPool = new FixedPool<HeatMapRecord>(2000);
        for (int i = 0; i < recordsPool.BufferSize; i++)
        {
            recordsPool.SetAtIndex(i, new HeatMapRecord());
        }

        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = defaultVector;
            properties[i] = defaultVector;
        }

        //5 FPS
        InvokeRepeating("RemoveOldRecords", 0, 1f / 5f);
        InvokeRepeating("UpdateDebugInfo", 0, 1f / 5f);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha0))
        {
            enableDebugOverlay = !enableDebugOverlay;
        }
    }

    void RemoveOldRecords()
    {
        for (int i = 0; i < recordsPool.BufferSize; i++)
        {
            var slot = recordsPool.Buffer[i];
            if(slot.Taken)
            {
                var record = slot.Element;
                if(Time.time  > record.Time + record.Length)
                {
                    recordsPool.ReturnElement(record);
                }
            }
        }
    }

    void UpdateDebugInfo()
    {
        if (!overlayLastFrame && !enableDebugOverlay)
            return;

        if(overlayLastFrame && !enableDebugOverlay)
        {
            terMat.SetInt("_EnableOverlay", 0);
            terMat.SetInt("_DataSize", 0);
            overlayLastFrame = false;

            terrain.materialType = Terrain.MaterialType.Custom;
            terrain.materialTemplate = null;
            terrain.materialTemplate = terMat;
            return;
        }

        dataAdded = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = defaultVector;
            properties[i] = defaultVector;
        }

        for (int i = 0; i < recordsPool.BufferSize; i++)
        {
            var slot = recordsPool.Buffer[i];
            if(slot.Taken)
            {
                var record = slot.Element;

                var team = record.Team == Team.TeamGreen ? 2 : 1;

                var size = detectionBias / mapSize;
                
                lines[dataAdded] = new Vector4(record.startX, record.startY, record.endX, record.endY);
                properties[dataAdded] = new Vector4(size, team, 0, 0);
                dataAdded++;
            }
        }
        

        terrain.materialTemplate = null;
        terrain.materialTemplate = terMat;

        terrain.materialType = Terrain.MaterialType.Custom;

        terMat.SetInt("_EnableOverlay", dataAdded != 0 ? 1 : 0);
        terMat.SetVectorArray("_Lines", lines);
        terMat.SetVectorArray("_Properties", properties);
        terMat.SetInt("_DataSize", dataAdded);

        overlayLastFrame = enableDebugOverlay;
    }

    public void AddRecord(Vector3 start, Vector3 end, float length, Team t)
    {
        var record = recordsPool.GetAvailable();

        var halfSize = mapSize / 2f;

        record.startX = (start.x + halfSize) / mapSize;
        record.startY = (start.z + halfSize) / mapSize;

        record.endX = (end.x + halfSize) / mapSize;
        record.endY = (end.z + halfSize) / mapSize;

        record.Time = Time.time;
        record.Length = length;

        record.Team = t;

        //print($"Record: x:{record.endX.ToString("0.000")} y{record.endY.ToString("0.000")}");
    }

    /// <summary>
    /// Get any record in the Point + inaccuracy provided via bias
    /// </summary>
    /// <param name="point">Checked point in space</param>
    /// <param name="bias">Inaccuracy of the search</param>
    /// <returns></returns>
    public HeatMapRecord CheckPoint(Vector3 point)
    {
        for (int i = 0; i < recordsPool.BufferSize; i++)
        {
            var slot = recordsPool.Buffer[i];
            if (slot.Taken)
            {
                var record = slot.Element;

                float pointX = point.x / mapSize;
                float pointY = point.y / mapSize;

                float distToStart = Vector2.Distance(new Vector2(pointX, pointY), new Vector2(record.startX, record.startY));
                float distToEnd = Vector2.Distance(new Vector2(pointX, pointY), new Vector2(record.endX, record.endY));
                float distStartEnd = Vector2.Distance(new Vector2(record.startX, record.startY), new Vector2(record.endX, record.endY));

                if(distToStart + distToEnd < distStartEnd + detectionBias)
                {
                    //valid record

                    return record;
                }
            }
        }

        return null;
    }
}
public class HeatMapRecord
{
    public float Time;
    public float Length;
    
    //0f-1f like UVs
    public float startX, startY;
    public float endX, endY;

    public Team Team;
}