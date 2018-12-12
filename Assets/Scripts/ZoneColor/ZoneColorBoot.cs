using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

public class ZoneColorBoot : MonoBehaviour {
    public Mesh _meshMap;
    public Material _matMap;
    [Space]
    public Mesh _meshFirefly;
    public Material _matFirefly;
    [Space]
    public int2 _zoneArraySize;
    [Range(0,5)]
    public float _spawnRadius;
    public Texture2D _filterTexReference;
    public int _fireflyCount;
    public float2 _fireflyRadiusRange;
    [Space]
    private Texture2D _filterTexGenerated;
    private EntityManager entityManager;

    // Use this for initialization

    void createFilterTex(){
        _filterTexGenerated = new Texture2D(_zoneArraySize.x*_filterTexReference.width, _zoneArraySize.y*_filterTexReference.height, _filterTexReference.format, true); 
        Color[] colorReference = _filterTexReference.GetPixels();
        for (int i = 0; i < _zoneArraySize.x; i++)
            for (int j = 0; j < _zoneArraySize.y; j++)
                _filterTexGenerated.SetPixels(
                        (i * _filterTexReference.width),
                        (j * _filterTexReference.height),
                        _filterTexReference.width,
                        _filterTexReference.height, colorReference
                        );
        _filterTexGenerated.Apply();
        _matMap.SetTexture("_filterTex", _filterTexGenerated);
    }

    public void CreateZoneArray(EntityArchetype zoneArrayArchetype)
    {
        Entity zoneArrayEntity;
        zoneArrayEntity = entityManager.CreateEntity(zoneArrayArchetype);
        //set the position to 0
        entityManager.SetComponentData(zoneArrayEntity, new Position { Value = new float3(0,0,1f) });
        //init the direction of the entity
        ZoneColorArray zoneColorArray = new ZoneColorArray
        {
            LengthArray = _zoneArraySize
        };
        Rotation rot = new Rotation() { Value = math.normalize(new quaternion(-1,0,0,1)) };
        entityManager.SetComponentData(zoneArrayEntity, rot);
        //set the CelestialBody
        entityManager.SetComponentData(zoneArrayEntity, zoneColorArray);
        //set the shared MeshInstanceRenderer
        entityManager.SetSharedComponentData(zoneArrayEntity, new MeshInstanceRenderer
        {
            mesh = _meshMap,
            material = _matMap
        });
    }

    public void CreateZoneColor(EntityArchetype zoneColorArchetype, EntityArchetype fireflyArchetype)
    {
        //offset is usefull to set the position of the zone on the center
        float2 offset = new float2(5f/_zoneArraySize.x-5f, 5f/_zoneArraySize.y-5f);
        for (int i = 0; i < _zoneArraySize.x; i++)
            for (int j = 0; j < _zoneArraySize.y; j++){
                Entity zoneColorEntity;
                zoneColorEntity = entityManager.CreateEntity(zoneColorArchetype);
                ZoneColor zoneC = new ZoneColor
                {
                    index = new int2(i,j),
                    position = new float2(10*i/ (float)_zoneArraySize.x + offset.x, 10*j/(float)_zoneArraySize.y+ offset.y),
                    color = new float3(1,1,1)
                };
                entityManager.SetComponentData(zoneColorEntity, zoneC);
            }
    }

    public void Createfirefly(EntityArchetype fireflyArchetype)
    {
        for (int i = 0; i < _fireflyCount; i++)
        {
            Entity fireflyEntity;
            fireflyEntity = entityManager.CreateEntity(fireflyArchetype);
            //define random color
            float fl1, fl2;
            fl1 = Random.Range(0f, 1f);
            fl2 = 1f-fl1;
            float3 tmpcolor;
            if (i % 3 == 0)
                tmpcolor = new float3(fl1, fl2, 0);
            else if (i % 3 == 1)
                tmpcolor = new float3(fl1, 0, fl2);
            else
                tmpcolor = new float3(0, fl2, fl1);
            //Firefly struct creation
            Firefly firefly = new Firefly
            {
                position = new float2(Random.Range(-_spawnRadius, _spawnRadius), Random.Range(-_spawnRadius, _spawnRadius)),
                color = tmpcolor,
                radius = Random.Range(_fireflyRadiusRange.x, _fireflyRadiusRange.y)
            };
            //Scale struct creation
            Scale scale = new Scale
            {
                Value = new float3(0.1f, 0.1f, 0.1f)
            };
            entityManager.SetComponentData(fireflyEntity, scale);
            entityManager.SetComponentData(fireflyEntity, firefly);
            entityManager.SetComponentData(fireflyEntity,
            //TransformMatrix creation
            new TransformMatrix
            {
                Value = new float4x4(1f,0,0,0,
                                    0,1f,0,0,
                                    0,0,1f,0,
                                    0,0,0,1f)
            });
            entityManager.SetComponentData(fireflyEntity, new Position { Value = new float3(firefly.position,0) });
            entityManager.SetComponentData(fireflyEntity, new Heading { Value = new float3(Random.insideUnitCircle,0) });
            entityManager.SetComponentData(fireflyEntity, new MoveSpeed { speed = 0.01f });
            //Color each center with good color create too many MeshInstanceRenderer
            //Material matTmp = new Material(_matFirefly);
            //matTmp.color = new Color(firefly.color.x, firefly.color.y, firefly.color.z);
            MeshInstanceRenderer mesfRender = new MeshInstanceRenderer
            {
                mesh = _meshFirefly,
                material = _matFirefly
            };
            entityManager.SetSharedComponentData(fireflyEntity, mesfRender);
        }
    }

    void Start () {
        entityManager = World.Active.GetOrCreateManager<EntityManager>();
        createFilterTex();
        EntityArchetype zoneArrayArchetype = entityManager.CreateArchetype(typeof(ZoneColorArray), typeof(Position), typeof(Rotation), ComponentType.Create<TransformMatrix>(), ComponentType.Create<MeshInstanceRenderer>());
        CreateZoneArray(zoneArrayArchetype);
        EntityArchetype fireflyArchetype = entityManager.CreateArchetype(typeof(Firefly), typeof(Scale), typeof(Position), typeof(Heading), typeof(MoveSpeed), ComponentType.Create<TransformMatrix>(), ComponentType.Create<MeshInstanceRenderer>());
        Createfirefly(fireflyArchetype);
        EntityArchetype zoneColorArchetype = entityManager.CreateArchetype(typeof(ZoneColor));
        CreateZoneColor(zoneColorArchetype, fireflyArchetype);
    }

  
}
