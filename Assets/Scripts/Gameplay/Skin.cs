using System.Collections.Generic;
using UnityEngine;
using Unity.Rendering;
using ArcCore.Gameplay.Behaviours;
using ArcCore.Storage.Data;

namespace ArcCore.Gameplay
{
    public class Skin : MonoBehaviour
    {
        public static Skin Instance;
        private void Awake()
        {
            colorShaderId      = Shader.PropertyToID("_Color");
            highlightShaderId  = Shader.PropertyToID("_Highlight");
            blendStyleShaderId = Shader.PropertyToID("_BlendStyle");
            Instance = this;
        }
        public MeshRenderer trackRenderer;

        public Mesh tapMesh;
        public Mesh arctapMesh;
        public Mesh connectionLineMesh;
        public Mesh holdMesh;

        public Mesh arcMesh;
        public Mesh arcShadowMesh;
        public Mesh arcHeadMesh;
        public Mesh arcHeightMesh;

        public Material arcMaterial;
        public Material arcShadowMaterial;
        public Material arcHeightIndicatorMaterial;

        [Header("Light")]
        public Material tapLightMaterial;
        public Material arcTapLightMaterial;
        public Material connectionLineLightMaterial;
        public Material holdLightMaterial;
        public Material trackLightMaterial;
        public Material tapParticleLightMaterial;

        [Header("Conflict")]
        public Material tapConflictMaterial;
        public Material arcTapConflictMaterial;
        public Material connectionLineConflictMaterial;
        public Material holdConflictMaterial;
        public Material trackConflictMaterial;
        public Material tapParticleConflictMaterial;

        [Header("Blend")]
        public Material tapBlendMaterial;
        public Material arcTapBlendMaterial;
        public Material connectionLineBlendMaterial;
        public Material holdBlendMaterial;
        public Material trackBlendMaterial;
        public Material tapParticleBlendMaterial;


        [HideInInspector] public RenderMesh tapRenderMesh;
        [HideInInspector] public RenderMesh arctapRenderMesh;
        [HideInInspector] public RenderMesh connectionLineRenderMesh;
        [HideInInspector] public RenderMesh holdInitialRenderMesh;
        [HideInInspector] public RenderMesh holdHighlightRenderMesh;
        [HideInInspector] public RenderMesh holdGrayoutRenderMesh;
        [HideInInspector] public RenderMesh arcShadowRenderMesh;
        [HideInInspector] public RenderMesh arcShadowGrayoutRenderMesh;
        [HideInInspector] public Material trackMaterial;
        [HideInInspector] public Material tapParticleMaterial;
        [HideInInspector] public List<RenderMesh> arcInitialRenderMeshes = new List<RenderMesh>();
        [HideInInspector] public List<RenderMesh> arcHighlightRenderMeshes = new List<RenderMesh>();
        [HideInInspector] public List<RenderMesh> arcGrayoutRenderMeshes = new List<RenderMesh>();
        [HideInInspector] public List<RenderMesh> arcHeadRenderMeshes = new List<RenderMesh>();
        [HideInInspector] public List<RenderMesh> arcHeightRenderMeshes = new List<RenderMesh>();

        private int colorShaderId;
        private int highlightShaderId;
        private int blendStyleShaderId;

        public (RenderMesh, RenderMesh, RenderMesh, RenderMesh, RenderMesh) GetArcRenderMeshVariants(int color)
            => (Instance.arcInitialRenderMeshes[color],
                Instance.arcHighlightRenderMeshes[color],
                Instance.arcGrayoutRenderMeshes[color],
                Instance.arcHeadRenderMeshes[color],
                Instance.arcHeightRenderMeshes[color]);

        private RenderMesh GetRenderMesh(Mesh mesh, Material material)
        {
            return new RenderMesh
            {
                mesh = mesh,
                material = material
            };
        }

        private Color32 GetColorOrFirst(Color32[] colors, int index)
        {
            if (index >= 0 && index < colors.Length) return colors[index];
            else return colors[0];
        }

        public void ApplyStyle(Style style, int maxArcColor, Color32[] arcColors)
        {
            switch (style)
            {
                case Style.Light:
                    tapRenderMesh = GetRenderMesh(tapMesh, tapLightMaterial);
                    arctapRenderMesh = GetRenderMesh(arctapMesh, arcTapLightMaterial);
                    connectionLineRenderMesh = GetRenderMesh(connectionLineMesh, connectionLineLightMaterial);
                    holdInitialRenderMesh = GetRenderMesh(holdMesh, holdLightMaterial);
                    trackMaterial = trackLightMaterial;
                    tapParticleMaterial = tapParticleLightMaterial;
                    break;
                case Style.Conflict:
                    tapRenderMesh = GetRenderMesh(tapMesh, tapConflictMaterial);
                    arctapRenderMesh = GetRenderMesh(arctapMesh, arcTapConflictMaterial);
                    connectionLineRenderMesh = GetRenderMesh(connectionLineMesh, connectionLineConflictMaterial);
                    holdInitialRenderMesh = GetRenderMesh(holdMesh, holdConflictMaterial);
                    trackMaterial = trackConflictMaterial;
                    tapParticleMaterial = tapParticleConflictMaterial;
                    break;
                case Style.Blend:
                    tapRenderMesh = GetRenderMesh(tapMesh, tapBlendMaterial);
                    arctapRenderMesh = GetRenderMesh(arctapMesh, arcTapBlendMaterial);
                    connectionLineRenderMesh = GetRenderMesh(connectionLineMesh, connectionLineBlendMaterial);
                    holdInitialRenderMesh = GetRenderMesh(holdMesh, holdBlendMaterial);
                    trackMaterial = trackBlendMaterial;
                    tapParticleMaterial = tapParticleBlendMaterial;
                    BlendAll(0);
                    break;
            }

            trackRenderer.material = trackMaterial;

            for (int i = 0; i <= maxArcColor; i++)
            {
                Material arcColorMaterialInstance             = Object.Instantiate(arcMaterial);
                Material heightIndicatorColorMaterialInstance = Object.Instantiate(arcHeightIndicatorMaterial);

                arcColorMaterialInstance            .SetColor(colorShaderId, GetColorOrFirst(arcColors, i));
                heightIndicatorColorMaterialInstance.SetColor(colorShaderId, GetColorOrFirst(arcColors, i));

                Material highlightMat = Object.Instantiate(arcColorMaterialInstance);
                Material grayoutMat   = Object.Instantiate(arcColorMaterialInstance);

                highlightMat.SetFloat(highlightShaderId, 1);
                grayoutMat  .SetFloat(highlightShaderId,-1);

                arcInitialRenderMeshes  .Add(GetRenderMesh(arcMesh, arcColorMaterialInstance));
                arcHighlightRenderMeshes.Add(GetRenderMesh(arcMesh, highlightMat));
                arcGrayoutRenderMeshes  .Add(GetRenderMesh(arcMesh, grayoutMat));
                arcHeadRenderMeshes     .Add(GetRenderMesh(arcHeadMesh, arcColorMaterialInstance));
                arcHeightRenderMeshes   .Add(GetRenderMesh(arcHeightMesh, heightIndicatorColorMaterialInstance));
            }

            Material arcShadowGrayoutMaterial = Object.Instantiate(arcShadowMaterial);
            arcShadowGrayoutMaterial.SetFloat(highlightShaderId, -1);

            arcShadowRenderMesh = GetRenderMesh(arcShadowMesh, arcShadowMaterial);
            arcShadowGrayoutRenderMesh = GetRenderMesh(arcShadowMesh, arcShadowGrayoutMaterial);

            Material holdHighlightMaterial = Object.Instantiate(holdInitialRenderMesh.material);
            Material holdGrayoutMaterial   = Object.Instantiate(holdInitialRenderMesh.material);

            holdHighlightMaterial.SetFloat(highlightShaderId, 1);
            holdGrayoutMaterial  .SetFloat(highlightShaderId, -1);

            holdHighlightRenderMesh = GetRenderMesh(holdMesh, holdHighlightMaterial);
            holdGrayoutRenderMesh   = GetRenderMesh(holdMesh, holdGrayoutMaterial);

            PlayManager.ParticlePool.ApplyTapParticleMaterial(tapParticleMaterial);
        }

        public void BlendTap(float blend)
        { 
            tapBlendMaterial.SetFloat(blendStyleShaderId, blend);
        }
        public void BlendArctap(float blend)
        {
            arcTapBlendMaterial.SetFloat(blendStyleShaderId, blend);
        }
        public void BlendConnectionLine(float blend)
        {
            connectionLineBlendMaterial.SetFloat(blendStyleShaderId, blend);
        }
        public void BlendHold(float blend)
        {
            holdBlendMaterial.SetFloat(blendStyleShaderId, blend);
        }
        public void BlendTrack(float blend)
        {
            trackBlendMaterial.SetFloat(blendStyleShaderId, blend);
        }
        public void BlendParticle(float blend)
        {
            tapParticleBlendMaterial.SetFloat(blendStyleShaderId, blend);
        }
        public void BlendAll(float blend)
        {
            BlendTap(blend);
            BlendArctap(blend);
            BlendConnectionLine(blend);
            BlendHold(blend);
            BlendTrack(blend);
            BlendParticle(blend);
        }
    }
}