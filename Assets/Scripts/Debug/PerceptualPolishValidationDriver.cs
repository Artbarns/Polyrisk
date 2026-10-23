using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Linq;
using System.Reflection;
using PolyRisk.Visuals;
using PolyRisk.Systems;
#endif
namespace PolyRisk.DebugTools
{
    /// <summary>Development-only readback and isolated visual tests. No shipping gameplay writes.</summary>
    [DefaultExecutionOrder(110)]
    public sealed class PerceptualPolishValidationDriver : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        BoardInteractionManager rig;BoardContactFeedback contact;PolyRiskBoard board;
        bool recording,running;float epoch,next;
        static readonly BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Mount(){if(!Application.isBatchMode)new GameObject("[QA] Perceptual").AddComponent<PerceptualPolishValidationDriver>();}
        void Cache(){if(rig==null)rig=FindObjectOfType<BoardInteractionManager>();if(contact==null)contact=FindObjectOfType<BoardContactFeedback>();if(board==null)board=FindObjectOfType<PolyRiskBoard>();}
        public void CaptureMarker(){Cache();recording=true;epoch=Time.unscaledTime;}
        public void EndCapture(){recording=false;}
        [System.Serializable] sealed class Frame
        {public float t,vt,x,y,alpha,sum,reflection;public int index,active;public string state;public bool hold,reduced;public Rect viewport;}
        void LateUpdate()
        {
            if(!recording||Time.unscaledTime<next)return;next=Time.unscaledTime+.025f;Cache();
            if(rig==null||contact==null||board==null)return;
            var pawn=rig.ObservedPawn;float x=-1,y=-1;
            if(pawn!=null)
            {
                var ground=pawn.position;ground.y=BoardManager.Instance.visualTiles[0].position.y;
                var point=Camera.main.WorldToScreenPoint(ground);x=(point.x-rig.BoardViewport.x)/rig.BoardViewport.width;y=(point.y-rig.BoardViewport.y)/rig.BoardViewport.height;
            }
            var texture=(Texture2D)typeof(BoardContactFeedback).GetField("texture",Private).GetValue(contact);
            float sum=0;int active=0;for(int i=0;i<40;i++){float a=texture.GetPixel(i,0).a;sum+=a;if(a>.001f)active++;}
            var lighting=board.GetComponent<BoardSurfaceLighting>();
            Debug.Log("PERCEPTUAL_FRAME "+JsonUtility.ToJson(new Frame{t=Time.unscaledTime,vt=Time.unscaledTime-epoch,x=x,y=y,
                alpha=contact.LastIndex>=0?texture.GetPixel(contact.LastIndex,0).a:0,sum=sum,active=active,index=contact.LastIndex,
                reflection=lighting!=null?lighting.Finish.contactReflection:0,state=rig.State.ToString(),hold=rig.IsArrivalHold,reduced=rig.ReducedMotionEnabled,viewport=rig.BoardViewport}));
        }
        static void Check(bool ok,string name){Debug.Log("PERCEPTUAL_CASE "+(ok?"PASS ":"FAIL ")+name);}
        public void RunEnvelope(){if(!running)StartCoroutine(EnvelopeCases());}
        float Alpha(int i)=>((Texture2D)typeof(BoardContactFeedback).GetField("texture",Private).GetValue(contact)).GetPixel(i,0).a;
        IEnumerator EnvelopeCases()
        {
            running=true;Cache();
            var stateField=typeof(PolyRiskBoard).GetField("statePixels",Private);
            var persistent=(Color[])((Color[])stateField.GetValue(board)).Clone();
            var camera=Camera.main;var pose=camera.transform.position;float size=camera.orthographicSize;
            contact.Clear();float began=Time.unscaledTime;contact.PulseAt(BoardManager.Instance.visualTiles[31].position,Color.cyan,false,false);
            yield return new WaitForEndOfFrame();
            float age=Time.unscaledTime-began;
            float expected=BoardContactFeedback.Envelope(age,contact.tuning.tileContactAttackDuration,contact.tuning.tileContactDecayDuration)*contact.tuning.tileContactPulseIntensity;
            Check(Mathf.Abs(Alpha(31)-expected)<.005f,"actual texture matches continuous envelope at measured first-frame age");
            Debug.Log("PERCEPTUAL_FIRST_SAMPLE age="+age.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+" alpha="+Alpha(31).ToString("R",System.Globalization.CultureInfo.InvariantCulture));
            yield return new WaitForSecondsRealtime(.045f);yield return new WaitForEndOfFrame();
            Check(Alpha(31)>0&&Alpha(31)<=contact.tuning.tileContactPulseIntensity+.002f,"actual texture carries bounded progressive pulse");
            float before=Alpha(31);contact.PulseAt(BoardManager.Instance.visualTiles[31].position,Color.cyan,false,false);
            yield return new WaitForEndOfFrame();
            Check(Alpha(31)>=before-.02f,"retrigger preserves current intensity at attack start");
            for(int n=0;n<6;n++){contact.PulseAt(BoardManager.Instance.visualTiles[31+n].position,Color.cyan,false,false);yield return null;}
            yield return new WaitForSecondsRealtime(.45f);
            Check(!contact.HasActiveContacts&&Enumerable.Range(0,40).All(i=>Alpha(i)==0),"rapid contacts clear without residual light");
            contact.PulseAt(BoardManager.Instance.visualTiles[31].position,Color.cyan,true,true);
            yield return new WaitForSecondsRealtime(.05f);yield return new WaitForEndOfFrame();
            Check(Alpha(31)<=contact.tuning.tileArrivalPulseIntensity*contact.tuning.reducedMotionIntensity+.002f,"Reduced Motion retains lower bounded contact");
            yield return new WaitForSecondsRealtime(.4f);
            Check(!contact.HasActiveContacts,"arrival clears independently of callbacks");
            Check(persistent.SequenceEqual((Color[])stateField.GetValue(board)),"all persistent tile state pixels unchanged through retrigger stress");
            Check(camera.transform.position==pose&&camera.orthographicSize==size,"isolated contact never moves camera");
            running=false;Debug.Log("PERCEPTUAL_ENVELOPE_DONE");
        }
#endif
    }
}
