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

using KSP.UI.Screens.Flight;
using System;
using System.Collections.Generic;
using System.Linq;
 using UnityEngine;

namespace JSIAdvTransparentPods
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class JSIAdvTransparentPods : MonoBehaviour
    {
        public static JSIAdvTransparentPods Instance;
        internal static List<Part> PartstoFilterfromIVADict;
        private GameObject baseGO;
        internal Camera IVAcamera;
        internal Transform IVAcameraTransform;
        internal Camera Maincamera;
        internal Transform MaincameraTransform;
        private Component IVACamJSICameraCuller;
        private Part crewEVAFromPart;
        private Part crewEVAToPart;

        public void Awake()
        {
            JSIAdvPodsUtil.Log_Debug("OnAwake in {0}", HighLogic.LoadedScene);
            if (Instance != null)
            {
                JSIAdvPodsUtil.Log_Debug("Instance already exists, so destroying this one");
                Destroy(this);
            }
            if (!HighLogic.LoadedSceneIsFlight)
            {
                JSIAdvPodsUtil.Log_Debug("Not in Flight, so destroying this instance");
                Destroy(this);
            }
            //DontDestroyOnLoad(this);
            Instance = this;
            GameEvents.OnMapEntered.Add(TurnoffIVACamera);
            GameEvents.OnMapExited.Add(TurnonIVACamera);
            
            PartstoFilterfromIVADict = new List<Part>();
        }
        
        public void Update()
        {
            if (Time.timeSinceLevelLoad < 1f)
                return;

            //If Stock Overlay Cam is On or we are NOT in Flight camera mode (IE. Map or IVA mode), turn OFF our camera.
            if (JSIAdvPodsUtil.StockOverlayCamIsOn || 
                (HighLogic.LoadedSceneIsFlight && CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.Flight))
            {
                TurnoffIVACamera();
                return;
            }
                
            //So we are in flight cam mode, the stock camera overlay is not on.
            //Check our IVACamera exists, if it doesn't, create one.
            if (IVAcamera == null)
                CreateIVACamera();
            TurnonIVACamera();
        }

        public void LateUpdate()
        {
            if (Time.timeSinceLevelLoad < 1f || CameraManager.Instance == null)
                return;
            
            //If the Stock Overlay camera is not on or we are in flight cam mode.
            if (!JSIAdvPodsUtil.StockOverlayCamIsOn && CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Flight)
            {
                //This is a bit of a performance hit, we are checking ALL loaded vessels to filter out NON JSIAdvTransparentPods.
                //PartstoFilterFromIVADict will contain all loaded vessels that are not JSIAdvTransparentPods, as well as any that are but are too far from the 
                //camera or are set to auto or OFF.
                foreach (Vessel vsl in FlightGlobals.Vessels.Where(p => p.loaded && JSIAdvPodsUtil.ValidVslType(p)))
                {
                    foreach (Part part in vsl.parts.Where(vp => vp.internalModel != null))
                    {
                        if (!part.Modules.Contains("JSIAdvTransparentPod"))
                        {
                            if (!PartstoFilterfromIVADict.Contains(part))
                                PartstoFilterfromIVADict.Add(part);
                        }
                    }
                }
            }

        }

        public void OnDestroy()
        {
            JSIAdvPodsUtil.Log_Debug("OnDestroy");
            DestroyIVACamera();
            GameEvents.OnMapEntered.Remove(TurnoffIVACamera);
            GameEvents.OnMapExited.Remove(TurnonIVACamera);
        }

        internal void CreateIVACamera()
        {
            JSIAdvPodsUtil.Log_Debug("CreateIVACamera");
            //Create a new IVA camera if one does not exist.
            if (IVAcamera == null)
            {
                //Create a new Gameobject to attach everything to.
                //Attach the IVA Camera to it.
                if (baseGO == null)
                    baseGO = new GameObject("JSIAdvTransparentPods");
                IVAcamera = baseGO.gameObject.AddComponent<Camera>();
                IVAcamera.clearFlags = CameraClearFlags.Depth;
                IVAcameraTransform = IVAcamera.transform;
                if (HighLogic.LoadedSceneIsFlight)
                {
                    //Get the Main Flight camera.
                    Maincamera = FlightCamera.fetch.mainCamera;
                    //The IVA camera Transform needs to be parented to the InternalSpace Transform.
                    IVAcameraTransform.parent = InternalSpace.Instance.transform;
                    MaincameraTransform = Maincamera.transform;
                }
                else //Editor
                {
                    //Get the Main Flight camera.
                    //Maincamera = EditorCamera.Instance.cam;
                    Maincamera = JSIAdvPodsUtil.GetCameraByName("Main Camera");
                    //The IVA camera Transform needs to be parented to the Main Camera Transform.
                    //IVAcameraTransform.parent = EditorCamera.Instance.transform;
                    IVAcameraTransform.parent = Maincamera.transform;
                    MaincameraTransform = Maincamera.transform;
                }
                
                //Depth of 3 is above the Main Cameras.
                IVAcamera.depth = 3f;
                IVAcamera.fieldOfView = Maincamera.fieldOfView;
                IVAcamera.farClipPlane = 200f;
                //Show Only Kerbals and Internal Space layers.
                IVAcamera.cullingMask = 1114112;
                //Attach a Culler class to the camera to cull objects we don't want rendered.
                if (IVACamJSICameraCuller == null && HighLogic.LoadedSceneIsFlight)
                    IVACamJSICameraCuller = IVAcamera.gameObject.AddComponent<JSIIVACameraEvents>();
            }
            //Finally turn the new camera on.
            TurnonIVACamera();
        }

        internal void DestroyIVACamera()
        {
            if (IVAcamera != null)
            {
                JSIAdvPodsUtil.Log_Debug("DestroyIVACamera");
                Destroy(IVACamJSICameraCuller);
                Destroy(IVAcamera);
                baseGO.DestroyGameObject();
            }
        }

        internal void TurnoffIVACamera()
        {
            if (IVAcamera != null)
            {
                if (IVAcamera.enabled)
                    IVAcamera.enabled = false;
                if (IVACamJSICameraCuller != null)
                    IVACamJSICameraCuller.gameObject.SetActive(false);
            }
        }

        internal void TurnonIVACamera()
        {
            if (IVAcamera != null)
            {
                if (!IVAcamera.enabled)
                    IVAcamera.enabled = true;
                if (IVACamJSICameraCuller != null)
                {
                    if (!IVACamJSICameraCuller.gameObject.activeInHierarchy)
                        IVACamJSICameraCuller.gameObject.SetActive(true);
                }
            }
        }
    }

    //This Class is Attached to the JSIIVA Camera object to trigger PreCull and PostRender events on the camera.
    //It will filter/cull out any Internals from the Camera that are in the list: JSIAdvTransparentPods.Instance.PartstoFilterfromIVADict
    //This list is populated with all NON JSIAdvTransparentPods and any filtered JSIAdvTransparentPods (eg: Mode turned to OFF or AUTO).
    public class JSIIVACameraEvents : MonoBehaviour
    {
        //private float overlayFrame = 0;
        private Camera camera;
        private int precullMsgCount = 0;
        void Start()
        {
            camera = GetComponent<Camera>();
            JSIAdvPodsUtil.Log_Debug("Object attached to Camera {0}" , camera.name);
        }

        public void OnPreCull()
        {
            //If the IVA and Main camera transforms are not null (should't be) position and rotate the IVACamera correctly.
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (JSIAdvTransparentPods.Instance.IVAcameraTransform != null &&
                  JSIAdvTransparentPods.Instance.MaincameraTransform != null && InternalSpace.Instance != null)
                {
                    JSIAdvTransparentPods.Instance.IVAcameraTransform.position =
                        InternalSpace.WorldToInternal(JSIAdvTransparentPods.Instance.MaincameraTransform.position);
                    JSIAdvTransparentPods.Instance.IVAcameraTransform.rotation =
                        InternalSpace.WorldToInternal(JSIAdvTransparentPods.Instance.MaincameraTransform.rotation);
                    JSIAdvTransparentPods.Instance.IVAcamera.fieldOfView =
                        JSIAdvTransparentPods.Instance.Maincamera.fieldOfView;
                }
            }
            else
            {
                if (JSIAdvTransparentPods.Instance.IVAcameraTransform != null &&
                  JSIAdvTransparentPods.Instance.MaincameraTransform != null && EditorCamera.Instance != null)
                {
                    JSIAdvTransparentPods.Instance.IVAcameraTransform.position =
                        JSIAdvTransparentPods.Instance.MaincameraTransform.position;
                    JSIAdvTransparentPods.Instance.IVAcameraTransform.rotation =
                        JSIAdvTransparentPods.Instance.MaincameraTransform.rotation;
                    JSIAdvTransparentPods.Instance.IVAcamera.fieldOfView =
                        JSIAdvTransparentPods.Instance.Maincamera.fieldOfView;
                    //JSIAdvPodsUtil.DumpCameras();
                }
            }
            

            for (int i = 0; i < JSIAdvTransparentPods.PartstoFilterfromIVADict.Count; i++)
            {
                try
                {
                    JSIAdvTransparentPods.PartstoFilterfromIVADict[i].internalModel.SetVisible(false);
                }
                catch (Exception ex)
                {
                    //if (precullMsgCount < 10)
                    //{
                    //    JSIAdvPodsUtil.Log_Debug("Unable to Precull internalModel for part {0}", JSIAdvTransparentPods.Instance.PartstoFilterfromIVADict[i].craftID);
                    //    JSIAdvPodsUtil.Log_Debug("Err : {0}", ex.Message);
                    //    precullMsgCount++;
                    //}
                }
            }
        }
        
        public void OnPostRender()
        {
            for (int i = 0; i < JSIAdvTransparentPods.PartstoFilterfromIVADict.Count; i++)
            {
                try
                {
                    JSIAdvTransparentPods.PartstoFilterfromIVADict[i].internalModel.SetVisible(true);
                }
                catch (Exception)
                {
                    //JSIAdvTPodsUtil.Log_Debug("Unable to PostRender internalModel for part {0}" , JSIAdvTransparentPods.Instance.PartstoFilterfromIVADict[i].craftID);
                }
            }
        }
    }
}
