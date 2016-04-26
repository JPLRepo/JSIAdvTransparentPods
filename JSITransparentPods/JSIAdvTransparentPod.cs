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
 * RJSIAdvTransparentPods is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
 * for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with JSIAdvTransparentPods.  If not, see <http://www.gnu.org/licenses/>.
 ****************************************************************************/
using System;
using System.Collections.Generic;
 using System.Linq;
 using KSP.UI.Screens;
 using KSP.UI.Screens.Flight;
using UnityEngine;

namespace JSIAdvTransparentPods
{
    public class JSIAdvTransparentPod : PartModule
    {
        [KSPField]
        public string transparentTransforms = string.Empty;

        [KSPField]
        public string transparentShaderName = "Legacy Shaders/Transparent/Specular";

        [KSPField]
        public string opaqueShaderName = string.Empty;

        [KSPField]
        public bool restoreShadersOnIVA = true;

        [KSPField]
        public bool disableLoadingInEditor;

        [KSPField]
        public float distanceToCameraThreshold = 50f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "TransparentPod")] //ON = transparentpod on, OFF = transparentpod off, AUTO = on when focused.
        public string transparentPodSetting = "ON";

        [KSPEvent(active = true, guiActive = true, guiActiveUnfocused = true, guiActiveEditor = true, unfocusedRange = 5f, name = "eventToggleTransparency", guiName = "TransparentPod")]
        public void eventToggleTransparency()
        {
            switch (transparentPodSetting)
            {
                case "ON":
                    transparentPodSetting = "OFF";
                    break;

                case "OFF":
                    transparentPodSetting = "AUTO";
                    break;

                default:
                    transparentPodSetting = "ON";
                    break;
            }
        }
        private Part knownRootPart;
        private Vessel lastActiveVessel;
        private Shader transparentShader, opaqueShader, DepthMaskShader;
        private bool hasOpaqueShader;
        private string DepthMaskShaderName = "DepthMask";
        private readonly Dictionary<Transform, Shader> shadersBackup = new Dictionary<Transform, Shader>();
        private bool mouseOver;
        private bool setVisible;
        private float distanceToCamera;
        private int frameCounter = 0;
        private Quaternion MagicalVoodooRotation = new Quaternion(0, 0.7f, -0.7f, 0);  //We still need this for Editor Mode?


        public override string GetInfo()
        {
            return "The windows of this capsule have had advanced cleaning.";
        }

        public override void OnStart(StartState state)
        {
            JSIAdvPodsUtil.Log_Debug("OnStart {0} {1} in state {2}" , part.craftID , part.name, state);
            if (state == StartState.Editor && disableLoadingInEditor)
            {
                // Early out for people who want to disable transparency in
                // the editor due to low-spec computers.
                return;
            }

            DepthMaskShader = Shader.Find(DepthMaskShaderName);

            if (distanceToCameraThreshold > 200)
                distanceToCameraThreshold = 200;
            
            if (!string.IsNullOrEmpty(opaqueShaderName))
            {
                opaqueShader = Shader.Find(opaqueShaderName);
                if (transparentShader == null)
                {
                    JSIAdvPodsUtil.Log("opaqueShader {0} not found.", opaqueShaderName);
                }
                else
                {
                    hasOpaqueShader = true;
                }
            }

            // In Editor, the camera we want to change is called "Main Camera". In flight, the camera to change is
            // "Camera 00", i.e. close range camera.

            if (state == StartState.Editor)
            {
                // I'm not sure if this change is actually needed, even. Main Camera's culling mask seems to already include IVA objects,
                // they just don't normally spawn them.
                JSIAdvPodsUtil.SetCameraCullingMaskForIVA("Main Camera", true);
            }
            else
            {
                GameEvents.onVesselChange.Add(CheckStowaways);
            }

            // If the internal model has not yet been created, try creating it and log the exception if we fail.
            if (part.internalModel == null)
            {
                try
                {
                    part.CreateInternalModel();
                }
                catch (Exception e)
                {
                    JSIAdvPodsUtil.Log("failed to create internal model in Onstart");
                    Debug.LogException(e, this);
                }
            }

            if (part.internalModel == null && part.partInfo != null)
            {
                // KSP 1.0.x introduced a new feature where it doesn't appear
                // to fully load parts if they're not the root.  In particular,
                // that CreateInternalModel() call above here returns null for
                // non-root parts until one exits the VAB and returns.
                // If the internalModel doesn't exist yet, I find the config
                // for this part, extract the INTERNAL node, and try to create
                // the model myself. Awfully roundabout.

                JSIAdvPodsUtil.Log_Debug("Let's see if anyone included parts so I can assemble the interior");
                ConfigNode ipNameNode = (from cfg in GameDatabase.Instance.GetConfigs("PART")
                                         where cfg.url == part.partInfo.partUrl
                                         select cfg.config.GetNode("INTERNAL")).FirstOrDefault();

                if (ipNameNode != null)
                {
                    part.internalModel = part.AddInternalPart(ipNameNode);
                }
            }

            // Apply shaders to transforms on startup.
            if (!string.IsNullOrEmpty(transparentTransforms))
            {
                try
                {
                    transparentShader = Shader.Find(transparentShaderName);
                }
                catch (Exception ex)
                {
                    JSIAdvPodsUtil.Log("Get transparentShader {0} failed. Error: {1}", transparentShaderName, ex);
                }
                if (transparentShader == null)
                {
                    JSIAdvPodsUtil.Log("transparentShader {0} not found.", transparentShaderName);
                }
                foreach (string transformName in transparentTransforms.Split('|'))
                {
                    try
                    {
                        Transform tr = part.FindModelTransform(transformName.Trim());
                        if (tr != null)
                        {
                            //We both change the shader and backup the original shader so we can undo it later.
                            Shader backupShader = tr.GetComponent<Renderer>().material.shader;
                            tr.GetComponent<Renderer>().material.shader = transparentShader;
                            shadersBackup.Add(tr, backupShader);
                        }
                        if (part.internalModel != null)
                        {
                            Transform itr = part.internalModel.FindModelTransform(transformName.Trim());
                            if (itr != null)
                            {
                                // We both change the shader and backup the original shader so we can undo it later.
                                Shader backupShader = itr.GetComponent<Renderer>().material.shader;
                                itr.GetComponent<Renderer>().material.shader = transparentShader;
                                shadersBackup.Add(itr, backupShader);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, this);
                    }
                }
            }


            // If we ended up with an existing internal model, 
            if (part.internalModel != null)
            {
                // Rotate it now, so that it is shown correctly in the editor. - OLD Method.
                if (state == StartState.Editor)
                {
                    // Just rotating the internal is sufficient in this case.
                    part.internalModel.transform.localRotation = MagicalVoodooRotation;
                    //Find all Renderer's with DepthMask shader assigned to them and make them inactive as they cause Z-Fighting in the Editor and are
                    //not needed in the editor - OLD Method.
                    EditorSetDepthMaskOff();
                }
                else
                {
                    // Else this is our first startup in flight scene, we reset the IVA.
                    ResetIVA();
                }
            }
            else
            {
                // Some error-proofing. I won't bother doing this every frame, because one error message
                // should suffice, this module is not supposed to be attached to parts that have no internals in the first place.
                JSIAdvPodsUtil.Log("Wait, where's my internal model?");
            }
        }

        public void OnDestroy()
        {
            GameEvents.onVesselChange.Remove(CheckStowaways);
        }

        private void SetShaders(bool state)
        {
            if (restoreShadersOnIVA)
            {
                if (state)
                {
                    foreach (KeyValuePair<Transform, Shader> backup in shadersBackup)
                    {
                        if (backup.Key != null)
                            backup.Key.GetComponent<Renderer>().material.shader = transparentShader;
                    }
                }
                else
                {
                    foreach (KeyValuePair<Transform, Shader> backup in shadersBackup)
                    {
                        if (backup.Key != null)
                            backup.Key.GetComponent<Renderer>().material.shader = hasOpaqueShader ? opaqueShader : backup.Value;
                    }
                }
            }
        }

        private void EditorSetDepthMaskOff()
        {
            //For some reason we need to keep turning off the Renderers with the DepthMask Shader.. 
            if (part.internalModel != null)
            {
                MeshRenderer[] meshRenderers = base.GetComponentsInChildren<MeshRenderer>();
                for (int i = 0; i < meshRenderers.Length; i++)
                {
                    MeshRenderer meshRenderer = meshRenderers[i];
                    if (meshRenderer.material.shader == DepthMaskShader)
                        meshRenderer.enabled = false;
                }
                SkinnedMeshRenderer[] skinnedMeshRenderers = base.GetComponentsInChildren<SkinnedMeshRenderer>();
                for (int j = 0; j < skinnedMeshRenderers.Length; j++)
                {
                    SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRenderers[j];
                    if (skinnedMeshRenderer.material.shader == DepthMaskShader)
                        skinnedMeshRenderer.enabled = false;
                }
            }
        }

        public void LateUpdate()
        {
            if (Time.timeSinceLevelLoad < 1f) return;
            
            //Reset the mouseOver flag.
            mouseOver = false;
        }
        
        private void ResetIVA()
        {
            try
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    JSIAdvPodsUtil.Log_Debug("Need to reset IVA in part {0}({1})", part.name, part.craftID);

                    // Now the cruical bit.
                    // If the root part changed, we actually need to recreate the IVA forcibly even if it still exists.
                    if (vessel.rootPart != knownRootPart)
                    {
                        // In this case we also need to kick the user out of IVA if they're currently in our pod,
                        // otherwise lots of things screw up in a bizarre fashion.
                        if (JSIAdvPodsUtil.UserIsInPod(part))
                        {
                            JSIAdvPodsUtil.Log_Debug("The user is in pod {0} and I need to kick them out.", part.partName);
                            CameraManager.Instance.SetCameraFlight();
                        }
                        if (part.internalModel != null)
                        {
                            Destroy(part.internalModel.gameObject);
                            part.internalModel = null;
                        }
                    }
                    // But otherwise the existing one will serve.

                    // If the internal model doesn't yet exist, this call will implicitly create it anyway.
                    // It will also initialise it, which in this case implies moving it into the correct location in internal space
                    // and populate it with crew, which is what we want.
                    part.SpawnIVA();
                    part.internalModel.SetVisible(true);
                    setVisible = true;
                    // And then we remember the root part and the active vessel these coordinates refer to.
                    knownRootPart = vessel.rootPart;
                    lastActiveVessel = FlightGlobals.ActiveVessel;
                }
            }
            catch (Exception ex)
            {
                JSIAdvPodsUtil.Log_Debug("Reset IVA failed: {0}", ex); 
            }
            
        }

        public void CheckStowaways(Vessel vsl)
        {
            // Now we need to make sure that the list of portraits in the GUI conforms to what actually is in the active vessel.
            // This is important because IVA/EVA buttons clicked on kerbals that are not in the active vessel cause problems
            // that I can't readily debug, and it shouldn't happen anyway.
            
            // Only the pods that are not the active vessel should be doing this. So if this part/vessel is not part of the active vessel then:-
            //Search the seats and where there is a kerbalRef try to Unregister them from the PortraitGallery.
            if (FlightGlobals.ActiveVessel.id != vessel.id && part.internalModel != null)
            {
                for (int i = 0; i < part.internalModel.seats.Count; i++)
                {
                    if (part.internalModel.seats[i].kerbalRef != null)
                    {
                        try
                        {
                            Portraits.DestroyPortrait(part.internalModel.seats[i].kerbalRef);
                        }
                        catch (Exception)
                        {
                            JSIAdvPodsUtil.Log_Debug("Unregister Portrait on inactive part failed {0}", part.internalModel.seats[i].kerbalRef.crewMemberName);
                        }
                    }
                }
            }
        }

        public override void OnUpdate()
        {
            if (Time.timeSinceLevelLoad < 1f) return;

            // In the editor, none of this logic should matter, even though the IVA probably exists already.
            if (HighLogic.LoadedSceneIsEditor)
            {
                // Make the internal model visible...
                part.internalModel.SetVisible(true);
                // And for a good measure we make sure the shader change has been applied.
                SetShaders(true);
                // Now we attach the restored IVA directly into the pod at zero local coordinates and rotate it,
                // so that it shows up on the main outer view camera in the correct location.
                VoodooRotate();
                setVisible = true;

                // If we are in editor mode we need to turn off the internal if the internal is in OFF or AUTO mode and not moused over.
                //Otherwise we make it visible.
                if (transparentPodSetting == "OFF" || (transparentPodSetting == "AUTO" && !mouseOver))
                {
                    SetShaders(false);
                    if (part.internalModel != null)
                        part.internalModel.SetVisible(false);
                    setVisible = false;
                }
                
                EditorSetDepthMaskOff();

                return;
            }
            

            //Now FlightScene Processing

            // If the root part changed, or the IVA is mysteriously missing, we reset it and take note of where it ended up.
            if (vessel.rootPart != knownRootPart || lastActiveVessel != FlightGlobals.ActiveVessel || part.internalModel == null)
            {
                ResetIVA();
            }

            //If we are in flight and the user has the Stock Overlay on and this part is not part of the active vessel we turn off the internal.
            // also if the user has set the LoadedInactive to False - we don't show TransparentPods that aren't on the active vessel.
            // We turn it off rather than registering it for the PreCull list because if Stock Overlay is on the JSI camera is not active.
            if (CameraManager.Instance != null && InternalSpace.Instance != null)
            {
                if (!vessel.isActiveVessel && (JSIAdvTransparentPods.Instance.StockOverlayCamIsOn || !LoadGlobals.settings.LoadedInactive))
                {
                    if (part.internalModel != null)
                        part.internalModel.SetVisible(false);
                    setVisible = false;
                    SetShaders(false);
                    JSIAdvPodsUtil.Log_Debug("Internal turned off as vessel is Not Active Vessel and stock overlay is on or LoadedInactive is False: ({0}) {1}", part.craftID, vessel.vesselName);
                    return;
                }

                //For some reason (probably performance) Squad do not actively update the position and rotation of InternalModels that are not part of the active vessel.
                if (!vessel.isActiveVessel)
                {
                    //Calculate the Vessel position and rotation and then apply that to the InternalModel position and rotation with the MagicalVoodooRotation.
                    if (part.internalModel != null)
                    {
                        Vector3 VesselPosition = part.vessel.transform.position + part.vessel.transform.rotation * part.orgPos;
                        part.internalModel.transform.position = InternalSpace.WorldToInternal(VesselPosition);
                        Quaternion VesselRotation = part.vessel.transform.rotation * part.orgRot;
                        part.internalModel.transform.rotation = InternalSpace.WorldToInternal(VesselRotation) * MagicalVoodooRotation;
                    }
                }
            }

            // If transparentPodSetting = OFF or AUTO and not the focused active part we treat the part like a non-transparent part.
            // and we turn off the shaders (if set) and the internal to the filter list and exit OnUpdate. 
            if (transparentPodSetting == "OFF" || (transparentPodSetting == "AUTO" && !mouseOver))
            {
                SetShaders(false);
                //part.internalModel.SetVisible(false);
                if (!JSIAdvTransparentPods.Instance.PartstoFilterfromIVADict.Contains(part))
                    JSIAdvTransparentPods.Instance.PartstoFilterfromIVADict.Add(part);
                setVisible = false;
                return;
            }
            
            // So we do have an internal model, right?
            if (part.internalModel != null)
            {
                // If the current part is not part of the active vessel, we calculate the distance from the part to the flight camera.
                // If this distance is > distanceToCameraThreshold metres we turn off transparency for the part.
                // Uses Maths calcs intead of built in Unity functions as this is up to 5 times faster.
                if (!vessel.isActiveVessel)
                {
                    Vector3 heading;
                    Transform thisPart = part.transform;
                    Transform flightCamera = FlightCamera.fetch.transform;
                    heading.x = thisPart.position.x - flightCamera.position.x;
                    heading.y = thisPart.position.y - flightCamera.position.y;
                    heading.z = thisPart.position.z - flightCamera.position.z;
                    var distanceSquared = heading.x * heading.x + heading.y * heading.y + heading.z * heading.z;
                    distanceToCamera = Mathf.Sqrt(distanceSquared);

                    if (distanceToCamera > distanceToCameraThreshold)
                    {
                        SetShaders(false);
                        //part.internalModel.SetVisible(false);
                        if (!JSIAdvTransparentPods.Instance.PartstoFilterfromIVADict.Contains(part))
                            JSIAdvTransparentPods.Instance.PartstoFilterfromIVADict.Add(part);
                        setVisible = false;
                        return;
                    }
                }

                //If inactive vessel IVAs are turned on via the settings then we:
                //Check for obstructions between this IVA and the Camera that may be on lower layers and turn off the IVA if there is one.
                //Not a perfect solution..... and bad performance-wise. 
                if (LoadGlobals.settings.LoadedInactive)
                {
                    if (JSIAdvTransparentPods.Instance != null && setVisible && CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Flight)
                    {
                        if (JSIAdvTransparentPods.Instance.IVAcameraTransform != null)
                        {
                            Transform IVAtoWorld = new GameObject().transform;
                            IVAtoWorld.position = InternalSpace.InternalToWorld(part.internalModel.transform.position);
                            IVAtoWorld.rotation = InternalSpace.InternalToWorld(part.internalModel.transform.rotation);
                            Transform IVACameratoWorld = new GameObject().transform;
                            IVACameratoWorld.position = InternalSpace.InternalToWorld(JSIAdvTransparentPods.Instance.IVAcameraTransform.position);
                            IVAtoWorld.rotation = InternalSpace.InternalToWorld(JSIAdvTransparentPods.Instance.IVAcameraTransform.rotation);
                            if (JSIAdvPodsUtil.IsIVAObstructed(IVAtoWorld, JSIAdvTransparentPods.Instance.IVAcameraTransform))
                            {
                                if (!JSIAdvTransparentPods.Instance.PartstoFilterfromIVADict.Contains(part))
                                    JSIAdvTransparentPods.Instance.PartstoFilterfromIVADict.Add(part);
                            }
                            IVAtoWorld.gameObject.DestroyGameObject();
                            IVACameratoWorld.gameObject.DestroyGameObject();
                        }
                    }
                }
                
                // Make the internal model visible...
                // And for a good measure we make sure the shader change has been applied.
                SetShaders(true);
                if (JSIAdvTransparentPods.Instance.PartstoFilterfromIVADict.Contains(part))
                    JSIAdvTransparentPods.Instance.PartstoFilterfromIVADict.Remove(part);
                setVisible = true;
                part.internalModel.SetVisible(true); 
            }
            else
            {
                JSIAdvPodsUtil.Log("Where is my Internal model for : {0}" , part.craftID);
            }
        }

        public void VoodooRotate()
        {
            // Now we attach the restored IVA directly into the pod at zero local coordinates and rotate it,
            // so that it shows up on the main outer view camera in the correct location.
            part.internalModel.transform.parent = part.transform;
            part.internalModel.transform.localRotation = MagicalVoodooRotation;
            part.internalModel.transform.localPosition = Vector3.zero;
            part.internalModel.transform.localScale = Vector3.one;
        }
        
        // When mouse is over this part set a flag for the transparentPodSetting = "AUTO" setting.
        private void OnMouseOver()
        {
            mouseOver = true; 
        }
    }
}
