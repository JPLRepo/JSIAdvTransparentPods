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
using System.Collections.Generic;
 using System.Linq;
using System.Text;
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

        [KSPField]
        public string transparentPodDepthMaskShaderTransform = "";

        [KSPField]
        public string stockOverlayDepthMaskShaderTransform = "";

        [KSPField]
        public bool combineDepthMaskShaders = false;

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
        //private Dictionary<Transform, Shader> shadersBackup = new Dictionary<Transform, Shader>();
        private List<KeyValuePair<Transform, Shader>> shadersBackup = new List<KeyValuePair<Transform, Shader>>();
        private bool mouseOver;
        private bool setVisible;
        private float distanceToCamera;
        private int frameCounter = 0;
        private Quaternion MagicalVoodooRotation = new Quaternion(0, 0.7f, -0.7f, 0);  //We still need this for Editor Mode?
        public bool isIVAobstructed = false;
        [KSPField(isPersistant = true)]
        private string prevtransparentPodSetting = "ON";
        [KSPField(isPersistant = true)]
        private bool previsIVAobstructed = false;
        private JSIZFighter JSIZfightertransparent;
        private JSIZFighter JSIZfighterStock;
        private Transform transparentPodTransform = null;
        private Transform stockOverlayTransform = null;


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
            //JSIAdvPodsUtil.DumpCameras();
            if (state == StartState.Editor)
            {
                // I'm not sure if this change is actually needed, even. Main Camera's culling mask seems to already include IVA objects,
                // they just don't normally spawn them.
                JSIAdvPodsUtil.SetCameraCullingMaskForIVA("Main Camera", true);
                //JSIAdvPodsUtil.SetCameraCullingMaskForIVA("Main Camera", false);
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
                            shadersBackup.Add(new KeyValuePair<Transform, Shader>(tr, backupShader));
                        }
                        if (part.internalModel != null)
                        {
                            Transform itr = part.internalModel.FindModelTransform(transformName.Trim());
                            if (itr != null)
                            {
                                // We both change the shader and backup the original shader so we can undo it later.
                                Shader backupShader = itr.GetComponent<Renderer>().material.shader;
                                itr.GetComponent<Renderer>().material.shader = transparentShader;
                                shadersBackup.Add(new KeyValuePair<Transform, Shader>(tr, backupShader));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, this);
                    }
                }
            }

            //If debugging is ON dump the InternalModel transforms.
            if (JSIAdvPodsUtil.debugLoggingEnabled && part.internalModel != null)
            {
                StringBuilder sb = new StringBuilder();
                JSIAdvPodsUtil.DumpGameObjectChilds(part.internalModel.gameObject.transform.parent.gameObject, part.name + " Internal ", sb);
                print("[JSIATP] " + sb);
            }

            //Check and process transparentPodDepthMaskShaderTransform field.
            if (!string.IsNullOrEmpty(transparentPodDepthMaskShaderTransform) && part.internalModel != null)
            {
                transparentPodTransform = part.internalModel.gameObject.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == transparentPodDepthMaskShaderTransform);
                if (transparentPodTransform == null)
                {
                    transparentPodDepthMaskShaderTransform = "";
                    JSIAdvPodsUtil.Log("Unable to find transparentPodDepthMaskShaderTransform {0} in InternalModel", transparentPodDepthMaskShaderTransform);
                }
                    
            }

            //Check and process stockOverlayDepthMaskShaderTransform field.
            if (!string.IsNullOrEmpty(stockOverlayDepthMaskShaderTransform) && part.internalModel != null)
            {
                stockOverlayTransform = part.internalModel.gameObject.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == stockOverlayDepthMaskShaderTransform);
                if (stockOverlayTransform == null)
                {
                    transparentPodDepthMaskShaderTransform = "";
                    JSIAdvPodsUtil.Log("Unable to find stockOverlayDepthMaskShaderTransform {0} in InternalModel", stockOverlayDepthMaskShaderTransform);

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
                    SetDepthMask();
                    //Turn on Zfighters for the depthmask overlays if they are present.
                    /*if (transparentPodTransform != null && JSIZfightertransparent == null)
                    {
                        JSIZfightertransparent = new JSIZFighter();
                        JSIZfightertransparent.Start(transparentPodTransform);
                    }
                    if (stockOverlayTransform != null && JSIZfighterStock == null)
                    {
                        JSIZfighterStock = new JSIZFighter();
                        JSIZfighterStock.Start(stockOverlayTransform);
                    }*/
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

        private void SetDepthMask(bool enabled = false, string MeshParent = "")
        {
            
             if (part.internalModel != null)
            {
                // Look for the transform where it's name matches the passed in string
                Transform meshBase = null;
                if (MeshParent != "")
                {
                    meshBase = part.internalModel.gameObject.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == MeshParent);
                }
                //First process meshRenderers
                MeshRenderer[] meshRenderers;
                if (MeshParent == "") //If no input string get all MeshRenderers
                    meshRenderers = base.GetComponentsInChildren<MeshRenderer>();
                else
                {
                    if (meshBase != null) //If we found the transform get all the MeshRenderers that are children of that transform.
                    {
                        meshRenderers = meshBase.GetComponentsInChildren<MeshRenderer>();
                    }
                    else  //If we didn't find the transform just revert back to getting ALL of the MeshRenderers
                    {
                        meshRenderers = base.GetComponentsInChildren<MeshRenderer>();
                    }
                }
                //Now go through and turn off any MeshRenderers that have DepthMaskShader
                for (int i = 0; i < meshRenderers.Length; i++)
                {
                    MeshRenderer meshRenderer = meshRenderers[i];
                    if (meshRenderer.material.shader == DepthMaskShader)
                        meshRenderer.enabled = enabled;
                }

                //Now we do the same but for skinnedMeshRenderers
                SkinnedMeshRenderer[] skinnedMeshRenderers;
                if (MeshParent == "") //If no input string get all MeshRenderers
                    skinnedMeshRenderers = base.GetComponentsInChildren<SkinnedMeshRenderer>();
                else
                {
                    if (meshBase != null) //If we found the transform get all the MeshRenderers that are children of that transform.
                    {
                        skinnedMeshRenderers = meshBase.GetComponentsInChildren<SkinnedMeshRenderer>();
                    }
                    else  //If we didn't find the transform just revert back to getting ALL of the MeshRenderers
                    {
                        skinnedMeshRenderers = base.GetComponentsInChildren<SkinnedMeshRenderer>();
                    }
                }
                //Now go through and turn off any MeshRenderers that have DepthMaskShader
                for (int j = 0; j < skinnedMeshRenderers.Length; j++)
                {
                    SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRenderers[j];
                    if (skinnedMeshRenderer.material.shader == DepthMaskShader)
                        skinnedMeshRenderer.enabled = enabled;
                }
            }
        }

        public void LateUpdate()
        {
            if (Time.timeSinceLevelLoad < 5f) return;

            //Reset the mouseOver flag.
            mouseOver = false;

            if (HighLogic.LoadedSceneIsFlight)// || HighLogic.LoadedSceneIsEditor)
            {
                //Process Multiple Depth Mask Shaders
                //If they are not defined then by default any baked in depth mask shader will be ON in flight.
                //If there is no baked in depth mask shader that doesn't matter either, that means the WHOLE IVA will be visible.
                //If stock is OFF
                if (!JSIAdvPodsUtil.StockOverlayCamIsOn)
                {
                    //If combine masks is false then if we have a stock overlay turn it off
                    // and if we have a transparentPod overlay turn it on.
                    if (!combineDepthMaskShaders)
                    {
                        if (stockOverlayDepthMaskShaderTransform != "")
                        {
                            SetDepthMask(false, stockOverlayDepthMaskShaderTransform);
                        }
                        if (transparentPodDepthMaskShaderTransform != "")
                        {
                            SetDepthMask(true, transparentPodDepthMaskShaderTransform);
                        }
                    }
                    //If combine masks is true we turn both on if defined to create a combined overlay
                    //for transparent pod mode.
                    else
                    {
                        if (stockOverlayDepthMaskShaderTransform != "")
                        {
                            SetDepthMask(true, stockOverlayDepthMaskShaderTransform);
                        }
                        if (transparentPodDepthMaskShaderTransform != "")
                        {
                            SetDepthMask(true, transparentPodDepthMaskShaderTransform);
                        }
                    }
                }
                //If stock is ON
                else
                {
                    //Regardless of combine mask setting, then if we have a stock overlay turn it on
                    // and if we have a transparentPod overlay turn it off.
                    if (stockOverlayDepthMaskShaderTransform != "")
                    {
                        SetDepthMask(true, stockOverlayDepthMaskShaderTransform);
                    }
                    if (transparentPodDepthMaskShaderTransform != "")
                    {
                        SetDepthMask(false, transparentPodDepthMaskShaderTransform);
                    }
                }
                if (HighLogic.LoadedSceneIsFlight)
                    ProcessPortraits(vessel);
            }
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
                    if (part.CrewCapacity > 0)
                        part.SpawnIVA();
                    else
                    {
                        part.CreateInternalModel();
                        part.internalModel.Initialize(part);
                    }
                    part.internalModel.SetVisible(true);
                    setVisible = true;
                    // And then we remember the root part and the active vessel these coordinates refer to.
                    knownRootPart = vessel.rootPart;
                    lastActiveVessel = FlightGlobals.ActiveVessel;
                    ResetShadersBackup();
                }
            }
            catch (Exception ex)
            {
                JSIAdvPodsUtil.Log_Debug("Reset IVA failed: {0}", ex); 
            }
            
        }

        private void ResetShadersBackup()
        {
            // Apply shaders to transforms on startup.
            if (!string.IsNullOrEmpty(transparentTransforms))
            {
                foreach (string transformName in transparentTransforms.Split('|'))
                {
                    try
                    {
                        //Remove all NULL key entries, this happens when the Internal model is re-created.
                        shadersBackup.RemoveAll(item => item.Key == null);
                        //Look for and re-add any transparent transforms on the internal model.
                        if (part.internalModel != null)
                        {
                            Transform itr = part.internalModel.FindModelTransform(transformName.Trim());
                            if (itr != null)
                            {
                                // We both change the shader and backup the original shader so we can undo it later.
                                Shader backupShader = itr.GetComponent<Renderer>().material.shader;
                                itr.GetComponent<Renderer>().material.shader = transparentShader;
                                shadersBackup.Add(new KeyValuePair<Transform, Shader>(itr, backupShader));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, this);
                    }
                }
            }
        }

        public void ProcessPortraits(Vessel vsl)
        {
            // Now we need to make sure that the list of portraits in the GUI conforms to what actually is in the active vessel.
            // This is important because IVA/EVA buttons clicked on kerbals that are not in the active vessel cause problems
            // that I can't readily debug, and it shouldn't happen anyway.
            
            // Only the pods that are not the active vessel should be doing this. So if this part/vessel is not part of the active vessel then:-
            //Search the seats and where there is a kerbalRef try to Unregister them from the PortraitGallery.
            if (part.internalModel != null)
            {
                for (int i = 0; i < part.internalModel.seats.Count; i++)
                {
                    if (part.internalModel.seats[i].kerbalRef != null)
                    {
                        try
                        {
                            if (FlightGlobals.ActiveVessel.id != vessel.id)
                                Portraits.DestroyPortrait(part.internalModel.seats[i].kerbalRef);
                            if (FlightGlobals.ActiveVessel.id == vessel.id)
                                Portraits.RestorePortrait(part, part.internalModel.seats[i].kerbalRef);
                        }
                        catch (Exception)
                        {
                            JSIAdvPodsUtil.Log_Debug("Un/Register Portrait on inactive part failed {0}", part.internalModel.seats[i].kerbalRef.crewMemberName);
                        }
                    }
                }
            }
        }

        public void Update()
        {
            if (Time.timeSinceLevelLoad < 1f) return;

            // In the editor, none of this logic should matter, even though the IVA probably exists already.
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (transparentPodSetting == "ON" || (transparentPodSetting == "AUTO" && mouseOver))
                {
                    // Make the internal model visible...
                    if (part.internalModel != null)
                        part.internalModel.SetVisible(true);
                    // And for a good measure we make sure the shader change has been applied.
                    SetShaders(true);
                    // Now we attach the restored IVA directly into the pod at zero local coordinates and rotate it,
                    // so that it shows up on the main outer view camera in the correct location.
                    VoodooRotate();
                    setVisible = true;
                    //if (JSIZfighterStock != null)
                    //    JSIZfighterStock.Update(stockOverlayTransform);
                    //if (JSIZfightertransparent != null)
                    //    JSIZfightertransparent.Update(transparentPodTransform);
                }

                // If we are in editor mode we need to turn off the internal if the internal is in OFF or AUTO mode and not moused over.
                //Otherwise we make it visible.
                if (transparentPodSetting == "OFF" || (transparentPodSetting == "AUTO" && !mouseOver))
                {

                    // Make the internal model Invisible...
                    if (part.internalModel != null)
                        part.internalModel.SetVisible(false);
                    SetShaders(false);
                    setVisible = false;
                }

                JSIAdvPodsUtil.Log_Debug("Part {0} : Layer {1}", part.name, part.gameObject.layer);

                //Turn the DepthMasks off in the Editor or we get Z-Fighting.
                SetDepthMask();
            }
            if (HighLogic.LoadedSceneIsFlight)
            {
                //Now FlightScene Processing

                //IVA OBstruction process of the transparentPodSetting field
                //If previously IVA was obstructed and now it is not reset the transparentPodSetting back to it's previous value.
                if (previsIVAobstructed && !isIVAobstructed)
                {
                    transparentPodSetting = prevtransparentPodSetting;
                    Events["eventToggleTransparency"].active = true;
                }
                previsIVAobstructed = isIVAobstructed;
                //isIVAobstructed = false;

                // If the root part changed, or the IVA is mysteriously missing, we reset it and take note of where it ended up.
                if (vessel.rootPart != knownRootPart || lastActiveVessel != FlightGlobals.ActiveVessel || part.internalModel == null)
                {
                    ResetIVA();
                }

                // So we do have an internal model, right?
                if (part.internalModel != null)
                {
                    // If transparentPodSetting = OFF or AUTO and not the focused active part we treat the part like a non-transparent part.
                    // and we turn off the shaders (if set) and the internal to the filter list and exit OnUpdate. 
                    if (transparentPodSetting == "OFF" || (transparentPodSetting == "AUTO" && !mouseOver) && !isIVAobstructed)
                    {
                        SetShaders(false);
                        if (!JSIAdvTransparentPods.PartstoFilterfromIVADict.Contains(part))
                            JSIAdvTransparentPods.PartstoFilterfromIVADict.Add(part);
                        setVisible = false;
                        return;
                    }

                    //If we are in flight and the user has the Stock Overlay on and this part is not part of the active vessel we turn off the internal.
                    // also if the user has set the LoadedInactive to False - we don't show TransparentPods that aren't on the active vessel.
                    // We turn it off rather than registering it for the PreCull list because if Stock Overlay is on the JSI camera is not active.
                    if (!vessel.isActiveVessel &&
                            (JSIAdvPodsUtil.StockOverlayCamIsOn || !LoadGlobals.settings.LoadedInactive))
                    {
                        part.internalModel.SetVisible(false);
                        setVisible = false;
                        SetShaders(false);
                        JSIAdvPodsUtil.Log_Debug(
                            "Internal turned off as vessel is Not Active Vessel and stock overlay is on or LoadedInactive is False: ({0}) {1}",
                            part.craftID, vessel.vesselName);
                        return;
                    }

                    if (!vessel.isActiveVessel)
                    {
                        //For some reason (probably performance) Squad do not actively update the position and rotation of InternalModels that are not part of the active vessel.
                        //Calculate the Vessel position and rotation and then apply that to the InternalModel position and rotation with the MagicalVoodooRotation.
                        Vector3 VesselPosition = part.vessel.transform.position +
                                                    part.vessel.transform.rotation * part.orgPos;
                        part.internalModel.transform.position = InternalSpace.WorldToInternal(VesselPosition);
                        Quaternion VesselRotation = part.vessel.transform.rotation * part.orgRot;
                        part.internalModel.transform.rotation = InternalSpace.WorldToInternal(VesselRotation) *
                                                                MagicalVoodooRotation;

                        // If the current part is not part of the active vessel, we calculate the distance from the part to the flight camera.
                        // If this distance is > distanceToCameraThreshold metres we turn off transparency for the part.
                        // Uses Maths calcs intead of built in Unity functions as this is up to 5 times faster.
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
                            if (!JSIAdvTransparentPods.PartstoFilterfromIVADict.Contains(part))
                                JSIAdvTransparentPods.PartstoFilterfromIVADict.Add(part);
                            setVisible = false;
                            return;
                        }
                    }

                    //If inactive vessel IVAs are turned on via the settings then we:
                    //Check for obstructions between this IVA and the Camera that may be on lower layers and turn off the IVA if there is one.
                    //Not a perfect solution..... and bad performance-wise. 
                    if (LoadGlobals.settings.LoadedInactive)
                    {
                        if (JSIAdvTransparentPods.Instance != null &&
                            CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Flight)
                        {
                            if (JSIAdvTransparentPods.Instance.MaincameraTransform != null)
                            {
                                isIVAobstructed = IsIVAObstructed(part.transform, JSIAdvTransparentPods.Instance.MaincameraTransform);
                                if (isIVAobstructed)
                                {
                                    if (!JSIAdvTransparentPods.PartstoFilterfromIVADict.Contains(part))
                                        JSIAdvTransparentPods.PartstoFilterfromIVADict.Add(part);
                                    SetShaders(false);
                                    setVisible = false;
                                    //Set the prevtransparentPodSetting to the current transparentPodSetting and then set transparenPodSetting to "OFF"
                                    if (!previsIVAobstructed)
                                    {
                                        Events["eventToggleTransparency"].active = false;
                                        prevtransparentPodSetting = transparentPodSetting;
                                        transparentPodSetting = "Obstructed";
                                    }
                                    return;
                                }
                            }
                        }
                    }

                    // Make the internal model visible...
                    // And for a good measure we make sure the shader change has been applied.
                    SetShaders(true);
                    if (JSIAdvTransparentPods.PartstoFilterfromIVADict.Contains(part))
                        JSIAdvTransparentPods.PartstoFilterfromIVADict.Remove(part);
                    setVisible = true;
                    part.internalModel.SetVisible(true);
                }
                else
                {
                    JSIAdvPodsUtil.Log("Where is my Internal model for : {0}", part.craftID);
                }
            }
        }

        public override void OnUpdate()
        {
            if (Time.timeSinceLevelLoad < 1f) return;
            
            
        }

        internal bool IsIVAObstructed(Transform Origin, Transform Target)
        {
            float distance = Vector3.Distance(Target.position, Origin.position);
            RaycastHit[] hitInfo;
            Vector3 direction = (Target.position - Origin.position).normalized;
            #if LINEDEBUG
            drawMyLine(Origin.position, Target.position, Color.yellow, 1f);
            #endif
            hitInfo = Physics.RaycastAll(new Ray(Origin.position, direction), distance, 1148433);

            for (int i = 0; i < hitInfo.Length; i++)
            {
                
                JSIAdvPodsUtil.Log_Debug("View Obstructed by {0} , Origin: {1} , Target {2} , Direction {3} , Hit: {4}",
                    hitInfo[i].collider.name, Origin.position, Target.position, direction, hitInfo[i].transform.position);
                if (Origin.position != hitInfo[i].transform.position)
                {
                    return true;
                }
            }

            JSIAdvPodsUtil.Log_Debug("No View obstruction");
            return false;
        }

        #if LINEDEBUG
        internal void drawMyLine(Vector3 start, Vector3 end, Color color, float duration = 0.2f)
        {
            StartCoroutine(JSIAdvPodsUtil.drawLine(start, end, color, duration));
        }
        #endif

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
