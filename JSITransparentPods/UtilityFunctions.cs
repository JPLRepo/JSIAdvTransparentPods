/*****************************************************************************
 * JSIAdvTransparentPods
 * =====================
 * Plugin for Kerbal Space Program
 *
 * Re-Written by JPLRepo (Jamie Leighton).
 * Based on original JSITransparentPod by Mihara (Eugene Medvedev), 
 * MOARdV, and other contributors
 * JSIAdvTransparentPods has been split off from the main RasterPropMonitor
 * project and distrubtion files and will be maintained and distributed 
 * separately going foward. But as with all free software the license 
 * continues to be the same as the original RasterPropMonitor license:
 * JSIAdvTransparentPods is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, revision
 * date 29 June 2007, or (at your option) any later version.
 * 
 * JSIAdvTransparentPods is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
 * for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with JSIAdvTransparentPods.  If not, see <http://www.gnu.org/licenses/>.
 ****************************************************************************/
using System;
using System.Collections;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace JSIAdvTransparentPods
{
    public static class JSIAdvPodsUtil
    {
        public static bool debugLoggingEnabled = true;
        public static bool cameraMaskShowsIVA;

        
        // Dump all Unity Cameras
        internal static void DumpCameras()
        {
            Debug.Log("--------- Dump Unity Cameras ------------");
            foreach (Camera c in Camera.allCameras)
            {
                string layernames = "";
                int val = c.cullingMask;
                var arr = new BitArray(BitConverter.GetBytes(val));
                for (int i = 0; i < arr.Length; i++)
                {
                    if (arr[i]) //True = 1
                    {
                        layernames += LayerMask.LayerToName(i) + ",";
                    }
                }
                Debug.Log("Camera " + c.name + " cullingmask " + c.cullingMask + " depth " + c.depth + " farClipPlane " + c.farClipPlane + " nearClipPlane " + c.nearClipPlane + "Layernames: " + layernames);
                Debug.Log("--- Transform Pos " + c.transform.position + " Transform Rot " + c.transform.rotation + " Transform LocalPos " + c.transform.localPosition + " Transform LocalRot " + c.transform.localRotation);
                Debug.Log("--- Parent " + c.transform.parent.name);
            }
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (EditorCamera.Instance.cam != null)
                {
                    Camera c = EditorCamera.Instance.cam;
                    string layernames = "";
                    int val = c.cullingMask;
                    var arr = new BitArray(BitConverter.GetBytes(val));
                    for (int i = 0; i < arr.Length; i++)
                    {
                        if (arr[i]) //True = 1
                        {
                            layernames += LayerMask.LayerToName(i) + ",";
                        }
                    }
                    Debug.Log("Camera " + c.name + " cullingmask " + c.cullingMask + " depth " + c.depth + " farClipPlane " + c.farClipPlane + " nearClipPlane " + c.nearClipPlane + "Layernames: " + layernames);
                    Debug.Log("--- Transform Pos " + c.transform.position + " Transform Rot " + c.transform.rotation + " Transform LocalPos " + c.transform.localPosition + " Transform LocalRot " + c.transform.localRotation);
                    Debug.Log("--- Parent " + c.transform.parent.name);
                }
            }
            Debug.Log("--------------------------------------");
        }
        
        internal static void DumpGameObjectChilds(GameObject go, string pre, StringBuilder sb)
        {
            bool first = pre == "";
            List<GameObject> neededChilds = new List<GameObject>();
            int count = go.transform.childCount;
            for (int i = 0; i < count; i++)
            {
                GameObject child = go.transform.GetChild(i).gameObject;
                if (!child.GetComponent<Part>() && child.name != "main camera pivot")
                    neededChilds.Add(child);
            }

            count = neededChilds.Count;

            sb.Append(pre);
            if (!first)
            {
                sb.Append(count > 0 ? "--+" : "---");
            }
            else
            {
                sb.Append("+");
            }
            sb.AppendFormat("{0} T:{1} L:{2} ({3})\n", go.name, go.tag, go.layer, LayerMask.LayerToName(go.layer));

            string front = first ? "" : "  ";
            string preComp = pre + front + (count > 0 ? "| " : "  ");

            Component[] comp = go.GetComponents<Component>();

            for (int i = 0; i < comp.Length; i++)
            {
                if (comp[i] is Transform)
                {
                    sb.AppendFormat("{0}  {1} - {2}\n", preComp, comp[i].GetType().Name, go.transform.name);
                }
                else
                {
                    sb.AppendFormat("{0}  {1} - {2}\n", preComp, comp[i].GetType().Name, comp[i].name);
                }
            }

            sb.AppendLine(preComp);

            for (int i = 0; i < count; i++)
            {
                DumpGameObjectChilds(neededChilds[i], i == count - 1 ? pre + front + " " : pre + front + "|", sb);
            }
        }

        public static void SetCameraCullingMaskForIVA(string cameraName, bool flag)
        {
            Camera thatCamera = GetCameraByName(cameraName);

            if (thatCamera != null)
            {
                if (flag)
                {
                    thatCamera.cullingMask |= 1 << 16 | 1 << 20;
                }
                else
                {
                    thatCamera.cullingMask &= ~(1 << 16 | 1 << 20);
                }
            }
            else if (flag != cameraMaskShowsIVA)
            {
                Log("Could not find camera {0} to change its culling mask, check your code.", cameraName);
                cameraMaskShowsIVA = false;
            }
        }

        public static bool IsPodTransparent(Part thatPart)
        {
            return thatPart.Modules.OfType<JSIAdvTransparentPod>().Any();
        }

        public static bool ActiveKerbalIsLocal(this Part thisPart)
        {
            return FindCurrentKerbal(thisPart) != null;
        }

        public static int CurrentActiveSeat(this Part thisPart)
        {
            Kerbal activeKerbal = thisPart.FindCurrentKerbal();
            return activeKerbal != null ? activeKerbal.protoCrewMember.seatIdx : -1;
        }

        public static Kerbal FindCurrentKerbal(this Part thisPart)
        {
            if (thisPart.internalModel == null || !VesselIsInIVA(thisPart.vessel))
                return null;
            // InternalCamera instance does not contain a reference to the kerbal it's looking from.
            // So we have to search through all of them...
            return (from thatSeat in thisPart.internalModel.seats
                    where thatSeat.kerbalRef != null
                    where thatSeat.kerbalRef.eyeTransform == InternalCamera.Instance.transform.parent
                    select thatSeat.kerbalRef).FirstOrDefault();
        }

        public static Camera GetCameraByName(string name)
        {
            for (int i = 0; i < Camera.allCamerasCount; ++i)
            {
                if (Camera.allCameras[i].name == name)
                {
                    return Camera.allCameras[i];
                }
            }
            return null;
        }

        public static bool StockOverlayCamIsOn
        {
            get
            {
                Camera StockOverlayCamera = JSIAdvPodsUtil.GetCameraByName("InternalSpaceOverlay Host");
                if (StockOverlayCamera != null) return true;
                else return false;
            }
        }

        public static bool VesselIsInIVA(Vessel thatVessel)
        {
            // Inactive IVAs are renderer.enabled = false, this can and should be used...
            // ... but now it can't because we're doing transparent pods, so we need a more complicated way to find which pod the player is in.
            return HighLogic.LoadedSceneIsFlight && IsActiveVessel(thatVessel) && IsInIVA();
        }

        public static bool UserIsInPod(Part thisPart)
        {

            // Just in case, check for whether we're not in flight.
            if (!HighLogic.LoadedSceneIsFlight)
                return false;

            // If we're not in IVA, or the part does not have an instantiated IVA, the user can't be in it.
            if (!VesselIsInIVA(thisPart.vessel) || thisPart.internalModel == null)
                return false;

            // Now that we got that out of the way, we know that the user is in SOME pod on our ship. We just don't know which.
            // Let's see if he's controlling a kerbal in our pod.
            if (ActiveKerbalIsLocal(thisPart))
                return true;

            // There still remains an option of InternalCamera which we will now sort out.
            if (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Internal)
            {
                // So we're watching through an InternalCamera. Which doesn't record which pod we're in anywhere, like with kerbals.
                // But we know that if the camera's transform parent is somewhere in our pod, it's us.
                // InternalCamera.Instance.transform.parent is the transform the camera is attached to that is on either a prop or the internal itself.
                // The problem is figuring out if it's in our pod, or in an identical other pod.
                // Unfortunately I don't have anything smarter right now than get a list of all transforms in the internal and cycle through it.
                // This is a more annoying computation than looking through every kerbal in a pod (there's only a few of those,
                // but potentially hundreds of transforms) and might not even be working as I expect. It needs testing.
                return thisPart.internalModel.GetComponentsInChildren<Transform>().Any(thisTransform => thisTransform == InternalCamera.Instance.transform.parent);
            }

            return false;
        }

        public static bool IsActiveVessel(Vessel thatVessel)
        {
            return (HighLogic.LoadedSceneIsFlight && thatVessel != null && thatVessel.isActiveVessel);
        }

        public static bool IsInIVA()
        {
            try
            {
                return CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA || CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Internal;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static bool ValidVslType(Vessel v)
        {
            switch (v.vesselType)
            {
                case VesselType.Base:
                case VesselType.Lander:
                case VesselType.Probe:
                case VesselType.Rover:
                case VesselType.Ship:
                case VesselType.Station:
                    return true;

                default:
                    return false;
            }
        }
        
        internal static IEnumerator drawLine(Vector3 start, Vector3 end, Color color, float duration = 0.2f)
        {
            GameObject myLine = new GameObject();
            myLine.transform.position = start;
            myLine.AddComponent<LineRenderer>();
            LineRenderer lr = myLine.GetComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Particles/Additive"));
            lr.SetColors(color, color);
            lr.SetWidth(0.1f, 0.1f);
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            yield return new WaitForSeconds(duration);
            GameObject.Destroy(myLine);
        }

        #region Logging
        // Logging Functions
        // Name of the Assembly that is running this MonoBehaviour
        internal static String _AssemblyName
        { get { return Assembly.GetExecutingAssembly().GetName().Name; } }
        
        /// <summary>
        /// Logging to the debug file
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>			

        internal static void Log_Debug(String Message, params object[] strParams)
        {
            if (debugLoggingEnabled)
            {
                Log("DEBUG: " + Message, strParams);
            }
        }

        /// <summary>
        /// Logging to the log file
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>

        internal static void Log(String Message, params object[] strParams)
        {
            Message = String.Format(Message, strParams);                  // This fills the params into the message
            String strMessageLine = String.Format("{0},{2},{1}",
                DateTime.Now, Message,
                _AssemblyName);                                           // This adds our standardised wrapper to each line
            Debug.Log(strMessageLine);                        // And this puts it in the log
        }
        #endregion Logging
    }

}

