using UnityEngine;
using PolyRisk.Multiplayer;

namespace PolyRisk.Visuals
{
    /// <summary>Independent transient layer. Never reads/writes the persistent tile-state pixels.</summary>
    [DefaultExecutionOrder(90)]
    public sealed class BoardContactFeedback : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Tuning
        {
            public float tileContactPulseDuration=.12f, tileContactPulseIntensity=1.15f;
            public float tileContactOutlineIntensity=1f, tileContactInnerGlowIntensity=.075f;
            [Range(0,1)] public float tileContactColorBlend=.55f;
            public float tileArrivalPulseDuration=.24f, tileArrivalPulseIntensity=1.65f;
            public float reducedMotionIntensity=.45f;
        }
        public Tuning tuning=new Tuning();
        public int Contacts { get; private set; }
        public int LastIndex { get; private set; } = -1;
        public float LastContactTime { get; private set; } = -1;
        public float LastIntensity { get; private set; }
        public bool LastArrival { get; private set; }
        public bool HasActiveContacts { get; private set; }
        const int Width=64;
        struct Pulse { public float began,duration,intensity; public Color color; }
        readonly Pulse[] pulses=new Pulse[40];
        readonly Color[] pixels=new Color[Width*2];
        readonly Transform[] anchors=new Transform[40];
        PolyRiskBoard board;
        Texture2D texture;
        static readonly int ContactTex=Shader.PropertyToID("_ContactTex");
        void OnEnable()
        {
            board=GetComponent<PolyRiskBoard>();
            foreach(var id in GetComponentsInChildren<TileIdentifier>())
                if(id.TileIndex>=0&&id.TileIndex<40)anchors[id.TileIndex]=id.transform;
            texture=new Texture2D(Width,2,TextureFormat.RGBAHalf,false,true)
                {name="Board transient contact only",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            Clear();
            OnlineGameController.PresentationStateApplied+=Clear;
        }
        void OnDisable()
        {
            OnlineGameController.PresentationStateApplied-=Clear;
            Bind(Texture2D.blackTexture);
            if(texture!=null)Destroy(texture);
        }
        void Bind(Texture value)
        {
            if(board==null)return;
            if(board.tileMaterial!=null)board.tileMaterial.SetTexture(ContactTex,value);
            if(board.neonMaterial!=null)board.neonMaterial.SetTexture(ContactTex,value);
        }
        public void Clear()
        {
            System.Array.Clear(pulses,0,pulses.Length);System.Array.Clear(pixels,0,pixels.Length);
            HasActiveContacts=false;
            if(texture!=null){texture.SetPixels(pixels);texture.Apply(false,false);Bind(texture);}
        }
        public void PulseAt(Vector3 reached,Color identity,bool arrival,bool reduced)
        {
            int index=-1;
            for(int i=0;i<40;i++)if(anchors[i]!=null&&(anchors[i].position-reached).sqrMagnitude<.0001f){index=i;break;}
            if(index<0)return; // Only a real indexed anchor can receive a contact.
            float duration=arrival?tuning.tileArrivalPulseDuration:tuning.tileContactPulseDuration;
            float intensity=(arrival?tuning.tileArrivalPulseIntensity:tuning.tileContactPulseIntensity)*(reduced?tuning.reducedMotionIntensity:1);
            pulses[index]=new Pulse{began=Time.unscaledTime,duration=Mathf.Max(.01f,duration),intensity=intensity,
                color=Color.Lerp(new Color(.70f,.80f,.84f),identity,tuning.tileContactColorBlend)};
            pixels[Width+index]=new Color(tuning.tileContactOutlineIntensity,tuning.tileContactInnerGlowIntensity,arrival?1:0,0);
            Contacts++;LastIndex=index;LastContactTime=Time.unscaledTime;LastIntensity=intensity;LastArrival=arrival;HasActiveContacts=true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("TILE_CONTACT index="+index+" arrival="+arrival+" reduced="+reduced+" intensity="+intensity.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" t="+LastContactTime.ToString("F4",System.Globalization.CultureInfo.InvariantCulture));
#endif
        }
        void LateUpdate()
        {
            if(!HasActiveContacts)return;
            HasActiveContacts=false;float now=Time.unscaledTime;
            for(int i=0;i<40;i++)
            {
                var pulse=pulses[i];float phase=pulse.duration>0?(now-pulse.began)/pulse.duration:1;
                float strength=phase<1?pulse.intensity*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,1,phase))):0;
                var color=pulse.color;color.a=strength;pixels[i]=color;
                HasActiveContacts|=strength>0;
            }
            // One tiny texture upload only while a contact is alive, including its final clear.
            texture.SetPixels(pixels);texture.Apply(false,false);Bind(texture);
        }
    }
}
