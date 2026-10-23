using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Reflection;
using PolyRisk.Core;
using PolyRisk.Systems;
using PolyRisk.Visuals;
using PolyRisk.UI;
#endif
namespace PolyRisk.DebugTools
{
    /// <summary>Opt-in camera QA. All methods and automatic mounting are absent from Release.</summary>
    [DefaultExecutionOrder(120)]
    public sealed class CloseStrategicCameraValidationDriver : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        BoardInteractionManager rig; bool running,recording;float nextTrace;
        int checks,failed;
        [Serializable] sealed class CameraReadback
        {
            public float time,reference,size,factor;public Vector3 position,rotation,pawn;
            public string state;public bool manual,hold,reduced,expanded;public int active,logical,inspected;
            public Rect viewport;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Mount()
        {
            if(!Application.isBatchMode)new GameObject("[QA] CloseStrategic").AddComponent<CloseStrategicCameraValidationDriver>();
        }
        void Cache(){if(rig==null)rig=FindObjectOfType<BoardInteractionManager>();}
        public void Describe()
        {
            Cache();var camera=Camera.main;var turns=TurnManager.Instance;
            if(rig==null||camera==null||turns?.CurrentPlayer==null){Debug.Log("CLOSE_CAMERA_NOT_READY");return;}
            var pawn=GameObject.Find("Pawn_Player_"+turns.CurrentPlayer.PlayerId)?.transform;
            Debug.Log("CLOSE_CAMERA_READBACK "+JsonUtility.ToJson(new CameraReadback{
                time=Time.unscaledTime,reference=rig.StrategicReferenceOrthographicSize,size=camera.orthographicSize,
                factor=rig.StrategicReferenceOrthographicSize/camera.orthographicSize,
                position=camera.transform.position,rotation=camera.transform.eulerAngles,pawn=pawn!=null?pawn.position:Vector3.zero,
                state=rig.State.ToString(),manual=rig.IsManuallyExploring,hold=rig.IsArrivalHold,reduced=rig.ReducedMotionEnabled,
                expanded=rig.actionButtonsWidget!=null&&rig.actionButtonsWidget.IsCockpitExpanded,
                active=turns.CurrentPlayer.PlayerId,logical=turns.GetPlayerPosition(turns.CurrentPlayer),
                inspected=rig.actionButtonsWidget!=null?rig.actionButtonsWidget.InspectedTileIndex:-1,viewport=rig.BoardViewport}));
        }
        public void CaptureMarker(){recording=true;Describe();}
        public void EndCapture(){recording=false;Describe();}
        void LateUpdate(){if(recording&&Time.unscaledTime>=nextTrace){nextTrace=Time.unscaledTime+.05f;Describe();}}
        void Check(bool value,string name){checks++;if(!value)failed++;Debug.Log("CLOSE_CAMERA_CASE "+(value?"PASS ":"FAIL ")+name);}
        void Call(string name,params object[] args)=>typeof(BoardInteractionManager).GetMethod(name,Private).Invoke(rig,args);
        Vector2 Plane(Vector3 point)=>new Vector2(Vector3.Dot(point,Camera.main.transform.right),Vector3.Dot(point,Camera.main.transform.up));
        Vector2 Center()
        {
            var c=Camera.main;
            return Plane(c.transform.position)+(rig.BoardViewport.center-new Vector2(Screen.width,Screen.height)*.5f)*(c.orthographicSize*2/Screen.height);
        }
        public void RunAll(){if(!running)StartCoroutine(Fixtures());}
        IEnumerator Fixtures()
        {
            running=true;checks=failed=0;Cache();
            var board=BoardManager.Instance;var turns=TurnManager.Instance;var camera=Camera.main;
            if(rig==null||board?.visualTiles==null||turns?.CurrentPlayer==null||rig.BoardViewport.width<=0)
            {Check(false,"prerequisites");running=false;yield break;}
            rig.RestoreOverview();yield return null;
            float size=camera.orthographicSize;var pose=camera.transform.position;var rotation=camera.transform.rotation;
            int logical=turns.GetPlayerPosition(turns.CurrentPlayer);
            Check(board.visualTiles.Length==40,"40 anchors remain");
            Check(Mathf.Abs(rig.StrategicReferenceOrthographicSize/size-1.4f)<.00001f,"permanent x1.40 actual camera");
            Check(Mathf.Abs(camera.transform.eulerAngles.x-40)<.001f,"orthographic 40 degree elevation");
            Check(camera.orthographic,"orthographic projection preserved");
            var cockpit=rig.actionButtonsWidget;
            int oldInspection=cockpit!=null?cockpit.InspectedTileIndex:-1;
            bool oldExpanded=cockpit!=null&&cockpit.IsCockpitExpanded;
            if(cockpit!=null && cockpit.CanInspect)
            {
                var toggle=cockpit.GetType().GetMethod("TogglePulsePanel",Private);
                toggle.Invoke(cockpit,null);yield return new WaitForSecondsRealtime(.4f);
                Check(camera.transform.position==pose&&camera.orthographicSize==size,"cockpit toggle never moves camera");
                toggle.Invoke(cockpit,null);yield return new WaitForSecondsRealtime(.4f);
                Check(camera.transform.position==pose&&camera.orthographicSize==size,"cockpit restored without camera motion");
                // Ensure no UI shields the exploration center during these isolated fixtures.
                if(cockpit.IsCockpitExpanded)toggle.Invoke(cockpit,null);
                yield return new WaitForSecondsRealtime(.4f);
            }
            for(int i=0;i<40;i++)
            {
                Vector2 start=rig.BoardViewport.center;
                Vector2 wanted=Plane(board.visualTiles[i].position);
                Vector2 end=start+(Center()-wanted)*Screen.height/(2*size);
                Call("BeginPointer",start);Call("MovePointer",end);Call("EndPointer",end);
                yield return null;
                Vector3 screen=camera.WorldToScreenPoint(board.visualTiles[i].position);
                bool visible=rig.BoardViewport.Contains(screen);
                RaycastHit hit;bool ray=board.visualTiles[i].GetComponent<BoxCollider>().Raycast(camera.ScreenPointToRay(screen),out hit,1000f);
                Check(visible&&ray&&hit.collider.gameObject==board.visualTiles[i].gameObject,"bounded pan reaches collider "+i);
                var paused=camera.transform.position;
                yield return null;
                Check(paused==camera.transform.position&&camera.orthographicSize==size,"inspection exploration stable "+i);
                Check(camera.transform.rotation==rotation,"rotation unchanged "+i);
            }
            Check(rig.IsManuallyExploring,"manual exploration persists");
            // Reapplying an unchanged authoritative position must not steal exploration.
            var before=camera.transform.position;Call("HandlePresentationRestore");yield return null;
            Check(rig.IsManuallyExploring&&camera.transform.position==before,"same-position network restore preserves exploration");
            // Return to a tile that is clear of pawn hit targets and exercise the exact tap route.
            int target=31;Vector2 targetPlane=Plane(board.visualTiles[target].position);
            var origin=rig.BoardViewport.center;var delta=(Center()-targetPlane)*Screen.height/(2*size);
            Call("BeginPointer",origin);Call("MovePointer",origin+delta);Call("EndPointer",origin+delta);yield return null;
            if(cockpit!=null&&cockpit.CanInspect)
            {
                Vector2 point=camera.WorldToScreenPoint(board.visualTiles[target].position);
                Call("BeginPointer",point);Call("EndPointer",point);yield return null;
                Check(cockpit.InspectedTileIndex==target,"tap selects actual collider into inspector");
                int inspected=cockpit.InspectedTileIndex;
                Call("BeginPointer",rig.BoardViewport.center);Call("MovePointer",rig.BoardViewport.center+Vector2.right*48);
                Call("EndPointer",rig.BoardViewport.center+Vector2.right*48);yield return null;
                Check(cockpit.InspectedTileIndex==inspected,"drag cannot select a tile");
            }
            rig.RestoreOverview();yield return null;
            Check(!rig.IsManuallyExploring&&camera.orthographicSize==size,"explicit gameplay restore resumes close rest");
            Check(turns.GetPlayerPosition(turns.CurrentPlayer)==logical,"logical pawn position untouched");
            if(cockpit!=null&&cockpit.CanInspect)
            {
                if(oldInspection>=0)cockpit.InspectTile(board.GetTileAt(oldInspection));
                if(cockpit.IsCockpitExpanded!=oldExpanded)cockpit.GetType().GetMethod("TogglePulsePanel",Private).Invoke(cockpit,null);
            }
            var realPawn=GameObject.Find("Pawn_Player_"+turns.CurrentPlayer.PlayerId)?.GetComponent<PawnMovement>();
            if(realPawn!=null)
            {
                var originalPosition=realPawn.transform.position;var originalScale=realPawn.transform.localScale;
                bool reduced=UIUXRuntime.ReducedMotion;
                yield return MovementCase(realPawn,8,2,"short corner10",false);
                yield return MovementCase(realPawn,8,44,"long four corners",false);
                yield return MovementCase(realPawn,18,2,"Reduced Motion",true);
                UIUXRuntime.SetReducedMotion(reduced);
                realPawn.transform.position=originalPosition;realPawn.transform.localScale=originalScale;
                realPawn.GetComponent<CompactPawnVisual>()?.ResetPresentation();rig.RestoreOverview();
                yield return null;
            }
            else Check(false,"real production pawn available for motion fixture");
            running=false;Describe();Debug.Log("CLOSE_CAMERA_FIXTURES_DONE checks="+checks+" failed="+failed);
        }

        IEnumerator MovementCase(PawnMovement pawn,int start,int steps,string name,bool reduced)
        {
            var board=BoardManager.Instance;var turns=TurnManager.Instance;var camera=Camera.main;
            int logical=turns.GetPlayerPosition(turns.CurrentPlayer);
            UIUXRuntime.SetReducedMotion(reduced);
            pawn.transform.position=board.visualTiles[start].position;
            var visual=pawn.GetComponent<CompactPawnVisual>();visual?.ResetPresentation();
            rig.RestoreOverview();yield return null;
            Vector3 rest=camera.transform.position;float size=camera.orthographicSize;
            var path=board.GetPath(start,steps);int callbacks=0;float completed=-1;
            bool holdAtCallback=false;int visibleSamples=0,hiddenSamples=0;
            PlayerVisualManager.NotifyVisualMovementStarted(turns.CurrentPlayer.PlayerId,pawn,path);
            float began=Time.unscaledTime;
            pawn.MoveAlongPath(path,()=>{callbacks++;completed=Time.unscaledTime;rig.CompleteVisualMovement(pawn.transform);holdAtCallback=rig.IsArrivalHold;});
            while(callbacks==0&&Time.unscaledTime-began<45f)
            {
                yield return new WaitForEndOfFrame();
                if(!reduced && Time.unscaledTime-began>.45f)
                {
                    bool inside=true;
                    if(visual!=null && visual.Visual!=null)
                    foreach(var renderer in visual.Visual.GetComponentsInChildren<Renderer>())
                    {
                        var bounds=renderer.bounds;
                        for(int n=0;n<8;n++)
                        {
                            var corner=new Vector3((n&1)==0?bounds.min.x:bounds.max.x,(n&2)==0?bounds.min.y:bounds.max.y,(n&4)==0?bounds.min.z:bounds.max.z);
                            inside &= rig.BoardViewport.Contains(camera.WorldToScreenPoint(corner));
                        }
                    }
                    if(inside)visibleSamples++;else hiddenSamples++;
                }
                CheckSizeQuietly(size,ref hiddenSamples);
                if(reduced && callbacks==0 && camera.transform.position!=rest) hiddenSamples++;
            }
            Check(callbacks==1&&Vector3.Distance(pawn.transform.position,path[path.Count-1])<.001f,name+" original callback once exact destination");
            Check(turns.GetPlayerPosition(turns.CurrentPlayer)==logical,name+" economy/logical position not advanced by camera fixture");
            Check(reduced || (visibleSamples>0&&hiddenSamples==0),name+" monolith volume inside usable viewport through route");
            Check(Mathf.Abs(size-camera.orthographicSize)<.00001f,name+" no accumulated action zoom");
            if(!reduced)
            {
                Check(holdAtCallback,name+" hold begins with original completion");
                if(visual!=null)Check(Mathf.Abs(visual.LastLockTime-completed)<.001f,name+" magnetic lock stays on callback clock");
                Vector3 holdPose=camera.transform.position;
                while(rig.IsArrivalHold && Time.unscaledTime-completed<1f)
                {
                    yield return new WaitForEndOfFrame();
                    if(rig.IsArrivalHold)Check(camera.transform.position==holdPose,name+" hold pose stable");
                }
                float holdElapsed=Time.unscaledTime-completed;
                Check(holdElapsed>=.20f&&holdElapsed<.55f,name+" 250ms hold within frame quantization");
                while(rig.State!=BoardInteractionManager.CameraState.StrategicOverview&&Time.unscaledTime-completed<2f)yield return null;
                Check(rig.State==BoardInteractionManager.CameraState.StrategicOverview,name+" settles into close rest");
                Check(Mathf.Abs(camera.orthographicSize-size)<.00001f&&camera.transform.position!=rest,name+" close rest follows new visual destination");
                Vector3 projected=camera.WorldToScreenPoint(pawn.transform.position);
                Check(rig.BoardViewport.Contains(projected),name+" arrived pawn accessible in rest");
            }
            else
            {
                Check(hiddenSamples==0&&rig.State==BoardInteractionManager.CameraState.StrategicOverview,name+" Reduced Motion keeps camera fixed through interpolation");
                yield return null;
                Vector3 projected=camera.WorldToScreenPoint(pawn.transform.position);
                Check(rig.BoardViewport.Contains(projected)&&camera.orthographicSize==size,name+" Reduced Motion establishes new close rest at completion");
            }
            yield return new WaitForSecondsRealtime(.35f);
        }
        static void CheckSizeQuietly(float expected,ref int failures)
        {if(Mathf.Abs(Camera.main.orthographicSize-expected)>.00001f)failures++;}
#endif
    }
}
