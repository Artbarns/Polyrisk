using UnityEngine;
using UnityEngine.Rendering;

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
            board.RefreshSurfaceMaterials();
            if(surfaces==null)surfaces=new[]{board.metalMaterial,board.tileMaterial,board.darkMaterial};
            var metalFilter=board.transform.Find("Board_Metal")?.GetComponent<MeshFilter>();
            float extent=metalFilter!=null?metalFilter.sharedMesh.bounds.extents.x:7f;
            for(int i=0;i<surfaces.Length;i++)
            {
                var material=surfaces[i];if(material==null)continue;bool tile=i==1;
                material.SetFloat("_Polish",1);
                material.SetColor("_SurfaceTint",tile?Finish.tileTint:Finish.boardTint);
                material.SetFloat("_Metallic",tile?Finish.tileMetallic:Finish.boardMetallic);
                material.SetFloat("_Smoothness",tile?Finish.tileSmoothness:Finish.boardSmoothness);
                material.SetFloat("_StudioStrength",i==2?.045f:Finish.studioReflection);
                material.SetFloat("_EdgeSpecular",Finish.edgeSpecular);
                material.SetFloat("_BackgroundSeparation",Finish.backgroundSeparation);
                material.SetFloat("_BoardHalfExtent",extent);
                material.SetFloat("_BandBoost",theme.neonBoost*Finish.neonEmission);
                material.SetFloat("_ReflectionStrength",tile?Finish.neonReflection:0);
                material.SetFloat("_ReflectionFalloff",Finish.reflectionFalloff);
                material.SetFloat("_ReflectionReach",Finish.reflectionReach);
                material.SetFloat("_ContactReflectionStrength",Finish.contactReflection);
                material.SetFloat("_ArrivalReflectionStrength",Finish.arrivalReflection);
                material.SetFloat("_ContactOutlineGain",Finish.contactOutline);
            }
            if(board.neonMaterial!=null)
            {
                board.neonMaterial.SetFloat("_Boost",theme.neonBoost*Finish.neonEmission);
                board.neonMaterial.SetFloat("_ContactGain",Finish.contactOutline);
            }
            if(Application.isPlaying)
            {
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=theme.fillColor;
                foreach(var light in FindObjectsOfType<Light>())if(light.type==LightType.Directional)
                {light.color=theme.keyColor;light.intensity=theme.keyIntensity;}
            }
            foreach(var pawn in CompactPawnVisual.Instances)if(pawn!=null)pawn.ApplyLightingFinish(Finish);
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
            if(active!=null&&Finish.pawnBaseReflection>0&&active.GlowEnabled)
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
