using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PolyRisk.Core;
using PolyRisk.Systems;
using PolyRisk.Visuals;
using PolyRisk.UI;
using PolyRisk.Multiplayer;
#endif
namespace PolyRisk.DebugTools
{
    /// <summary>Opt-in development fixtures and timestamped evidence; never part of a Release player.</summary>
    public sealed class StrategicCameraValidationDriver : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Serializable] sealed class Frame
        {
            public float t,vt,size; public string state; public bool hold,visible,expanded,reduced;
            public Vector3 position,rotation,pawn; public Rect pawnPixels;
        }
        bool recording,running;float epoch,nextTrace;Transform fixturePawn,cachedPawn;int cachedId=-1;
        BoardInteractionManager rig;ActionButtonsWidget cockpit;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Mount()
        {
            if(Application.isBatchMode) return;
            new GameObject("[QA] StrategicCamera").AddComponent<StrategicCameraValidationDriver>();
        }
        public void CaptureMarker() { recording=true;epoch=Time.unscaledTime;Debug.Log("CAMERA_CAPTURE_MARKER "+epoch.ToString("R",System.Globalization.CultureInfo.InvariantCulture)); }
        public void EndCapture() { recording=false; }
        void LateUpdate()
        {
            if(!recording || Time.unscaledTime<nextTrace) return;
            nextTrace=Time.unscaledTime+.05f;
            if(rig==null)rig=FindObjectOfType<BoardInteractionManager>();
            if(cockpit==null)cockpit=FindObjectOfType<ActionButtonsWidget>();
            var camera=Camera.main;var turns=TurnManager.Instance;
            if(rig==null || camera==null || turns?.CurrentPlayer==null) return;
            if(cachedPawn==null || cachedId!=turns.CurrentPlayer.PlayerId)
            {cachedId=turns.CurrentPlayer.PlayerId;cachedPawn=GameObject.Find("Pawn_Player_"+cachedId)?.transform;}
            Transform pawn=rig.ObservedPawn!=null?rig.ObservedPawn:fixturePawn!=null?fixturePawn:cachedPawn;
            if(pawn==null)return;
            var renderer=pawn.GetComponentInChildren<Renderer>();Rect pixels=default;
            if(renderer!=null)
            {
                var b=renderer.bounds;Vector2 min=Vector2.positiveInfinity,max=Vector2.negativeInfinity;
                for(int n=0;n<8;n++)
                {
                    var v=new Vector3((n&1)==0?b.min.x:b.max.x,(n&2)==0?b.min.y:b.max.y,(n&4)==0?b.min.z:b.max.z);
                    Vector2 p=camera.WorldToScreenPoint(v);min=Vector2.Min(min,p);max=Vector2.Max(max,p);
                }
                pixels=Rect.MinMaxRect(min.x,min.y,max.x,max.y);
            }
            var f=new Frame{t=Time.unscaledTime,vt=Time.unscaledTime-epoch,state=rig.State.ToString(),hold=rig.IsArrivalHold,
                position=camera.transform.position,rotation=camera.transform.eulerAngles,size=camera.orthographicSize,pawn=pawn.position,
                pawnPixels=pixels,visible=rig.BoardViewport.Contains(pixels.min)&&rig.BoardViewport.Contains(pixels.max),
                expanded=cockpit!=null&&cockpit.IsCockpitExpanded,reduced=UIUXRuntime.ReducedMotion};
            Debug.Log("CAMERA_FRAME "+JsonUtility.ToJson(f));
        }
        public void RunAll() { if(!running)StartCoroutine(Fixtures()); }
        static void Check(bool ok,string name) { Debug.Log("CAMERA_CASE "+(ok?"PASS ":"FAIL ")+name+" t="+Time.unscaledTime.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)); }
        IEnumerator Fixtures()
        {
            running=true;rig=FindObjectOfType<BoardInteractionManager>();cockpit=FindObjectOfType<ActionButtonsWidget>();
            var turns=TurnManager.Instance;var board=BoardManager.Instance;var original=GameObject.Find("Pawn_Player_"+turns?.CurrentPlayer?.PlayerId);
            if(rig==null||original==null||board==null||rig.BoardViewport.width<=0){Check(false,"fixture prerequisites");running=false;yield break;}
            bool oldReduced=UIUXRuntime.ReducedMotion;
            int actor=turns.CurrentPlayer.PlayerId,position=turns.GetPlayerPosition(turns.CurrentPlayer);
            fixturePawn=Instantiate(original).transform;fixturePawn.name="[QA] Existing pawn clone";
            foreach(var col in fixturePawn.GetComponentsInChildren<Collider>())col.enabled=false;
            var motion=fixturePawn.GetComponent<PawnMovement>();
            var camera=Camera.main;Vector3 resting=camera.transform.position;float size=camera.orthographicSize;
            var toggle=cockpit.GetType().GetMethod("TogglePulsePanel",BindingFlags.Instance|BindingFlags.NonPublic);
            UIUXRuntime.SetReducedMotion(false);
            if(!cockpit.IsCockpitExpanded)toggle.Invoke(cockpit,null);
            yield return new WaitForSecondsRealtime(.35f);
            Check(camera.transform.position==resting&&camera.orthographicSize==size,"cockpit opens without camera reaction");
            yield return PathCase(motion,board,actor,8,2,"two steps across corner 10");
            Check(!cockpit.IsCockpitExpanded,"cockpit collapsed at visual start and stays compact");
            yield return PathCase(motion,board,actor,8,44,"long path corners 10 20 30 0");
            resting=camera.transform.position;size=camera.orthographicSize;
            turns.TeleportPlayer(turns.CurrentPlayer,20);yield return new WaitForSecondsRealtime(.25f);
            Check(rig.State==BoardInteractionManager.CameraState.StrategicOverview&&camera.transform.position==resting&&camera.orthographicSize==size,"real TeleportPlayer has no cinematic follow");
            turns.TeleportPlayer(turns.CurrentPlayer,position);
            // Exercise the real snapshot application path locally; no transport or server is involved.
            var online=OnlineGameController.Instance;
            if(online!=null)
            {
                var capture=typeof(OnlineGameController).GetMethod("CaptureSnapshot",BindingFlags.Instance|BindingFlags.NonPublic);
                var apply=typeof(OnlineGameController).GetMethod("ApplyState",BindingFlags.Instance|BindingFlags.NonPublic);
                var snapshot=capture.Invoke(online,null);var dto=new GameStateDto{payloadJson=JsonUtility.ToJson(snapshot)};
                fixturePawn.position=board.visualTiles[8].position;var path=board.GetPath(8,3);
                PlayerVisualManager.NotifyVisualMovementStarted(actor,motion,path);motion.MoveAlongPath(path,()=>rig.CompleteVisualMovement(fixturePawn));
                yield return new WaitForSecondsRealtime(.12f);
                bool restored=(bool)apply.Invoke(online,new object[]{dto});motion.StopAllCoroutines();
                yield return null;
                Check(restored&&rig.State==BoardInteractionManager.CameraState.StrategicOverview&&camera.transform.position==resting&&camera.orthographicSize==size,"real ApplyState cancels follow and restores overview instantly");
                rig.CompleteVisualMovement(fixturePawn);yield return new WaitForSecondsRealtime(.7f);
                Check(rig.State==BoardInteractionManager.CameraState.StrategicOverview,"late presentation completion ignored after restore");
            }
            else Check(false,"snapshot fixture requires OnlineGameController");
            UIUXRuntime.SetReducedMotion(true);
            yield return PathCase(motion,board,actor,18,2,"reduced motion camera stays overview");
            UIUXRuntime.SetReducedMotion(oldReduced);Destroy(fixturePawn.gameObject);fixturePawn=null;rig.RestoreOverview();
            Check(turns.GetPlayerPosition(turns.CurrentPlayer)==position,"fixture restores original logical position");
            running=false;Debug.Log("CAMERA_FIXTURES_DONE");
#if UNITY_WEBGL && !UNITY_EDITOR
            Application.ExternalCall("cameraFixtureDone");
#endif
        }
        IEnumerator PathCase(PawnMovement motion,BoardManager board,int actor,int start,int steps,string name)
        {
            rig.RestoreOverview();motion.transform.position=board.visualTiles[start].position;
            var path=board.GetPath(start,steps);int callbacks=0;float began=Time.unscaledTime;
            PlayerVisualManager.NotifyVisualMovementStarted(actor,motion,path);
            motion.MoveAlongPath(path,()=>{callbacks++;rig.CompleteVisualMovement(motion.transform);});
            bool reduced=UIUXRuntime.ReducedMotion;
            Check(reduced?rig.State==BoardInteractionManager.CameraState.StrategicOverview:rig.State==BoardInteractionManager.CameraState.MovementFocus,name+" start");
            while(callbacks==0&&Time.unscaledTime-began<18f)yield return null;
            float finished=Time.unscaledTime,arrivalSize=Camera.main.orthographicSize;Vector3 arrivalPosition=Camera.main.transform.position;
            Check(callbacks==1&&Vector3.Distance(motion.transform.position,path[path.Count-1])<.001f,name+" callback once and real destination");
            yield return new WaitForSecondsRealtime(.20f);
            Check(reduced?rig.State==BoardInteractionManager.CameraState.StrategicOverview:
                rig.IsArrivalHold&&Camera.main.orthographicSize==arrivalSize&&Camera.main.transform.position==arrivalPosition,name+" arrival hold has no second zoom/pan");
            while(rig.State!=BoardInteractionManager.CameraState.StrategicOverview&&Time.unscaledTime-finished<2f)yield return null;
            Check(rig.State==BoardInteractionManager.CameraState.StrategicOverview,name+" automatic overview");
            yield return new WaitForSecondsRealtime(.3f);
        }
#endif
    }
}
