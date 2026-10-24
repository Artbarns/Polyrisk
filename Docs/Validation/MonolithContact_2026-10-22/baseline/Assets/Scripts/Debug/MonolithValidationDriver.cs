using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PolyRisk.Visuals;
using PolyRisk.Systems;
using PolyRisk.UI;
using PolyRisk.Multiplayer;
using PolyRisk.Core;
#endif
namespace PolyRisk.DebugTools
{
    [DefaultExecutionOrder(100)]
    public sealed class MonolithValidationDriver : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [System.Serializable] sealed class Frame
        {
            public float t,vt,size;public Vector3 camera,rotation,root,visual,scale,offset;
            public bool moving,arriving,shared,reduced,playing;public int steps,contacts,locks;
        }
        bool recording,running;float epoch,next;
        CompactPawnVisual observed;
        readonly List<CompactPawnVisual> fixtures=new List<CompactPawnVisual>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Mount(){if(!Application.isBatchMode)new GameObject("[QA] Monolith").AddComponent<MonolithValidationDriver>();}
        public void CaptureMarker(){recording=true;epoch=Time.unscaledTime;Debug.Log("MONOLITH_CAPTURE_MARKER "+epoch.ToString("R",System.Globalization.CultureInfo.InvariantCulture));}
        public void EndCapture(){recording=false;}
        static void Check(bool ok,string name){Debug.Log("MONOLITH_CASE "+(ok?"PASS ":"FAIL ")+name+" t="+Time.unscaledTime.ToString("F4",System.Globalization.CultureInfo.InvariantCulture));}
        void LateUpdate()
        {
            if(!recording||Time.unscaledTime<next||Camera.main==null)return;
            next=Time.unscaledTime+.05f;
            var pawn=observed;
            if(pawn==null)foreach(var p in CompactPawnVisual.Instances)if(p.IsActive){pawn=p;break;}
            if(pawn==null||pawn.Visual==null)return;
            var c=Camera.main;
            Debug.Log("MONOLITH_FRAME "+JsonUtility.ToJson(new Frame{t=Time.unscaledTime,vt=Time.unscaledTime-epoch,
                size=c.orthographicSize,camera=c.transform.position,rotation=c.transform.eulerAngles,
                root=pawn.transform.position,visual=pawn.Visual.position,scale=pawn.Visual.lossyScale,offset=pawn.SharedOffset,
                moving=pawn.IsMoving,arriving=pawn.IsArriving,shared=pawn.IsShared,reduced=UIUXRuntime.ReducedMotion,
                playing=pawn.GetComponent<AudioSource>()?.isPlaying??false,steps=pawn.Crossings,contacts=pawn.AudibleContacts,locks=pawn.FinalLocks}));
        }
        public void RunAll(){if(!running)StartCoroutine(Run());}
        public void ArmAudio()
        {
            var pawn=FindObjectOfType<CompactPawnVisual>();if(pawn==null)return;
            var source=pawn.GetComponent<AudioSource>();source.volume=0;
            source.clip=Resources.Load<AudioClip>("PolyRiskAudio/contact_light");source.Play();
            Debug.Log("MONOLITH_AUDIO_ARMED silent output initialization");
        }
        public void ShowSpecimen()
        {
            if(running||TurnManager.Instance?.CurrentPlayer==null)return;
            var manager=FindObjectOfType<PlayerVisualManager>();
            var prefab=(GameObject)typeof(PlayerVisualManager).GetField("pawnPrefab",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(manager);
            var board=FindObjectOfType<PolyRiskBoard>();
            var root=Instantiate(prefab,board.transform.TransformPoint(new Vector3(0,.12f,0)),Quaternion.identity);
            root.name="[QA] Material specimen 5x";
            foreach(var col in root.GetComponents<Collider>())col.enabled=false;
            var p=root.AddComponent<CompactPawnVisual>();p.Initialize(0,1);
            p.tuning.heightRatio*=5;p.tuning.baseWidthRatio*=5;p.SetActive(true,false);p.AudioEnabled=false;
            observed=p;Debug.Log("MONOLITH_SPECIMEN enlarged 5x geometry only; gameplay camera unchanged");
        }
        IEnumerator Run()
        {
            running=true;
            var manager=FindObjectOfType<PlayerVisualManager>();var board=BoardManager.Instance;var rig=FindObjectOfType<BoardInteractionManager>();
            if(manager==null||board==null||TurnManager.Instance?.CurrentPlayer==null){Check(false,"prerequisites");yield break;}
            int actor=TurnManager.Instance.CurrentPlayer.PlayerId;
            var prefab=(GameObject)typeof(PlayerVisualManager).GetField("pawnPrefab",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(manager);
            var anchor=board.visualTiles[1];var box=anchor.GetComponent<BoxCollider>();float width=Mathf.Min(box.size.x*anchor.lossyScale.x,box.size.z*anchor.lossyScale.z);
            bool reduced=UIUXRuntime.ReducedMotion;UIUXRuntime.SetReducedMotion(false);
            for(int i=0;i<6;i++)
            {
                var root=Instantiate(prefab,board.visualTiles[10].position,Quaternion.identity);root.name="[QA] Monolith "+i;
                foreach(var col in root.GetComponents<Collider>())col.enabled=false;
                var p=root.AddComponent<CompactPawnVisual>();p.Initialize(i,width);p.SetActive(i==actor);fixtures.Add(p);
            }
            observed=fixtures[actor%6];
            yield return null;
            foreach(var p in fixtures)Check(p.Visual!=null&&p.transform.GetComponent<MeshRenderer>().enabled==false,"faceted visual replaces primitive player "+p.PlayerId);
            for(int i=0;i<6;i++)fixtures[i].gameObject.SetActive(i==actor%6);
            for(int index=0;index<40;index++)
            {
                observed.transform.position=board.visualTiles[index].position;observed.ResetPresentation();yield return new WaitForEndOfFrame();
                bool visible=true;
                foreach(var renderer in observed.Visual.GetComponentsInChildren<Renderer>())
                {var b=renderer.bounds;for(int n=0;n<8;n++){var p=new Vector3((n&1)==0?b.min.x:b.max.x,(n&2)==0?b.min.y:b.max.y,(n&4)==0?b.min.z:b.max.z);visible&=rig.BoardViewport.Contains(Camera.main.WorldToScreenPoint(p));}}
                Check(visible,"real monolith volume visible at anchor "+index);
            }
            foreach(int count in new[]{1,2,3,4,6})
            {
                for(int i=0;i<6;i++){fixtures[i].gameObject.SetActive(i<count);fixtures[i].transform.position=board.visualTiles[10].position;fixtures[i].ResetPresentation();}
                yield return new WaitForSecondsRealtime(.35f);
                bool separated=true;
                for(int a=0;a<count;a++)for(int b=a+1;b<count;b++)separated&=Vector3.Distance(fixtures[a].Visual.position,fixtures[b].Visual.position)>width*.28f;
                Check(separated,"shared tile distinct "+count+" occupants");
                for(int i=0;i<count;i++)Check((fixtures[i].transform.position-board.visualTiles[10].position).sqrMagnitude<.00001f,"shared offset never changes root "+count+" player "+i);
            }
            // One moderate active impulse, then a perfectly static idle.
            for(int i=0;i<6;i++)fixtures[i].gameObject.SetActive(i==actor%6);
            observed.SetActive(false);observed.SetActive(true);yield return new WaitForSecondsRealtime(.35f);
            Vector3 stable=observed.Visual.position;Quaternion rotation=observed.Visual.rotation;
            yield return new WaitForSecondsRealtime(.35f);
            Check(observed.Visual.position==stable&&observed.Visual.rotation==rotation,"no floating rotating or pulsing idle");
            observed.GlowEnabled=false;yield return new WaitForSecondsRealtime(.5f);Check(observed.Visual.gameObject.activeInHierarchy,"silhouette visible with emission disabled");observed.GlowEnabled=true;
            yield return PathCase(8,2,"two steps corner 10",false,false);
            yield return PathCase(18,6,"six steps corner 20",false,false);
            yield return PathCase(28,12,"twelve steps corners 30 0",false,false);
            yield return PathCase(8,6,"slow frames",false,true);
            yield return PathCase(8,2,"reduced motion",true,false);
            int clicks=observed.AudibleContacts,locks=observed.FinalLocks;
            observed.transform.position=board.visualTiles[20].position;yield return new WaitForSecondsRealtime(.3f);
            Check(clicks==observed.AudibleContacts&&locks==observed.FinalLocks&&!observed.IsMoving,"teleport pose has no tick or lock");
            var online=OnlineGameController.Instance;
            if(online!=null)
            {
                var capture=typeof(OnlineGameController).GetMethod("CaptureSnapshot",BindingFlags.Instance|BindingFlags.NonPublic);
                var apply=typeof(OnlineGameController).GetMethod("ApplyState",BindingFlags.Instance|BindingFlags.NonPublic);
                var snapshot=capture.Invoke(online,null);var dto=new GameStateDto{payloadJson=JsonUtility.ToJson(snapshot)};
                var path=board.GetPath(20,6);var motion=observed.GetComponent<PawnMovement>();int callbacks=0,realCallbacks=0;
                var realPawn=GameObject.Find("Pawn_Player_"+actor).GetComponent<PawnMovement>();
                Vector3 realRest=realPawn.transform.position;
                realPawn.MoveAlongPath(board.GetPath(TurnManager.Instance.GetPlayerPosition(TurnManager.Instance.CurrentPlayer),6),()=>realCallbacks++);
                PlayerVisualManager.NotifyVisualMovementStarted(actor,motion,path);motion.MoveAlongPath(path,()=>callbacks++);
                yield return new WaitForSecondsRealtime(.09f);
                bool applied=(bool)apply.Invoke(online,new object[]{dto});motion.StopAllCoroutines();
                clicks=observed.AudibleContacts;locks=observed.FinalLocks;
                yield return new WaitForSecondsRealtime(.4f);
                Check(applied&&!observed.IsMoving&&observed.AudibleContacts==clicks&&observed.FinalLocks==locks&&callbacks==0,"real local ApplyState cancels presentation without replay or callback");
                Check(realCallbacks==0&&Vector3.Distance(realPawn.transform.position,realRest)<.0001f,"production pawn old coroutine stopped after replicated pose");
            }
            else Check(false,"ApplyState available");
            UIUXRuntime.SetReducedMotion(reduced);rig.RestoreOverview();
            foreach(var p in fixtures)Destroy(p.gameObject);fixtures.Clear();observed=null;
            running=false;Debug.Log("MONOLITH_FIXTURES_DONE");
#if UNITY_WEBGL && !UNITY_EDITOR
            Application.ExternalCall("monolithFixtureDone");
#endif
        }
        IEnumerator PathCase(int start,int steps,string name,bool reduced,bool slow)
        {
            UIUXRuntime.SetReducedMotion(reduced);var board=BoardManager.Instance;var rig=FindObjectOfType<BoardInteractionManager>();rig.RestoreOverview();
            observed.transform.position=board.visualTiles[start].position;observed.ResetPresentation();yield return null;
            var motion=observed.GetComponent<PawnMovement>();var path=board.GetPath(start,steps);int actor=TurnManager.Instance.CurrentPlayer.PlayerId;
            int before=observed.Crossings,sounds=observed.AudibleContacts,locks=observed.FinalLocks,callbacks=0;
            float began=Time.unscaledTime,maxLift=0,minDt=10;int frames=0;Vector3 camera=Camera.main.transform.position;
            PlayerVisualManager.NotifyVisualMovementStarted(actor,motion,path);
            motion.MoveAlongPath(path,()=>{callbacks++;rig.CompleteVisualMovement(motion.transform);});
            while(callbacks==0&&Time.unscaledTime-began<8f)
            {
                if(slow){var clock=System.Diagnostics.Stopwatch.StartNew();while(clock.ElapsedMilliseconds<130){} }
                yield return new WaitForEndOfFrame();
                frames++;if(!slow||frames>1)minDt=Mathf.Min(minDt,Time.unscaledDeltaTime);
                maxLift=Mathf.Max(maxLift,observed.Visual.position.y-board.visualTiles[start].position.y);
            }
            yield return null;
            Check(callbacks==1&&(motion.transform.position-path[path.Count-1]).sqrMagnitude<.00001f,name+" original callback once exact destination");
            Check(observed.Crossings-before==steps,name+" one observed crossing per reached waypoint");
            Check(observed.FinalLocks-locks==1,name+" one final lock replaces intermediate tick");
            Check(observed.AudibleContacts-sounds<=steps,name+" no extra or catchup contacts");
            Check(maxLift<=.065f,name+" child glide is low without hop max="+maxLift.ToString("F4"));
            Check(!reduced||Camera.main.transform.position==camera,name+" reduced camera preserved");
            if(slow)Check(minDt>.10f,name+" actual frame interval above100ms min="+minDt.ToString("F4"));
            yield return new WaitForSecondsRealtime(.8f);
            int quiet=observed.AudibleContacts;yield return new WaitForSecondsRealtime(.15f);
            Check(quiet==observed.AudibleContacts&&!observed.IsArriving&&!observed.IsMoving,name+" stable after lock no repeated sound");
            Debug.Log("MONOLITH_PATH "+name+" frames="+frames+" duration="+(Time.unscaledTime-began).ToString("F4"));
        }
#endif
    }
}
