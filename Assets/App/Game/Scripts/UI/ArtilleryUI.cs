using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ArtilleryUI : MonoBehaviour
{
    [SerializeField] ManagerContext context;

    [SerializeField] Terrain ter;
    [SerializeField] InputField a;
    [SerializeField] InputField b;

    public Action<Vector3> OnFire;

    public void Fire()
    {
        var aText = a.text.ToLower();
        var bText = b.text.ToLower();

        if(a.text.Length > 0 && b.text.Length > 0)
        {
            var aCord = (int)aText[0];
            var bCord = (int)bText[0];

            var chunkSize = (context.MapManager.MapSize / 2f) / 14f;
            var halfChunkSize = chunkSize / 2f;

            var mapSize = context.MapManager.MapSize - chunkSize;
            var halfMapSize = mapSize / 2f;


            if (97 <= aCord  && aCord <= 111 &&
                97 <= bCord && bCord <= 111)
            {
                var xPrec = (aCord - 97) / 14f;
                var yPrec = (bCord - 97) / 14f;

                var x = (xPrec * mapSize) - halfMapSize;
                var y = (yPrec * mapSize) - halfMapSize;

                var pos = new Vector3(x, 0, y);
                pos.y = ter.SampleHeight(pos);

                Debug.Log(pos);

                Debug.DrawLine(pos, pos + Vector3.up * 10, Color.red, 10);

                OnFire?.Invoke(pos);
                OnFire = null;
                Cancle();
            }
        }
    }

    public void Cancle()
    {
        context.UIManager.SetArilleryView(false);
    }
}
