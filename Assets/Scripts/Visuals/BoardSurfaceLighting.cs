using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PolyRisk.Visuals
{
    /// <summary>One event-driven finish system for both treatments. No camera or game-state writes.</summary>
    [DefaultExecutionOrder(95)]
    public sealed class BoardSurfaceLighting : MonoBehaviour
    {
        public BoardVisualTheme.SurfaceTreatment Treatment { get; private set; }
        public BoardVisualTheme.SurfaceFinish Finish { get; private set; }
        PolyRiskBoard board;
        Material[] surfaces;
        Light keyLight;
        Quaternion originalKeyRotation;
        bool environmentCaptured;
        Bloom bloom;
        float baselineBloom;
        public int CalibrationStage { get; private set; } = 4;
        Vector4 lastFoot=new Vector4(-999,-999,0,0),lastColor;
        static readonly int FootId=Shader.PropertyToID("_PawnFoot"),ColorId=Shader.PropertyToID("_PawnFootColor");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Mount()
        {
            var board=FindObjectOfType<PolyRiskBoard>();
            if(board!=null&&board.visualTheme!=null&&board.GetComponent<BoardSurfaceLighting>()==null)
                board.gameObject.AddComponent<BoardSurfaceLighting>();
        }
        void Start()
        {
            board=GetComponent<PolyRiskBoard>();
            if(board!=null&&board.visualTheme!=null)Apply(board.visualTheme.surfaceTreatment);
        }
        public void Apply(BoardVisualTheme.SurfaceTreatment treatment)
        {
            if(board==null)board=GetComponent<PolyRiskBoard>();
            if(board==null||board.visualTheme==null)return;
            var theme=board.visualTheme;Treatment=treatment;Finish=theme.Finish(treatment);
            CalibrationStage=4;
            board.RefreshSurfaceMaterials();
            if(surfaces==null)surfaces=new[]{board.metalMaterial,board.tileMaterial,board.darkMaterial};
            var metalFilter=board.transform.Find("Board_Metal")?.GetComponent<MeshFilter>();
            float extent=metalFilter!=null?metalFilter.sharedMesh.bounds.extents.x:7f;
            for(int i=0;i<surfaces.Length;i++)
            {
                var material=surfaces[i];if(material==null)continue;bool tile=i==1;
                material.SetFloat("_Polish",Finish.architectural?2:1);
                material.SetFloat("_PawnSurface",0);
                material.SetFloat("_BoardDark",i==2?1:0);
                material.SetFloat("_CornerGrow",board.settings.cornerGrow);
                material.SetFloat("_CornerTileSize",board.settings.cellPitchLong-board.settings.gap+board.settings.cornerGrow);
                material.SetVector("_StudioKey",theme.architecturalLight.studioKey);
                material.SetVector("_StudioFill",theme.architecturalLight.studioFill);
                material.SetVector("_ArchitecturalResponse",theme.architecturalLight.surfaceResponse);
                material.SetColor("_SurfaceTint",tile?Finish.tileTint:Finish.boardTint);
                material.SetFloat("_Metallic",tile?Finish.tileMetallic:Finish.boardMetallic);
                material.SetFloat("_Smoothness",tile?Finish.tileSmoothness:Finish.boardSmoothness);
                material.SetFloat("_StudioStrength",i==2?(Finish.architectural?Finish.studioReflection*.78f:.045f):Finish.studioReflection);
                material.SetFloat("_EdgeSpecular",Finish.edgeSpecular);
                material.SetFloat("_BackgroundSeparation",Finish.backgroundSeparation);
                material.SetFloat("_BoardHalfExtent",extent);
                material.SetFloat("_BandBoost",theme.neonBoost*Finish.neonEmission);
                material.SetFloat("_ReflectionStrength",tile||(Finish.architectural&&i==2)?Finish.neonReflection:0);
                material.SetFloat("_ReflectionFalloff",Finish.reflectionFalloff);
                material.SetFloat("_ReflectionReach",Finish.reflectionReach);
                material.SetFloat("_ContactReflectionStrength",Finish.contactReflection);
                material.SetFloat("_ArrivalReflectionStrength",Finish.arrivalReflection);
                material.SetFloat("_ContactOutlineGain",Finish.contactOutline);
                material.SetFloat("_ContactReflectionFalloff",Finish.contactReflectionFalloff);
                material.SetFloat("_ContactReflectionReach",Finish.contactReflectionReach);
                material.SetFloat("_ActiveSurfaceContrast",Finish.activeSurfaceContrast);
                if(Finish.architectural)material.SetFloat("_MinimumBandPixels",1.8f);
            }
            if(board.neonMaterial!=null)
            {
                board.neonMaterial.SetFloat("_Boost",theme.neonBoost*Finish.neonEmission);
                board.neonMaterial.SetFloat("_ContactGain",Finish.contactOutline);
                board.neonMaterial.SetFloat("_Polish",Finish.architectural?2:1);
                board.neonMaterial.SetFloat("_SourceCore",.08f);
            }
            if(Application.isPlaying)
            {
                CaptureEnvironment();
                ApplyEnvironment(Finish.architectural);
            }
            foreach(var pawn in CompactPawnVisual.Instances)if(pawn!=null)pawn.ApplyLightingFinish(Finish);
            lastFoot=new Vector4(-999,-999,0,0);
        }
        void CaptureEnvironment()
        {
            if(environmentCaptured)return;
            foreach(var light in FindObjectsOfType<Light>())
                if(light.enabled&&light.type==LightType.Directional){keyLight=light;break;}
            if(keyLight!=null)originalKeyRotation=keyLight.transform.rotation;
            var volume=FindObjectOfType<Volume>();
            if(volume!=null&&volume.profile!=null&&volume.profile.TryGet(out bloom))baselineBloom=bloom.intensity.value;
            environmentCaptured=true;
        }
        void ApplyEnvironment(bool architectural)
        {
            var theme=board.visualTheme;var light=theme.architecturalLight;
            RenderSettings.ambientMode=architectural?AmbientMode.Trilight:AmbientMode.Flat;
            if(architectural)
            {
                RenderSettings.ambientSkyColor=light.ambientSky;
                RenderSettings.ambientEquatorColor=light.ambientEquator;
                RenderSettings.ambientGroundColor=light.ambientGround;
            }
            else RenderSettings.ambientLight=theme.fillColor;
            if(keyLight!=null)
            {
                keyLight.transform.rotation=architectural?Quaternion.Euler(light.keyEuler):originalKeyRotation;
                keyLight.color=architectural?light.keyColor:theme.keyColor;
                keyLight.intensity=architectural?light.keyIntensity:theme.keyIntensity;
            }
            if(bloom!=null)bloom.intensity.value=architectural?light.bloom:baselineBloom;
        }
        /// <summary>Calibration layers alter presentation only, on the existing materials.</summary>
        public void ApplyArtCalibration(bool baseline,int stage)
        {
            Apply(baseline?BoardVisualTheme.SurfaceTreatment.GraphiteReflectiveNeon:BoardVisualTheme.SurfaceTreatment.ArchitecturalGraphite);
            if(baseline)return;
            CalibrationStage=Mathf.Clamp(stage,0,4);
            if(CalibrationStage<4)
            {
                // A calibration event may allocate a small profile copy; ordinary
                // gameplay never does. Keep the authored theme and baseline immutable.
                var stagedFinish=new BoardVisualTheme.SurfaceFinish();
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(Finish),stagedFinish);
                Finish=stagedFinish;
                if(CalibrationStage<1){Finish.pawnStudioReflection=0;Finish.pawnEdgeSpecular=0;}
                if(CalibrationStage<2)Finish.pawnEmission=0;
                if(CalibrationStage<3)Finish.pawnBaseReflection=0;
                foreach(var pawn in CompactPawnVisual.Instances)if(pawn!=null)pawn.ApplyLightingFinish(Finish);
            }
            if(CalibrationStage==0&&Application.isPlaying)ApplyEnvironment(false);
            if(bloom!=null)bloom.intensity.value=CalibrationStage<4?0:board.visualTheme.architecturalLight.bloom;
            foreach(var material in surfaces)
            {
                if(material==null)continue;
                if(CalibrationStage<1)
                {
                    material.SetFloat("_StudioStrength",0);material.SetFloat("_EdgeSpecular",0);
                    material.SetFloat("_EdgeNormalStrength",0);material.SetFloat("_BackgroundSeparation",0);
                    material.SetVector("_ArchitecturalResponse",new Vector4(0,0,0,0));
                }
                if(CalibrationStage<2){material.SetFloat("_BandBoost",0);material.SetFloat("_ContactOutlineGain",0);}
                if(CalibrationStage<3)
                {
                    material.SetFloat("_ReflectionStrength",0);material.SetFloat("_ContactReflectionStrength",0);
                    material.SetFloat("_ArrivalReflectionStrength",0);material.SetVector(FootId,Vector4.zero);
                }
            }
            if(board.neonMaterial!=null&&CalibrationStage<2)
            {board.neonMaterial.SetFloat("_Boost",0);board.neonMaterial.SetFloat("_ContactGain",0);}
            lastFoot=new Vector4(-999,-999,0,0);
        }
        void LateUpdate()
        {
            if(Finish==null||board==null)return;
            CompactPawnVisual active=null;
            foreach(var pawn in CompactPawnVisual.Instances)
            {
                if(pawn==null||!pawn.isActiveAndEnabled||pawn.Visual==null)continue;
                if(pawn.LightingFinish!=Finish)pawn.ApplyLightingFinish(Finish);
                if(pawn.IsActive&&(active==null||pawn.IsMoving||pawn.IsArriving||(!active.IsMoving&&!active.IsArriving)))active=pawn;
            }
            Vector4 foot=Vector4.zero,color=Vector4.zero;
            if(active!=null&&Finish.pawnBaseReflection>0&&active.GlowEnabled&&(!Finish.architectural||CalibrationStage>=3))
            {
                Vector3 local=board.transform.InverseTransformPoint(active.Visual.position);
                float radius=board.transform.InverseTransformVector(Vector3.right*active.Visual.lossyScale.x*.66f).magnitude;
                float pulse=active.IsArriving&&!PolyRisk.UI.UIUXRuntime.ReducedMotion?1-Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.unscaledTime-active.LastLockTime)/active.tuning.arrivalPulseDuration)):0;
                foot=new Vector4(local.x,local.z,Mathf.Max(.01f,radius),Finish.pawnBaseReflection*(1+pulse*.65f));
                color=PlayerVisualManager.GetPlayerColor(active.PlayerId);
            }
            if(foot==lastFoot&&color==lastColor)return;
            foreach(var material in surfaces)if(material!=null){material.SetVector(FootId,foot);material.SetVector(ColorId,color);}
            lastFoot=foot;lastColor=color;
        }
        void OnDisable()
        {
            if(surfaces!=null)foreach(var material in surfaces)if(material!=null)material.SetVector(FootId,Vector4.zero);
        }
    }
}
