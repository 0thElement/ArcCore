using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Gameplay.Utility;
using ArcCore.Gameplay.Components;
using ArcCore.Parsing.Data;
using ArcCore.Gameplay.Components.Chunk;
using ArcCore.Utilities.Extensions;
using Unity.Rendering;

namespace ArcCore.Gameplay.Behaviours.EntityCreation
{
    public class HoldEntityCreator : ECSMonoBehaviour
    {
        public static HoldEntityCreator Instance { get; private set; }
        [SerializeField] private GameObject holdNotePrefab;
        private Entity holdNoteEntityPrefab;

        //Temporary solution, will be refactored when proper skinning is implemented
        [HideInInspector] public RenderMesh HighlightRenderMesh, GrayoutRenderMesh;
        private void Awake()
        {
            Instance = this;
            holdNoteEntityPrefab = GameObjectConversionSettings.ConvertToNote(holdNotePrefab, EntityManager);

            RenderMesh holdRenderMesh = EntityManager.GetSharedComponentData<RenderMesh>(holdNoteEntityPrefab);

            Material highlightMaterial = Instantiate(holdRenderMesh.material);
            Material grayoutMaterial = Instantiate(holdRenderMesh.material);

            var highlightShaderID = Shader.PropertyToID("_Highlight");
            highlightMaterial.SetFloat(highlightShaderID, 1);
            grayoutMaterial.SetFloat(highlightShaderID, -1);

            
            HighlightRenderMesh = new RenderMesh {
                mesh = holdRenderMesh.mesh,
                material = highlightMaterial
            };

            GrayoutRenderMesh = new RenderMesh {
                mesh = holdRenderMesh.mesh,
                material = grayoutMaterial
            };
        }

        public unsafe void CreateEntities(List<HoldRaw> affHoldList)
        {
            affHoldList.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });

            foreach (HoldRaw hold in affHoldList)
            {
                //Main entity
                Entity holdEntity = EntityManager.Instantiate(holdNoteEntityPrefab);

                float x = Conversion.TrackToX(hold.track);
                const float y = 0;
                const float z = 0;

                const float scalex = 1;
                const float scaley = 1;

                float endFloorPosition = Conductor.Instance.GetFloorPositionFromTiming(hold.endTiming, hold.timingGroup);
                float startFloorPosition = Conductor.Instance.GetFloorPositionFromTiming(hold.timing, hold.timingGroup);
                float scalez = - endFloorPosition + startFloorPosition;

                EntityManager.SetComponentData<Translation>(holdEntity, new Translation(){
                    Value = new float3(x, y, z)
                });
                EntityManager.AddComponentData<NonUniformScale>(holdEntity, new NonUniformScale(){
                    Value = new float3(scalex, scaley, scalez)
                });
                EntityManager.SetComponentData<BaseLength>(holdEntity, new BaseLength(scalez));

                EntityManager.SetComponentData<FloorPosition>(holdEntity, new FloorPosition(startFloorPosition));
                EntityManager.SetComponentData<TimingGroup>(holdEntity, new TimingGroup(hold.timingGroup));
                EntityManager.SetComponentData<ChartTime >(holdEntity, new ChartTime{value = hold.timing});

                EntityManager.SetComponentData(holdEntity, new ChartLane(hold.track));

                //Appear and disappear time
                int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(startFloorPosition + Constants.RenderFloorPositionRange, 0);
                int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(endFloorPosition - Constants.RenderFloorPositionRange, 0);
                int appearTime = (t1 < t2) ? t1 : t2;

                EntityManager.SetComponentData<AppearTime>(holdEntity, new AppearTime(appearTime));

                //Judge entities
                float startBpm = Conductor.Instance.GetTimingEventFromTiming(hold.timing, hold.timingGroup).bpm;
                EntityManager.SetComponentData(holdEntity, ChartIncrTime.FromBpm(hold.timing, hold.endTiming, startBpm, out int comboCount));

                //Add combo
                ScoreManager.Instance.tracker.noteCount += comboCount;
            }
        }
    }

}