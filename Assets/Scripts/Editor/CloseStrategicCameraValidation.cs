using System;
using UnityEditor;
using UnityEngine;
using PolyRisk.Systems;

namespace PolyRisk.Editor
{
    /// <summary>Release camera geometry/input regressions; does not build or mutate assets.</summary>
    public static class CloseStrategicCameraValidation
    {
        static int checks;
        static void Check(bool ok,string name)
        {
            checks++;
            if(!ok) throw new Exception("CLOSE_CAMERA FAIL "+name);
        }
        public static void Run()
        {
            checks=0;
            Check(BoardInteractionManager.PermanentCloseFactor==1.40f,"approved permanent factor");
            Check(Mathf.Abs(BoardInteractionManager.CloseStrategicSize(18.2081718445f)-13.0058370318f)<.00001f,"430x830 PASS2F.1 reference / 1.40");
            Check(Mathf.Abs(BoardInteractionManager.CloseStrategicSize(11.2809009552f)-8.0577863966f)<.00001f,"1200x900 PASS2F.1 reference / 1.40");
            foreach(float size in new[]{1f,8f,18.2081718445f,30f})
                Check(Mathf.Abs(size/BoardInteractionManager.CloseStrategicSize(size)-1.4f)<.00001f,"factor independent of format "+size);
            Check(!BoardInteractionManager.ExceedsDragThreshold(Vector2.zero,new Vector2(6,7),10),"touch jitter remains tap");
            Check(BoardInteractionManager.ExceedsDragThreshold(Vector2.zero,new Vector2(6,8),10),"threshold diagonal becomes drag");
            Check(BoardInteractionManager.ExceedsDragThreshold(Vector2.zero,new Vector2(-10,0),10),"negative axis drag");
            Check(!BoardInteractionManager.ExceedsDragThreshold(Vector2.zero,new Vector2(15,0),20),"same UI jitter at 2x pixels");
            Check(BoardInteractionManager.ExceedsDragThreshold(Vector2.zero,new Vector2(20,0),20),"scaled UI drag threshold");
            var rotation=BoardInteractionManager.DefaultRotation(Quaternion.Euler(57,45,5));
            Check(Mathf.Abs(rotation.eulerAngles.x-40f)<.0001f&&Mathf.Abs(rotation.eulerAngles.y-45f)<.0001f&&Mathf.Abs(rotation.eulerAngles.z)<.0001f,"fixed isometric orientation");
            // Navigation's inset remains smaller than each useful half-extent, including a
            // viewport larger than the board. Thus edge points cannot become inaccessible.
            var min=new Vector2(-15,-10);var max=new Vector2(15,10);
            foreach(var half in new[]{new Vector2(6.5f,7),new Vector2(10,5),new Vector2(100,100)})
            {
                foreach(var edge in new[]{min,max,new Vector2(min.x,max.y),new Vector2(max.x,min.y)})
                {
                    var center=BoardInteractionManager.ClampExplorationCenter(edge,min,max,half);
                    Check(Mathf.Abs(center.x-edge.x)<=half.x&&Mathf.Abs(center.y-edge.y)<=half.y,"all extremes reachable "+half+edge);
                }
                var bounded=BoardInteractionManager.ClampExplorationCenter(Vector2.one*10000,min,max,half);
                Check(bounded.x<=max.x&&bounded.y<=max.y&&bounded.x>=min.x&&bounded.y>=min.y,"bounded exploration "+half);
            }
            var rig=UnityEngine.Object.FindObjectOfType<BoardInteractionManager>();
            if(rig!=null)
            {
                var serialized=new SerializedObject(rig);
                Check(serialized.FindProperty("movementZoomFactor").floatValue==1f,"zero additional action zoom");
                Check(Mathf.Abs(serialized.FindProperty("arrivalHoldDuration").floatValue-.25f)<.00001f,"250ms lock hold unchanged");
                Check(serialized.FindProperty("followHorizontalDamping").floatValue<serialized.FindProperty("followVerticalDamping").floatValue,"horizontal priority preserved");
            }
            Debug.Log("CLOSE_CAMERA_EDITOR_PASS "+checks+" checks");
        }
    }
}
