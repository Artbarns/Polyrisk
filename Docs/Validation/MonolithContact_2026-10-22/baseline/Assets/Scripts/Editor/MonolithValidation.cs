using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor.SceneManagement;
using PolyRisk.Systems;
using PolyRisk.Visuals;

namespace PolyRisk.Editor
{
    public static class MonolithValidation
    {
        static int checks;
        static void Check(bool value,string name)
        {checks++;if(!value)throw new Exception("MONOLITH EDITOR FAIL "+name);}
        public static void RunAndBuild()
        {
            EconomyBatchValidation.RunAllEditorRegressions();
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var board=UnityEngine.Object.FindObjectOfType<PolyRiskBoard>();
            var anchors=board.GetComponentsInChildren<TileIdentifier>().OrderBy(x=>x.TileIndex).ToArray();
            Check(anchors.Length==40&&anchors.Select(x=>x.TileIndex).SequenceEqual(Enumerable.Range(0,40)),"40 indices");
            Physics.SyncTransforms();
            foreach(var a in anchors)
            {RaycastHit hit;Check(Physics.Raycast(a.transform.position+Vector3.up*2,Vector3.down,out hit,3)&&hit.collider.gameObject==a.gameObject,"raycast "+a.TileIndex);}
            Check(board.GetComponentsInChildren<MeshFilter>().Length==4,"four original board meshes");
            for(int a=0;a<6;a++)for(int b=a+1;b<6;b++)
                Check(Vector3.Distance(CompactPawnVisual.SlotOffset(a,.30f),CompactPawnVisual.SlotOffset(b,.30f))>=.2999f,"six deterministic nonoverlapping slots "+a+" "+b);
            for(int n=-6;n<12;n++)Check(CompactPawnVisual.IdentitySlot(n)>=0&&CompactPawnVisual.IdentitySlot(n)<6,"stable identity "+n);
            var config=new CompactPawnVisual.Tuning();
            Check(config.baseWidthRatio<config.sharedTileSpacing,"base fits adjacent slots");
            Check(config.sharedTileSpacing+config.baseWidthRatio/2<.5f,"shared footprint inside narrow tile");
            Check(config.heightRatio<=.7f&&config.glideElevation<=.05f,"portrait-calibrated compact height and low glide");
            Check(config.stepPulseDuration>=.07f&&config.stepPulseDuration<=.11f,"step pulse range");
            Check(config.arrivalPulseDuration>=.18f&&config.arrivalPulseDuration<=.26f,"arrival range");
            var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
            var shell=(Mesh)typeof(CompactPawnVisual).GetMethod("Shell",flags).Invoke(null,null);
            var ring=(Mesh)typeof(CompactPawnVisual).GetMethod("Ring",flags).Invoke(null,null);
            try
            {
                for(int id=0;id<6;id++)
                {
                    bool inside=true;var offset=CompactPawnVisual.SlotOffset(id,config.sharedTileSpacing);
                    foreach(var mesh in new[]{shell,ring})foreach(var point in mesh.vertices)
                    {
                        Vector3 p=point*(config.baseWidthRatio*1.06f*(mesh==ring?1.15f:1f));
                        p=Quaternion.Euler(0,45,0)*p+offset;
                        inside&=Mathf.Abs(p.x)<.5f&&Mathf.Abs(p.z)<.5f;
                    }
                    Check(inside,"real rotated geometry and active arrival ring inside narrow tile for slot "+id);
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(shell);UnityEngine.Object.DestroyImmediate(ring);}
            Check(Resources.Load<AudioClip>("PolyRiskAudio/contact_light")!=null&&Resources.Load<AudioClip>("PolyRiskAudio/contact_tac")!=null,"reused audio clips available");
            var host=new GameObject("[QA] callback");bool skip=GameBootstrapper.SkipMovementAnimation;
            try
            {
                var pawn=host.AddComponent<PawnMovement>();int callbacks=0;
                pawn.MoveAlongPath(null,()=>callbacks++);Check(callbacks==1,"empty callback exactly once");
                GameBootstrapper.SkipMovementAnimation=true;var end=anchors[20].transform.position;
                pawn.MoveAlongPath(new System.Collections.Generic.List<Vector3>{end},()=>callbacks++);
                Check(callbacks==2&&host.transform.position==end,"skip callback exactly once and exact destination");
            }
            finally{GameBootstrapper.SkipMovementAnimation=skip;UnityEngine.Object.DestroyImmediate(host);}
            string report="MONOLITH EDITOR PASS "+checks+" checks, plus existing 612 regression assertions.\n";
            Directory.CreateDirectory("Docs/Validation/Monolith_2026-10-21");
            File.WriteAllText("Docs/Validation/Monolith_2026-10-21/editor-checks.txt",report);Debug.Log(report);
            EconomyBatchValidation.BuildWebGL();
        }
    }
}
