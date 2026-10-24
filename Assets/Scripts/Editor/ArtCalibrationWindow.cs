using UnityEditor;
using UnityEngine;
using PolyRisk.DebugTools;

namespace PolyRisk.Editor
{
    /// <summary>Artist-facing controls for the isolated Development calibration bench.</summary>
    public sealed class ArtCalibrationWindow : EditorWindow
    {
        Vector2 scroll;
        [MenuItem("PolyRisk/Art/Graphite Calibration")]
        static void Open() => GetWindow<ArtCalibrationWindow>("Graphite Calibration");
        void OnGUI()
        {
            EditorGUILayout.HelpBox("Play the real SampleScene, start an isolated local game, and keep its approved camera. Samples change presentation only. Restore before ordinary play. Frozen peaks do not validate timing.",MessageType.Info);
            var driver=FindObjectOfType<ArtCalibrationDriver>();
            using(new EditorGUI.DisabledScope(!EditorApplication.isPlaying||driver==null))
            {
                if(driver==null)EditorGUILayout.LabelField("Calibration mounts automatically in Editor Play Mode.");
                scroll=EditorGUILayout.BeginScrollView(scroll);
                Row("A · exact baseline",()=>driver.Baseline(),"B · proposal",()=>driver.NewProposal());
                Row("0 · neutral",()=>driver.StageNeutral(),"1 · architecture",()=>driver.StageArchitecture());
                Row("2 · sources",()=>driver.StageSources(),"3 · local response",()=>driver.StageResponse());
                Row("4 · optical finish",()=>driver.StageOptics(),"Restore presentation",()=>driver.RestoreScene());
                EditorGUILayout.Space();
                Row("Inactive graphite",()=>driver.FixtureInactive(),"Colored sample",()=>driver.FixtureOwned());
                Row("Selection",()=>driver.FixtureSelected(),"Contact peak",()=>driver.FixtureContact());
                Row("Arrival peak",()=>driver.FixtureArrival(),"Local reflection",()=>driver.FixtureReflection());
                Row("Resting monolith",()=>driver.FixturePawnIdle(),"Active monolith",()=>driver.FixturePawnActive());
                Row("Edge / bevel",()=>driver.FixtureMetal(),"Corner contact",()=>driver.FixtureCornerContact());
                Row("Corner arrival",()=>driver.FixtureCornerArrival(),"Current readback",()=>driver.Describe());
                EditorGUILayout.Space();
                Row("Short trajectory",()=>driver.MoveShort(),"Long trajectory",()=>driver.MoveLong());
                Row("Cross corner",()=>driver.MoveCorner(),"Invariants",()=>driver.RunChecks());
                if(GUILayout.Button("Measure 6 seconds (global frame / CPU if available, no GPU claim)"))driver.Measure();
                EditorGUILayout.EndScrollView();
            }
            var board=FindObjectOfType<PolyRiskBoard>();
            if(board!=null&&board.visualTheme!=null&&GUILayout.Button("Select reusable theme"))Selection.activeObject=board.visualTheme;
        }
        static void Row(string a,System.Action first,string b,System.Action second)
        {
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button(a))first();
            if(GUILayout.Button(b))second();
            EditorGUILayout.EndHorizontal();
        }
    }
}
