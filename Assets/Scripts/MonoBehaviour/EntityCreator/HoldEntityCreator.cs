﻿using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using Arcaoid.Utility;

public class HoldEntityCreator : MonoBehaviour
{
    public static HoldEntityCreator Instance { get; private set; }
    [SerializeField] private GameObject holdNotePrefab;
    private Entity holdNoteEntityPrefab;
    private World defaultWorld;
    private EntityManager entityManager;
    private void Awake()
    {
        Instance = this;
        defaultWorld = World.DefaultGameObjectInjectionWorld;
        entityManager = defaultWorld.EntityManager;
        GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(defaultWorld, null);
        holdNoteEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(holdNotePrefab, settings);
    }

    public void CreateEntities(List<AffHold> affHoldList)
    {
        foreach(AffHold hold in affHoldList)
        {
            Entity holdEntity = entityManager.Instantiate(holdNoteEntityPrefab);

            float x = Convert.TrackToX(hold.track);
            float y = 0;
            float z = 0;

            float scalex = 1.53f;
            float endFloorPosition = Conductor.Instance.GetFloorPositionFromTiming(hold.endTiming, hold.timingGroup);
            float startFloorPosition = Conductor.Instance.GetFloorPositionFromTiming(hold.timing, hold.timingGroup);
            float scaley = (endFloorPosition - startFloorPosition) / 3790f;
            float scalez = 1;

            entityManager.SetComponentData<Translation>(holdEntity, new Translation(){
                Value = new float3(x, y, z)
            });
            entityManager.SetComponentData<NonUniformScale>(holdEntity, new NonUniformScale(){
                Value = new float3(scalex, scaley, scalez)
            });
            entityManager.SetComponentData<FloorPosition>(holdEntity, new FloorPosition(){
                Value = startFloorPosition
            });
        }
    }
}
