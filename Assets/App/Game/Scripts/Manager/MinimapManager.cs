using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class MinimapManager : MonoBehaviour
{
    [SerializeField] ManagerContext context;

    [Header("Context")]
    [SerializeField] RawImage minimapImage;
    [SerializeField] RawImage artilleryImage;
    [SerializeField] float mapSize;
    
    Vector4 defaultVector = new Vector4(0, 0, 0, 0);
    Vector4[] data = new Vector4[1000];
    int dataAdded;
    int takenAmount;
    void Awake()
    {
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = defaultVector;
        }

        //5FPS
        InvokeRepeating("UpdateMinimap", Random.Range(0.1f, 0.2f), 1f / 5f);
    }
    
    //TODO: map size isn't dynamic
    void UpdateMinimap()
    {
        dataAdded = 0;

        for (int i = 0; i < data.Length; i++)
        {
            data[i] = defaultVector;
        }

        var units = context.UnitPool.Units.Values;
        foreach(var category in units)
        {
            foreach(var slot in category.Buffer)
            {
                if (!slot.Taken || !slot.Element.IsAlive || !slot.Element.IsVisible)
                    continue;
                
                //x - x pos, y - y pos, z - size, w - color
                //(viz Minimap.shader) colors: 0 white, 1 red, 2 green, 3 blue

                var unit = slot.Element;
                var x = (unit.Position.x + mapSize / 2f) / mapSize;
                var y = (unit.Position.z + mapSize / 2f) / mapSize;
                var v4 = data[dataAdded];
                v4.x = x;
                v4.y = y;
                v4.z = 0.008f;
                v4.w = unit.Team == Team.TeamRed ? 1f : 2f;

                data[dataAdded] = v4;

                dataAdded++;
            }
        }

        if (artilleryImage.gameObject.activeInHierarchy)
        {
            var mat = artilleryImage.material;
            mat.SetVectorArray("_Data", data);
            mat.SetInt("_DataSize", dataAdded);
            minimapImage.SetMaterialDirty();
        }
        else
        {
            var mat = minimapImage.material;
            mat.SetVectorArray("_Data", data);
            mat.SetInt("_DataSize", dataAdded);
            minimapImage.SetMaterialDirty();
        }
    }
}
