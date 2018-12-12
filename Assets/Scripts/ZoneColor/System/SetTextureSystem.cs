using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Transforms;
using Unity.Collections;

//Update the texture on ZoneColorArray
public class SetTextureSystem : ComponentSystem {
    
    private Texture2D _mainTexGenerated;
    private Material _mat;
    private bool first = true;
    private int2 size;
    public struct ZoneColorGroup
    {
        public readonly int Length;
        public readonly int GroupIndex;
        public ComponentDataArray<ZoneColor> zoneColor;
    }

    public struct ZoneColorArrayGroup
    {
        public readonly int Length;
        public readonly int GroupIndex;
        public ComponentDataArray<ZoneColorArray> arrayColor;
    }

    [Inject] ZoneColorGroup _zoneColorGroup;
    [Inject] ZoneColorArrayGroup _zoneColorArrayGroup;

    protected override void OnCreateManager(int capacity)
    {
        base.OnCreateManager(capacity);
        _mat = Resources.Load<Material>("matZoneColor");
    }


    protected override void OnUpdate()
    {
        if (first)
        {
            size = _zoneColorArrayGroup.arrayColor[0].LengthArray;
            _mainTexGenerated = new Texture2D(_zoneColorArrayGroup.arrayColor[0].LengthArray.x, _zoneColorArrayGroup.arrayColor[0].LengthArray.y, TextureFormat.ARGB32, true);
            first = false;
            _mainTexGenerated.filterMode = FilterMode.Point;
        }
       for (int i = 0; i < _zoneColorGroup.Length; i++)
       {
           var zoneColori = _zoneColorGroup.zoneColor[i];
           _mainTexGenerated.SetPixel(size.x-zoneColori.index.x-1, size.y-zoneColori.index.y-1, new Color(zoneColori.color.x, zoneColori.color.y, zoneColori.color.z));
       }
        _mainTexGenerated.Apply();
        _mat.SetTexture("_MainTex", _mainTexGenerated);
    }

}
