using System.Collections.Generic;
using UnityEngine;
using PolyRisk.UI;
using PolyRisk.Multiplayer;
using PolyRisk.Systems;

namespace PolyRisk.Visuals
{
    /// <summary>
    /// Presentation only. Observes the actual waypoint snaps of PawnMovement;
    /// never writes its root, path, duration, callback or a PlayerModel.
    /// </summary>
    [DefaultExecutionOrder(80)]
    public sealed class CompactPawnVisual : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Tuning
        {
            public Color bodyColor = new Color(.24f,.29f,.34f);
            [Range(0,1)] public float metallic=.48f, smoothness=.44f;
            public float heightRatio=.62f, baseWidthRatio=.30f;
            public float emissionIntensity=.65f, activeEmissionMultiplier=1.85f, movingEmissionMultiplier=1.25f;
            public float baseRingIntensity=1.15f, baseRingActiveMultiplier=1.2f, topAccentIntensity=.8f;
            [Range(.07f,.38f)] public float visibleSlitWidth=.26f;
            public float glideElevation=.04f, stepPulseDuration=.09f, stepPulseIntensity=.55f;
            public float clickVolume=.22f, clickPitchVariation=.018f;
            public float arrivalPulseDuration=.22f, arrivalPulseIntensity=1.15f, arrivalClickVolume=.26f;
            public float sharedTileSpacing=.32f, offsetDuration=.16f, fxIntensity=1f;
        }
        public Tuning tuning = new Tuning();
        public int PlayerId { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsMoving => moving;
        public bool IsArriving => Time.unscaledTime<arrivalEnd;
        public bool IsShared { get; private set; }
        public int Crossings { get; private set; }
        public int AudibleContacts { get; private set; }
        public int FinalLocks { get; private set; }
        public Transform Visual => visual;
        public Vector3 SharedOffset => offset;
        public bool GlowEnabled { get; set; } = true;
        public bool AudioEnabled { get; set; } = true;
        public static readonly List<CompactPawnVisual> Instances = new List<CompactPawnVisual>(8);

        static Mesh shellMesh,ringMesh,slitMesh;
        static readonly Mesh[] signatureMeshes=new Mesh[6];
        static Material sharedBody,sharedMetal;
        static AudioClip stepClip,lockClip;
        static int users;
        Transform visual,ring;
        Material signature;
        MaterialPropertyBlock ringProperties,topProperties;
        Renderer ringRenderer,topRenderer;
        Transform slit;
        BoardContactFeedback boardContact;
        AudioSource source;
        PawnMovement movement;
        List<Vector3> path;
        int next,sequence;
        float shortWidth=1f,floor,lift,pulseEnd,arrivalEnd,turnPulseEnd,lastAudio=-100f;
        bool initialized,moving,holdingFloor,restored,pendingArrival;
        int finalWaypoint;
        public float LastLockTime { get; private set; } = -1;
        public bool IsAwaitingArrival => pendingArrival;
        Vector3 offset,previousRoot,offsetStart,offsetTarget;
        float offsetTime;
        Color identity;

        public void Initialize(int playerId,float tileWidth)
        {
            if(initialized)return;
            initialized=true;PlayerId=playerId;shortWidth=Mathf.Max(.1f,tileWidth);
            movement=GetComponent<PawnMovement>();identity=PlayerVisualManager.GetPlayerColor(playerId);
            EnsureResources(tuning);users++;
            var old=GetComponent<MeshRenderer>();if(old!=null)old.enabled=false;
            visual=new GameObject("Compact Tech Monolith").transform;visual.SetParent(transform,false);
            MakePart("Satin shell",shellMesh,new[]{sharedBody,sharedMetal},visual);
            signature=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Pawn signature "+playerId};
            signature.SetFloat("_Metallic",.25f);signature.SetFloat("_Smoothness",.48f);
            signature.SetColor("_BaseColor",Color.Lerp(identity,Color.white,.16f));signature.EnableKeyword("_EMISSION");
            slit=MakePart("Visible front slit",slitMesh,new[]{signature},visual);
            var crown=MakePart("Precision luminous crown",signatureMeshes[IdentitySlot(playerId)],new[]{signature},visual);
            topRenderer=crown.GetComponent<Renderer>();topProperties=new MaterialPropertyBlock();
            ring=MakePart("Precision base ring",ringMesh,new[]{signature},visual);
            ringRenderer=ring.GetComponent<Renderer>();ringProperties=new MaterialPropertyBlock();
            var board=FindObjectOfType<PolyRiskBoard>();
            if(board!=null){boardContact=board.GetComponent<BoardContactFeedback>();if(boardContact==null)boardContact=board.gameObject.AddComponent<BoardContactFeedback>();}
            source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;
            source.loop=false;source.volume=1;source.dopplerLevel=0;
            floor=transform.position.y;previousRoot=transform.position;
            Instances.Add(this);ApplyPose();SetEmission(0);
        }
        void OnEnable()
        {
            PlayerVisualManager.VisualMovementStarted+=OnMovement;
            OnlineGameController.PresentationStateApplied+=ResetPresentation;
        }
        void OnDisable()
        {
            PlayerVisualManager.VisualMovementStarted-=OnMovement;
            OnlineGameController.PresentationStateApplied-=ResetPresentation;
            ResetPresentation();
        }
        void OnDestroy()
        {
            Instances.Remove(this);
            if(signature!=null)Destroy(signature);
            if(!initialized || --users>0)return;
            Destroy(shellMesh);Destroy(ringMesh);Destroy(slitMesh);Destroy(sharedBody);Destroy(sharedMetal);
            for(int i=0;i<signatureMeshes.Length;i++){Destroy(signatureMeshes[i]);signatureMeshes[i]=null;}
            shellMesh=null;ringMesh=null;slitMesh=null;sharedBody=null;sharedMetal=null;
        }
        public void SetActive(bool active,bool turnImpulse=true)
        {
            bool changed=IsActive!=active;IsActive=active;
            if(active&&changed&&turnImpulse&&!UIUXRuntime.ReducedMotion)turnPulseEnd=Time.unscaledTime+.18f;
            if(initialized)SetEmission(0);
        }
        void OnMovement(int playerId,PawnMovement pawn,List<Vector3> waypoints)
        {
            if(pawn!=movement || !initialized)return;
            BeginPath(waypoints);
        }
        public void BeginPath(List<Vector3> waypoints)
        {
            ResetPresentation();
            if(waypoints==null||GameBootstrapper.SkipMovementAnimation)return;
            path=waypoints;next=0;sequence++;
            while(next<path.Count&&PlanarDistanceSquared(transform.position,path[next])<.0001f)next++;
            moving=next<path.Count;floor=transform.position.y;holdingFloor=moving;
            Trace("BEGIN",next,false);
        }
        public void ResetPresentation()
        {
            moving=false;pendingArrival=false;holdingFloor=false;path=null;lift=0;pulseEnd=arrivalEnd=turnPulseEnd=0;
            restored=true;
            if(source!=null)source.Stop();
            if(initialized){floor=transform.position.y;previousRoot=transform.position;SetEmission(0);}
        }
        void LateUpdate()
        {
            if(!initialized)return;
            float now=Time.unscaledTime,dt=Time.unscaledDeltaTime;
            bool reduced=UIUXRuntime.ReducedMotion;
            // Teleports/restores never create a contact or replay an old path.
            if(PlanarDistanceSquared(previousRoot,transform.position)>4f)ResetPresentation();
            previousRoot=transform.position;
            if(moving&&path!=null)
            {
                if(PlanarDistanceSquared(transform.position,path[next])<.00000001f)
                {
                    floor=path[next].y;Crossings++;bool final=next==path.Count-1;
                    if(final)
                    {
                        // The root has reached the final tile, but its historical bounce
                        // still owns completion. Keep the child settled until that clock ends.
                        moving=false;pendingArrival=true;finalWaypoint=next;
                        Trace("REACHED",next,false);
                    }
                    else
                    {
                        if(!reduced)pulseEnd=now+tuning.stepPulseDuration;
                        TileContact(path[next],false);
                        bool played=Contact(false,now);Trace("TICK",next,played);
                    }
                    next++;
                }
            }
            // Camera completes through the untouched business callback earlier this frame.
            // Its hold and this lock therefore share the same rendered frame, even at low FPS.
            if(pendingArrival&&movement.IsVisuallySettled)
            {
                pendingArrival=false;arrivalEnd=now+tuning.arrivalPulseDuration;FinalLocks++;
                LastLockTime=now;if(!reduced)pulseEnd=arrivalEnd;
                TileContact(path[finalWaypoint],true);
                bool played=Contact(true,now);Trace("LOCK",finalWaypoint,played);
            }
            // Keep the original final 80ms bounce/callback entirely on the invisible root.
            // Only the child is settled. No coroutine delays any business consequence.
            if(holdingFloor&&!moving&&!pendingArrival&&now>=arrivalEnd)
            {holdingFloor=false;floor=transform.position.y;}
            if(!holdingFloor)floor=transform.position.y;
            float targetLift=moving&&!reduced?tuning.glideElevation*shortWidth:0;
            lift=Mathf.MoveTowards(lift,targetLift,dt*shortWidth*.9f);
            UpdateSharedOffset(dt,reduced);
            float pulse=0;
            if(!reduced&&tuning.fxIntensity>0)
            {
                if(now<turnPulseEnd)pulse=Mathf.Max(pulse,Mathf.Sin((1-(turnPulseEnd-now)/.18f)*Mathf.PI)*.5f);
                if(now<pulseEnd)
                {
                    float duration=IsArriving?tuning.arrivalPulseDuration:tuning.stepPulseDuration;
                    pulse=Mathf.Max(pulse,(1-Mathf.SmoothStep(0,1,Mathf.Clamp01(1-(pulseEnd-now)/duration)))*
                        (IsArriving?tuning.arrivalPulseIntensity:tuning.stepPulseIntensity));
                }
            }
            SetEmission(pulse*tuning.fxIntensity);
            float spread=!reduced&&IsArriving?1+Mathf.Sin(Mathf.Clamp01(1-(arrivalEnd-now)/tuning.arrivalPulseDuration)*Mathf.PI)*.15f*tuning.fxIntensity:1;
            ring.localScale=new Vector3(spread,1,spread);
            slit.localScale=new Vector3(tuning.visibleSlitWidth/.07f,1,1);ApplyPose();
        }
        void UpdateSharedOffset(float dt,bool reduced)
        {
            int count=0;
            if(!moving)foreach(var other in Instances)
                if(other!=null&&other.isActiveAndEnabled&&!other.moving&&PlanarDistanceSquared(transform.position,other.transform.position)<.0025f)count++;
            IsShared=count>1;
            Vector3 desired=IsShared?SlotOffset(PlayerId,shortWidth*tuning.sharedTileSpacing):Vector3.zero;
            if(restored||reduced){offset=offsetStart=offsetTarget=desired;offsetTime=tuning.offsetDuration;}
            else
            {
                if(desired!=offsetTarget){offsetStart=offset;offsetTarget=desired;offsetTime=0;}
                offsetTime=Mathf.Min(offsetTime+dt,tuning.offsetDuration);
                offset=Vector3.Lerp(offsetStart,offsetTarget,Mathf.SmoothStep(0,1,offsetTime/Mathf.Max(.01f,tuning.offsetDuration)));
            }
            restored=false;
        }
        void ApplyPose()
        {
            visual.position=new Vector3(transform.position.x,floor+lift+.006f,transform.position.z)+offset;
            visual.rotation=Quaternion.Euler(0,45,0);
            Vector3 parent=transform.lossyScale;
            float emphasis=IsActive?1.06f:1f;
            visual.localScale=new Vector3(shortWidth*tuning.baseWidthRatio/Mathf.Max(.001f,parent.x),
                shortWidth*tuning.heightRatio/Mathf.Max(.001f,parent.y),
                shortWidth*tuning.baseWidthRatio/Mathf.Max(.001f,parent.z))*emphasis;
        }
        float previousEmission=-1;
        void SetEmission(float pulse)
        {
            if(signature==null)return;
            float value=GlowEnabled?tuning.emissionIntensity*(IsActive?tuning.activeEmissionMultiplier:.65f)*
                (moving?tuning.movingEmissionMultiplier:1)+pulse:0;
            if(Mathf.Abs(value-previousEmission)<.0001f)return;
            signature.SetColor("_EmissionColor",identity*value);
            ringProperties.SetColor("_EmissionColor",identity*value*tuning.baseRingIntensity*(IsActive?tuning.baseRingActiveMultiplier:1));
            ringRenderer.SetPropertyBlock(ringProperties);
            topProperties.SetColor("_EmissionColor",identity*value*tuning.topAccentIntensity);
            topRenderer.SetPropertyBlock(topProperties);previousEmission=value;
        }
        void TileContact(Vector3 reached,bool final)
        {
            if(boardContact!=null)boardContact.PulseAt(reached,identity,final,UIUXRuntime.ReducedMotion);
        }
        bool Contact(bool final,float now)
        {
            // Latest reached waypoint wins; no timer and no catch-up audio burst.
            if(!AudioEnabled||(!final&&now-lastAudio<.065f)||source==null)return false;
            AudioClip clip=final?lockClip:stepClip;if(clip==null)return false;
            float gap=now-lastAudio;lastAudio=now;
            float variation=((Crossings%3)-1)*tuning.clickPitchVariation;
            source.pitch=(final?.88f:1f)+variation;
            source.Stop();source.clip=clip;
            source.volume=(final?tuning.arrivalClickVolume:tuning.clickVolume)*(gap<.14f?.65f:1f);
            source.Play();AudibleContacts++;return true;
        }
        public static int IdentitySlot(int id) {int n=id%6;return n<0?n+6:n;}
        public static Vector3 SlotOffset(int id,float radius)
        {
            float a=IdentitySlot(id)*Mathf.PI/3+Mathf.PI/6;
            return new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);
        }
        static float PlanarDistanceSquared(Vector3 a,Vector3 b){float x=a.x-b.x,z=a.z-b.z;return x*x+z*z;}
        void Trace(string phase,int waypoint,bool audio)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("MONOLITH_CONTACT "+phase+" actor="+PlayerId+" seq="+sequence+" waypoint="+waypoint+
                " crossings="+Crossings+" audio="+audio+" t="+Time.unscaledTime.ToString("F4",System.Globalization.CultureInfo.InvariantCulture));
#endif
        }
        static Transform MakePart(string name,Mesh mesh,Material[] materials,Transform parent)
        {
            var part=new GameObject(name);part.transform.SetParent(parent,false);
            part.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=part.AddComponent<MeshRenderer>();renderer.sharedMaterials=materials;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            return part.transform;
        }
        static void EnsureResources(Tuning config)
        {
            if(shellMesh!=null)return;
            sharedBody=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Shared pawn satin graphite"};
            sharedBody.SetColor("_BaseColor",config.bodyColor);sharedBody.SetFloat("_Metallic",config.metallic);sharedBody.SetFloat("_Smoothness",config.smoothness);
            sharedMetal=new Material(sharedBody){name="Shared pawn precision edges"};sharedMetal.SetColor("_BaseColor",new Color(.42f,.48f,.54f));
            sharedMetal.SetFloat("_Metallic",.62f);sharedMetal.SetFloat("_Smoothness",.55f);
            shellMesh=Shell();ringMesh=Ring();slitMesh=Signature();for(int i=0;i<6;i++)signatureMeshes[i]=Crown(i+1);
            stepClip=Resources.Load<AudioClip>("PolyRiskAudio/contact_light");lockClip=Resources.Load<AudioClip>("PolyRiskAudio/contact_tac");
        }
        sealed class Builder
        {
            public readonly List<Vector3> v=new List<Vector3>();
            public readonly List<int>[] triangles={new List<int>(),new List<int>()};
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int material=0)
            {int n=v.Count;v.Add(a);v.Add(b);v.Add(c);v.Add(d);triangles[material].AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            public Mesh Finish(string name,int submeshes=1)
            {var m=new Mesh{name=name};m.SetVertices(v);m.subMeshCount=submeshes;for(int i=0;i<submeshes;i++)m.SetTriangles(triangles[i],i);m.RecalculateNormals();m.RecalculateBounds();return m;}
        }
        static Vector3 Section(int index,float x,float z,float y)
        {
            // Chamfered rectangle: clean planar faces, no smoothed plastic normals.
            Vector2[] shape={new Vector2(-.72f,-1),new Vector2(.72f,-1),new Vector2(1,-.72f),new Vector2(1,.72f),new Vector2(.72f,1),new Vector2(-.72f,1),new Vector2(-1,.72f),new Vector2(-1,-.72f)};
            return new Vector3(shape[index].x*x,y,shape[index].y*z);
        }
        static void Prism(Builder b,float x,float z,float bottom,float top,int material)
        {
            for(int i=0;i<8;i++)
            {int j=(i+1)%8;b.Quad(Section(i,x,z,bottom),Section(i,x,z,top),Section(j,x,z,top),Section(j,x,z,bottom),material);}
            // Cover cap side strips using a fan, preserving the octagonal outline.
            for(int i=0;i<8;i++){int j=(i+1)%8;var a=Section(i,x,z,top);var c=Section(j,x,z,top);b.Quad(Vector3.up*top,c,a,a,material);}
        }
        static Mesh Shell()
        {
            var b=new Builder();Prism(b,.405f,.405f,0,.095f,1);Prism(b,.37f,.34f,.095f,.90f,0);
            for(int i=0;i<8;i++){int j=(i+1)%8;b.Quad(Section(i,.37f,.34f,.90f),Section(i,.335f,.305f,.98f),Section(j,.335f,.305f,.98f),Section(j,.37f,.34f,.90f),1);}
            Prism(b,.335f,.305f,.975f,.98f,1);return b.Finish("Monolith shared faceted shell",2);
        }
        static Mesh Ring()
        {
            var b=new Builder();
            for(int i=0;i<8;i++){int j=(i+1)%8;b.Quad(Section(i,.397f,.397f,.099f),Section(i,.345f,.345f,.099f),Section(j,.345f,.345f,.099f),Section(j,.397f,.397f,.099f));}
            return b.Finish("Shared fine octagonal ring");
        }
        static Mesh Crown(int count)
        {
            var b=new Builder();
            for(int i=0;i<8;i++){int j=(i+1)%8;
                b.Quad(Section(i,.326f,.296f,.983f),Section(i,.245f,.215f,.983f),
                    Section(j,.245f,.215f,.983f),Section(j,.326f,.296f,.983f));}
            for(int i=0;i<count;i++)
            {float x=((i%3)-1)*.135f,z=((i/3)-.5f)*.15f;float s=.035f;
             b.Quad(new Vector3(x-s,.984f,z-s),new Vector3(x-s,.984f,z+s),new Vector3(x+s,.984f,z+s),new Vector3(x+s,.984f,z-s));}
            return b.Finish("Crown and identity code "+count);
        }
        static Mesh Signature()
        {
            var b=new Builder();b.Quad(new Vector3(-.035f,.23f,-.342f),new Vector3(-.035f,.83f,-.342f),new Vector3(.035f,.83f,-.342f),new Vector3(.035f,.23f,-.342f));
            return b.Finish("Shared visible front slit");
        }
    }
}
