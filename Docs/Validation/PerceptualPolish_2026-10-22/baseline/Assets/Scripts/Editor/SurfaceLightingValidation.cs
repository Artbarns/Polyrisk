using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
using PolyRisk.Visuals;
namespace PolyRisk.Editor
{
    public static class SurfaceLightingValidation
    {
        const string Report="Docs/Validation/ReflectiveNeon_2026-10-22";
        static int checks;
        static void Check(bool ok,string name){checks++;if(!ok)throw new Exception("GRAPHITE FINISH FAIL "+name);}
        static string Hash(string text){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text)));}
        public static void RunAndBuild()
        {
            EconomyBatchValidation.RunAllEditorRegressions();
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var board=UnityEngine.Object.FindObjectOfType<PolyRiskBoard>();var theme=board.visualTheme;
            theme.surfaceTreatment=BoardVisualTheme.SurfaceTreatment.GraphiteReflectiveNeon;
            EditorUtility.SetDirty(theme);AssetDatabase.SaveAssets();
            var anchors=board.GetComponentsInChildren<TileIdentifier>().OrderBy(x=>x.TileIndex).ToArray();
            Check(anchors.Length==40&&anchors.Select(x=>x.TileIndex).SequenceEqual(Enumerable.Range(0,40)),"40 indices");
            var meshes=board.GetComponentsInChildren<MeshFilter>();
            Check(meshes.Length==4,"four original fused meshes");
            var geometry=meshes.Select(m=>Hash(EditorJsonUtility.ToJson(m.sharedMesh))).ToArray();
            var anchorState=anchors.Select(a=>Hash(EditorJsonUtility.ToJson(a.transform)+EditorJsonUtility.ToJson(a.GetComponent<BoxCollider>()))).ToArray();
            string camera=Hash(EditorJsonUtility.ToJson(Camera.main)+EditorJsonUtility.ToJson(Camera.main.transform));
            var lighting=board.gameObject.AddComponent<BoardSurfaceLighting>();
            foreach(var treatment in new[]{BoardVisualTheme.SurfaceTreatment.GraphiteSatin,BoardVisualTheme.SurfaceTreatment.GraphiteReflectiveNeon})
            {
                lighting.Apply(treatment);var finish=theme.Finish(treatment);
                Check(geometry.SequenceEqual(meshes.Select(m=>Hash(EditorJsonUtility.ToJson(m.sharedMesh)))),treatment+" identical meshes");
                Check(anchorState.SequenceEqual(anchors.Select(a=>Hash(EditorJsonUtility.ToJson(a.transform)+EditorJsonUtility.ToJson(a.GetComponent<BoxCollider>())))),treatment+" identical anchors colliders");
                Check(camera==Hash(EditorJsonUtility.ToJson(Camera.main)+EditorJsonUtility.ToJson(Camera.main.transform)),treatment+" identical camera");
                Check(board.tileMaterial.GetFloat("_Polish")==1&&board.tileMaterial.GetFloat("_ReflectionStrength")==finish.neonReflection,treatment+" actual material uses profile");
                Physics.SyncTransforms();foreach(var anchor in anchors){RaycastHit hit;Check(Physics.Raycast(anchor.transform.position+Vector3.up*2,Vector3.down,out hit,3)&&hit.collider.gameObject==anchor.gameObject,treatment+" raycast "+anchor.TileIndex);}
            }
            Check(theme.reflectiveNeon.neonReflection>theme.graphiteSatin.neonReflection,"B local reflection exceeds A");
            Check(theme.reflectiveNeon.arrivalReflection>theme.reflectiveNeon.contactReflection,"arrival reflection stronger");
            var contact=new BoardContactFeedback.Tuning();Check(contact.tileContactPulseDuration==.12f&&contact.tileArrivalPulseDuration==.24f,"original contact durations");
            var low=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Low_PipelineAsset.asset");
            var medium=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Medium_PipelineAsset.asset");
            Check(low.mainLightRenderingMode==LightRenderingMode.PerPixel&&low.supportsMainLightShadows==false,"Low one main light with no shadow passes");
            Check(low.useSRPBatcher&&medium.useSRPBatcher,"SRP Batcher enabled Low Medium");
            string diagnostics="";
            foreach(var name in new[]{"PolyRisk/GraphiteSurface","PolyRisk/NeonState"})
            {
                var shader=Shader.Find(name);Check(shader!=null,name+" included");
                Check(ShaderUtil.GetShaderMessages(shader).All(m=>m.severity.ToString()!="Error"),name+" import diagnostics");
                var sample=new Material(shader);bool compiled=sample.SetPass(0);UnityEngine.Object.DestroyImmediate(sample);
                var method=typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static,null,new[]{typeof(Shader),typeof(int)},null);
                if(method!=null&&compiled&&SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    int code=(int)method.Invoke(null,new object[]{shader,0});
                    var reason=typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityIssueReason",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
                    string message=reason!=null?(string)reason.Invoke(null,new object[]{shader,0,code}):"unavailable";
                    diagnostics+=name+" SRP compatibility code="+code+" "+message+" device="+SystemInfo.graphicsDeviceType+"\n";
                    Debug.Log(diagnostics);Check(code==0,name+" SRP Batcher compatible after material compilation");
                }
                else diagnostics+=name+" SRP compatibility measurement unavailable on this graphics device; CBUFFER layout reviewed\n";

            }
            lighting.Apply(theme.surfaceTreatment);
            Directory.CreateDirectory(Report);File.WriteAllText(Report+"/theme-after.json",EditorJsonUtility.ToJson(theme,true));
            File.WriteAllText(Report+"/editor-checks.txt","GRAPHITE FINISH PASS "+checks+" checks + 612 existing assertions\n"+diagnostics);
            Debug.Log("GRAPHITE FINISH EDITOR PASS "+checks+" checks\n"+diagnostics);
            UnityEngine.Object.DestroyImmediate(lighting);
            EconomyBatchValidation.BuildWebGL();
        }
    }
}
