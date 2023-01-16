using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public float MapSize;

    /*
    [SerializeField] int wTiles;
    [SerializeField] float width;

    public List<Vector3> Tiles = new List<Vector3>();

    void Awake()
    {
        var tileSize =  width / wTiles;
        for (int x = 1; x <= wTiles; x++)
        {
            for (int y = 1; y <= wTiles; y++)
            {
                Tiles.Add(transform.position 
                          + new Vector3((x * tileSize) - tileSize / 2, 0, (y * tileSize) - tileSize / 2) 
                          - (new Vector3(1, 0, 1) * (tileSize / 2f) * wTiles));
            }
        }
    }

    void Update()
    {
        foreach(var x in Tiles)
        {
            Debug.DrawLine(x, x + Vector3.up, Color.magenta);
        }
    }
    */
}
