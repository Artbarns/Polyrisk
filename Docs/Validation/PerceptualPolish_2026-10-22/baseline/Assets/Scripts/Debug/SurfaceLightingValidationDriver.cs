using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Linq;
using System.Reflection;
using PolyRisk.Multiplayer;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Profiling;
using Unity.Profiling;
using PolyRisk.Visuals;
#endif
namespace PolyRisk.DebugTools
{
    public sealed class SurfaceLightingValidationDriver : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Mount(){if(!Application.isBatchMode)new GameObject("[QA] Graphite").AddComponent<SurfaceLightingValidationDriver>();}
        public void Satin(){FindObjectOfType<BoardSurfaceLighting>()?.Apply(BoardVisualTheme.SurfaceTreatment.GraphiteSatin);Describe();}
        public void Reflective(){FindObjectOfType<BoardSurfaceLighting>()?.Apply(BoardVisualTheme.SurfaceTreatment.GraphiteReflectiveNeon);Describe();}
        public void Low(){QualitySettings.SetQualityLevel(1,true);Describe();}
        public void Medium(){QualitySettings.SetQualityLevel(2,true);Describe();}
        public void Describe()
        {
            var board=FindObjectOfType<PolyRiskBoard>();var presenter=FindObjectOfType<BoardSurfaceLighting>();
            var urp=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var volume=FindObjectOfType<Volume>();var bloom=volume!=null?volume.profile.components.OfType<Bloom>().FirstOrDefault():null;
            var exposure=volume!=null?volume.profile.components.OfType<ColorAdjustments>().FirstOrDefault():null;
            var camera=Camera.main;int lights=FindObjectsOfType<Light>().Count(l=>l.enabled);
            Debug.Log("GRAPHITE_QA "+JsonUtility.ToJson(new Status{treatment=presenter!=null?presenter.Treatment.ToString():"missing",
                quality=QualitySettings.names[QualitySettings.GetQualityLevel()],pipeline=urp!=null?urp.name:"missing",
                mainLight=urp!=null?urp.mainLightRenderingMode.ToString():"missing",srpBatcher=urp!=null&&urp.useSRPBatcher,
                width=Screen.width,height=Screen.height,indices=board!=null?board.GetComponentsInChildren<TileIdentifier>().Length:0,
                meshes=board!=null?board.GetComponentsInChildren<MeshFilter>().Length:0,lights=lights,
                realtimeProbes=FindObjectsOfType<ReflectionProbe>().Count(p=>p.enabled&&p.mode==ReflectionProbeMode.Realtime),
                bloom=bloom!=null?bloom.intensity.value:-1,exposure=exposure!=null?exposure.postExposure.value:-999,
                camera=camera!=null?camera.transform.position:Vector3.zero,angles=camera!=null?camera.transform.eulerAngles:Vector3.zero,
                size=camera!=null?camera.orthographicSize:0,finish=presenter!=null?presenter.Finish:null}));
        }
        static void Check(bool ok,string name){Debug.Log("GRAPHITE_CASE "+(ok?"PASS ":"FAIL ")+name);}
        public void SpecimenIdle(){foreach(var p in CompactPawnVisual.Instances)if(p!=null&&p.name.StartsWith("[QA] Material specimen"))p.SetActive(false,false);}
        public void SpecimenActive(){foreach(var p in CompactPawnVisual.Instances)if(p!=null&&p.name.StartsWith("[QA] Material specimen"))p.SetActive(true,false);}
        public void VerifyVariants()
        {
            var presenter=FindObjectOfType<BoardSurfaceLighting>();var board=FindObjectOfType<PolyRiskBoard>();
            var original=presenter.Treatment;var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;float size=camera.orthographicSize;
            var anchors=board.GetComponentsInChildren<TileIdentifier>().OrderBy(a=>a.TileIndex).ToArray();
            var poses=anchors.Select(a=>a.transform.position).ToArray();var boxes=anchors.Select(a=>a.GetComponent<BoxCollider>()).ToArray();
            var centers=boxes.Select(b=>b.center).ToArray();var sizes=boxes.Select(b=>b.size).ToArray();
            var meshIds=board.GetComponentsInChildren<MeshFilter>().Select(m=>m.sharedMesh.GetInstanceID()).ToArray();
            var stateField=typeof(PolyRiskBoard).GetField("statePixels",BindingFlags.NonPublic|BindingFlags.Instance);
            var state=(Color[])((Color[])stateField.GetValue(board)).Clone();
            var pawns=CompactPawnVisual.Instances.Where(p=>p!=null).ToArray();var roots=pawns.Select(p=>p.transform.position).ToArray();
            var sounds=pawns.Select(p=>p.AudibleContacts).ToArray();var locks=pawns.Select(p=>p.FinalLocks).ToArray();
            int quality=QualitySettings.GetQualityLevel();bool reduced=PolyRisk.UI.UIUXRuntime.ReducedMotion;
            var online=OnlineGameController.Instance;var snapshot=typeof(OnlineGameController).GetMethod("CaptureSnapshot",BindingFlags.Instance|BindingFlags.NonPublic);
            string economy=online!=null?JsonUtility.ToJson(snapshot.Invoke(online,null)):null;
            Check(anchors.Length==40&&anchors.Select(a=>a.TileIndex).SequenceEqual(Enumerable.Range(0,40)),"40 unique indices");
            foreach(var treatment in new[]{BoardVisualTheme.SurfaceTreatment.GraphiteSatin,BoardVisualTheme.SurfaceTreatment.GraphiteReflectiveNeon})
            {
                presenter.Apply(treatment);
                Check(camera.transform.position==position&&camera.transform.rotation==rotation&&camera.orthographicSize==size,treatment+" camera identical");
                Check(poses.SequenceEqual(anchors.Select(a=>a.transform.position))&&centers.SequenceEqual(boxes.Select(b=>b.center))&&sizes.SequenceEqual(boxes.Select(b=>b.size)),treatment+" 40 anchor collider contracts unchanged");
                Check(meshIds.SequenceEqual(board.GetComponentsInChildren<MeshFilter>().Select(m=>m.sharedMesh.GetInstanceID())),treatment+" same four fused mesh objects");
                Check(state.SequenceEqual((Color[])stateField.GetValue(board)),treatment+" all persistent state pixels unchanged");
                Check(roots.SequenceEqual(pawns.Select(p=>p.transform.position))&&sounds.SequenceEqual(pawns.Select(p=>p.AudibleContacts))&&locks.SequenceEqual(pawns.Select(p=>p.FinalLocks)),treatment+" pawn positions audio and locks unchanged");
                Check(economy!=null&&economy==JsonUtility.ToJson(snapshot.Invoke(online,null)),treatment+" actual full economic snapshot identical");
                Check(QualitySettings.GetQualityLevel()==quality&&PolyRisk.UI.UIUXRuntime.ReducedMotion==reduced,treatment+" quality accessibility preference unchanged");
                Physics.SyncTransforms();foreach(var a in anchors){RaycastHit hit;Check(a.GetComponent<BoxCollider>().Raycast(new Ray(a.transform.position+Vector3.up*2,Vector3.down),out hit,3)&&hit.collider.gameObject==a.gameObject,treatment+" raycast "+a.TileIndex);}
            }
            presenter.Apply(original);Describe();Debug.Log("GRAPHITE_VARIANTS_DONE");
        }
        [System.Serializable] sealed class Status
        {public string treatment,quality,pipeline,mainLight;public bool srpBatcher;public int width,height,indices,meshes,lights,realtimeProbes;public float bloom,exposure,size;public Vector3 camera,angles;public BoardVisualTheme.SurfaceFinish finish;}
        bool measuring;
        public void Measure(){if(!measuring)StartCoroutine(MeasureRoutine());}
        IEnumerator MeasureRoutine()
        {
            measuring=true;
            var draw=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count");
            var batches=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count");
            var passes=ProfilerRecorder.StartNew(ProfilerCategory.Render,"SetPass Calls Count");
            float start=Time.realtimeSinceStartup,sum=0,max=0;int frames=0;long maxDraw=0,maxBatches=0,maxPass=0;
            while(Time.realtimeSinceStartup-start<6)
            {
                yield return null;frames++;sum+=Time.unscaledDeltaTime;max=Mathf.Max(max,Time.unscaledDeltaTime);
                if(draw.Valid)maxDraw=System.Math.Max(maxDraw,draw.LastValue);
                if(batches.Valid)maxBatches=System.Math.Max(maxBatches,batches.LastValue);
                if(passes.Valid)maxPass=System.Math.Max(maxPass,passes.LastValue);
            }
            var result=new Measurement{quality=QualitySettings.names[QualitySettings.GetQualityLevel()],treatment=FindObjectOfType<BoardSurfaceLighting>().Treatment.ToString(),
                frames=frames,seconds=Time.realtimeSinceStartup-start,meanFrameMs=frames>0?sum/frames*1000:0,maxFrameMs=max*1000,
                renderCountersAvailable=draw.Valid&&maxDraw>0,drawCalls=maxDraw,batches=maxBatches,setPass=maxPass,
                totalAllocatedBytes=Profiler.GetTotalAllocatedMemoryLong(),totalReservedBytes=Profiler.GetTotalReservedMemoryLong()};
            draw.Dispose();batches.Dispose();passes.Dispose();measuring=false;
            Debug.Log("GRAPHITE_MEASURE "+JsonUtility.ToJson(result));
        }
        [System.Serializable] sealed class Measurement
        {public string quality,treatment;public int frames;public float seconds,meanFrameMs,maxFrameMs;public bool renderCountersAvailable;public long drawCalls,batches,setPass,totalAllocatedBytes,totalReservedBytes;}
#endif
    }
}
