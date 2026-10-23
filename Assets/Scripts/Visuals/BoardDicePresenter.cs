using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PolyRisk.Core;
using PolyRisk.Systems;
using PolyRisk.UI;

namespace PolyRisk.Visuals
{
    /// <summary>Presentation of confirmed dice only. No physics, RNG, or game completion callback.</summary>
    public sealed class BoardDicePresenter : MonoBehaviour
    {
        public static BoardDicePresenter Instance { get; private set; }
        [SerializeField, Range(.7f,1.1f)] private float diceAnimationDuration = .94f;
        [SerializeField, Range(.7f,1.4f)] private float dieEdge = 1.10f;
        [SerializeField, Range(.08f,.25f)] private float departureFadeDuration = .14f;
        private readonly Transform[] dice = new Transform[2];
        private readonly int[] values = new int[2];
        private readonly List<Mesh> meshes = new List<Mesh>();
        private GameObject stage;
        private Material body, pips, trim;
        private Coroutine animation;
        private bool visible, releasing, resultShown;
        private int actor = -1;
        private long eventId;
        private GameEvent latestConfirmed, movementFact;
        private float started, releaseAt;
        private Vector3 boardCenter;
        private readonly MaterialPropertyBlock holographicTint = new MaterialPropertyBlock();
        public bool IsVisible => visible;
        public float WorldDieEdge => dice[0]!=null?dice[0].lossyScale.x:0f;
        public bool IsResultShown => resultShown;
        public int First => values[0];
        public int Second => values[1];
        public float LeadInRemaining(int playerId) => visible && !releasing && actor==playerId
            ? Mathf.Clamp(started+(UIUXRuntime.ReducedMotion ? .24f : diceAnimationDuration)-Time.unscaledTime,0f,1.1f) : 0f;
        public bool ResultFacesUp => dice[0]!=null && Vector3.Dot(dice[0].rotation*FaceNormal(values[0]),Vector3.up)>.999f
            && Vector3.Dot(dice[1].rotation*FaceNormal(values[1]),Vector3.up)>.999f;

        private void OnEnable()
        {
            Instance=this;
            PlayerVisualManager.VisualMovementStarted+=OnMovement;
            GameEvents.OnGameEventPublished+=ObserveFact;
            PolyRisk.Multiplayer.OnlineGameController.PresentationStateApplied+=OnRestored;
            GameEvents.OnTurnStarted+=OnTurn;
        }
        private void OnDisable()
        {
            PlayerVisualManager.VisualMovementStarted-=OnMovement;
            GameEvents.OnGameEventPublished-=ObserveFact;
            PolyRisk.Multiplayer.OnlineGameController.PresentationStateApplied-=OnRestored;
            GameEvents.OnTurnStarted-=OnTurn;
            Cancel();if(Instance==this)Instance=null;
        }
        private void ObserveFact(GameEvent item) { if(item.Kind==GameEventKind.DiceRolled)latestConfirmed=item; }
        private void OnRestored() { movementFact=latestConfirmed;Cancel(); }
        private void OnTurn(int playerId) { if(visible && actor!=playerId) Cancel(); }
        public bool Present(GameEvent item)
        {
            if(item==null || item.Kind!=GameEventKind.DiceRolled || item.ValueA<1 || item.ValueA>6 || item.ValueB<1 || item.ValueB>6) return false;
            if(!isActiveAndEnabled || object.ReferenceEquals(item,movementFact) || GameBootstrapper.SkipMovementAnimation || Application.isBatchMode) return false;
            var board=FindObjectOfType<PolyRiskBoard>();
            if(board==null || Camera.main==null) return false;
            latestConfirmed=item;Cancel();EnsureStage();
            actor=item.ActorId;eventId=item.Id;values[0]=item.ValueA;values[1]=item.ValueB;
            for(int i=0;i<2;i++) dice[i].GetChild(0).GetComponent<MeshFilter>().sharedMesh=PipMesh();
            boardCenter=board.transform.TransformPoint(new Vector3(0,board.settings.tileThickness*.34f+.015f,0));
            started=Time.unscaledTime;releasing=false;resultShown=false;visible=true;stage.SetActive(true);
            ActionButtonsWidget.SetTransientDicePresentation(true);
            Trace("BEGIN");animation=StartCoroutine(Animate());return true;
        }
        private void OnMovement(int playerId,PawnMovement pawn,List<Vector3> path)
        {
            movementFact=latestConfirmed;
            if(!visible || actor!=playerId || releasing) return;
            releasing=true;releaseAt=Time.unscaledTime;Trace("FADE");
        }
        public void Cancel()
        {
            if(animation!=null)StopCoroutine(animation);animation=null;
            bool restore=visible;visible=false;releasing=false;
            if(stage!=null)stage.SetActive(false);
            if(restore)ActionButtonsWidget.SetTransientDicePresentation(false);
        }
        private IEnumerator Animate()
        {
            bool reduced=UIUXRuntime.ReducedMotion;
            float duration=reduced ? .24f : diceAnimationDuration;
            while(visible)
            {
                reduced=UIUXRuntime.ReducedMotion;
                duration=reduced ? .24f : diceAnimationDuration;
                float elapsed=Time.unscaledTime-started;
                if(!releasing && elapsed>duration+.45f){releasing=true;releaseAt=Time.unscaledTime;}
                float fade=releasing?(reduced?1f:Mathf.Clamp01((Time.unscaledTime-releaseAt)/departureFadeDuration)):0f;
                float flight=reduced?1f:Mathf.Clamp01(elapsed/(diceAnimationDuration*.66f));
                float ease=flight*flight*(3f-2f*flight);
                for(int i=0;i<2;i++)
                {
                    Quaternion target=FaceUp(values[i],i==0?-12f:12f);
                    float extra=(1f-ease)*(i==0?1f:-1f);
                    dice[i].rotation=target*Quaternion.Euler(220f*extra,100f*extra,145f*extra);
                    float lift=reduced?0f:Mathf.Sin(flight*Mathf.PI)*.34f;
                    float settle=reduced?0f:Mathf.Sin(Mathf.Clamp01((elapsed/diceAnimationDuration-.66f)/.14f)*Mathf.PI)*.055f;
                    Vector3 side=Camera.main.transform.right*(i==0?-.76f:.76f)*dieEdge;
                    dice[i].position=boardCenter+side+Vector3.up*(dieEdge*.5f+lift+settle);
                    dice[i].localScale=Vector3.one*(dieEdge*(1f-fade));
                    Color tint=Color.Lerp(new Color(.3f,.57f,.58f),new Color(.43f,.37f,.57f),.5f+.5f*Mathf.Sin(extra*2f+i));
                    holographicTint.SetColor("_BaseColor",tint);holographicTint.SetColor("_EmissionColor",tint*.28f);
                    dice[i].GetChild(1).GetComponent<Renderer>().SetPropertyBlock(holographicTint);
                }
                if(!resultShown && (reduced || elapsed>=diceAnimationDuration*.80f))
                {resultShown=true;Trace("RESULT");}
                if(fade>=1f)break;
                yield return null;
            }
            animation=null;visible=false;stage.SetActive(false);
            ActionButtonsWidget.SetTransientDicePresentation(false);
        }
        private void Trace(string phase)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("DICE_"+phase+" id="+eventId+" actor="+actor+" values="+values[0]+","+values[1]+
                " up="+ResultFacesUp+" t="+Time.unscaledTime.ToString("F4",System.Globalization.CultureInfo.InvariantCulture));
#endif
        }
        public static Vector3 FaceNormal(int value) => value==1?Vector3.up:value==6?Vector3.down:
            value==2?Vector3.forward:value==5?Vector3.back:value==3?Vector3.right:Vector3.left;
        public static Quaternion FaceUp(int value,float yaw=0) => Quaternion.AngleAxis(yaw,Vector3.up)*Quaternion.FromToRotation(FaceNormal(value),Vector3.up);
        private void EnsureStage()
        {
            if(stage!=null)return;
            stage=new GameObject("Board dice — presentation only");stage.transform.SetParent(null,false);stage.SetActive(false);
            body=Material(new Color(.16f,.20f,.24f),.42f,.48f,Color.black);
            pips=Material(new Color(.82f,.91f,.93f),.18f,.4f,new Color(.20f,.30f,.31f));
            trim=Material(new Color(.24f,.48f,.49f),.5f,.56f,new Color(.05f,.16f,.17f));
            Mesh cube=BodyMesh(),marks=TrimMesh();
            for(int i=0;i<2;i++)
            {
                dice[i]=Part("Die "+i,stage.transform,cube,body);
                Part("Pips",dice[i],null,pips);Part("Holographic edge",dice[i],marks,trim);
            }
        }
        private static Material Material(Color color,float metallic,float smoothness,Color emission)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name="BoardDice reusable satin";
            m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smoothness);
            if(emission.maxColorComponent>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",emission);}
            return m;
        }
        private static Transform Part(string name,Transform parent,Mesh mesh,Material mat)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;
            r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;r.receiveShadows=true;return go.transform;
        }
        private readonly List<Vector3> vertices=new List<Vector3>();
        private readonly List<int> triangles=new List<int>();
        private void Polygon(Vector3[] points,Vector3 normal)
        {
            int start=vertices.Count;vertices.AddRange(points);
            bool reverse=Vector3.Dot(Vector3.Cross(points[1]-points[0],points[2]-points[0]),normal)<0;
            for(int i=1;i<points.Length-1;i++){triangles.Add(start);triangles.Add(start+(reverse?i+1:i));triangles.Add(start+(reverse?i:i+1));}
        }
        private Mesh Finish(string name)
        {
            var m=new Mesh{name=name};m.SetVertices(vertices);m.SetTriangles(triangles,0);m.RecalculateNormals();m.RecalculateBounds();
            vertices.Clear();triangles.Clear();meshes.Add(m);return m;
        }
        private static void Basis(Vector3 n,out Vector3 r,out Vector3 u)
        {r=Mathf.Abs(n.y)>.5f?Vector3.right:Vector3.Cross(Vector3.up,n).normalized;u=Vector3.Cross(n,r);}
        private Mesh BodyMesh()
        {
            for(int f=1;f<=6;f++)
            {
                Vector3 n=FaceNormal(f);Basis(n,out var r,out var u);Vector3 c=n*.5f;
                Polygon(new[]{c-r*.46f-u*.46f,c+r*.46f-u*.46f,c+r*.46f+u*.46f,c-r*.46f+u*.46f},n);
            }
            var axes=new[]{Vector3.right,Vector3.up,Vector3.forward};
            for(int a=0;a<3;a++)for(int b=a+1;b<3;b++)for(int sa=-1;sa<=1;sa+=2)for(int sb=-1;sb<=1;sb+=2)
            {
                Vector3 n=axes[a]*sa,k=axes[b]*sb,along=axes[3-a-b]*.46f;
                Polygon(new[]{n*.5f+k*.46f-along,n*.46f+k*.5f-along,n*.46f+k*.5f+along,n*.5f+k*.46f+along},n+k);
            }
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            {
                Vector3 n=new Vector3(x,y,z);
                Polygon(new[]{Vector3.Scale(n,new Vector3(.5f,.46f,.46f)),Vector3.Scale(n,new Vector3(.46f,.5f,.46f)),Vector3.Scale(n,new Vector3(.46f,.46f,.5f))},n);
            }
            return Finish("Shared compact bevel die");
        }
        private static readonly int[][] Patterns={new int[0],new[]{4},new[]{0,8},new[]{0,4,8},new[]{0,2,6,8},new[]{0,2,4,6,8},new[]{0,2,3,5,6,8}};
        private Mesh pipMesh;
        private Mesh PipMesh()
        {
            if(pipMesh!=null)return pipMesh;
            for(int f=1;f<=6;f++)
            {
                Vector3 n=FaceNormal(f);Basis(n,out var r,out var u);
                foreach(int dot in Patterns[f])
                {
                    Vector3 c=n*.502f+r*((dot%3-1)*.235f)+u*((1-dot/3)*.235f);var ring=new Vector3[10];
                    for(int i=0;i<10;i++){float a=i*Mathf.PI*.2f;ring[i]=c+(r*Mathf.Cos(a)+u*Mathf.Sin(a))*.057f;}
                    Polygon(ring,n);
                }
            }
            return pipMesh=Finish("Shared true six-face pips");
        }
        private Mesh TrimMesh()
        {
            for(int f=1;f<=6;f++)
            {
                Vector3 n=FaceNormal(f);Basis(n,out var r,out var u);Vector3 c=n*.503f+u*.414f;
                Polygon(new[]{c-r*.34f-u*.018f,c+r*.34f-u*.018f,c+r*.34f+u*.018f,c-r*.34f+u*.018f},n);
            }
            return Finish("Shared fine holographic inlay");
        }
        private void OnDestroy()
        {
            if(stage!=null)Destroy(stage);foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
            if(body!=null)Destroy(body);if(pips!=null)Destroy(pips);if(trim!=null)Destroy(trim);
        }
    }
}
