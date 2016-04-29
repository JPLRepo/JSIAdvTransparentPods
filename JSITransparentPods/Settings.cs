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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace JSIAdvTransparentPods
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class LoadGlobals : MonoBehaviour
    {
        public static LoadGlobals Instance;
        public static Settings settings;
        private string globalConfigFilename;
        private ConfigNode globalNode = new ConfigNode();

        //Awake Event - when the DLL is loaded
        public void Awake()
        {
            if (Instance != null)
                return;
            Instance = this;
            DontDestroyOnLoad(this);
            settings = new Settings();
            globalConfigFilename = Path.Combine(_AssemblyFolder, "Config.cfg").Replace("\\", "/");
            JSIAdvPodsUtil.Log("globalConfigFilename = " + globalConfigFilename);
            if (!File.Exists(globalConfigFilename))
            {
                settings.Save(globalNode);
                globalNode.Save(globalConfigFilename);
            }
            globalNode = ConfigNode.Load(globalConfigFilename);
            settings.Load(globalNode);
            JSIAdvPodsUtil.debugLoggingEnabled = settings.DebugLogging;
            JSIAdvPodsUtil.Log("JSIAdvTransparentPods LoadGlobals Awake Complete");
        }

        public void Start()
        {
            //GameEvents.onGameSceneSwitchRequested.Add(onGameSceneSwitchRequested);
        }

        public void OnDestroy()
        {
            //GameEvents.onGameSceneSwitchRequested.Remove(onGameSceneSwitchRequested);
        }

        #region Assembly/Class Information

        /// <summary>
        /// Name of the Assembly that is running this MonoBehaviour
        /// </summary>
        internal static String _AssemblyName
        { get { return Assembly.GetExecutingAssembly().GetName().Name; } }

        /// <summary>
        /// Full Path of the executing Assembly
        /// </summary>
        internal static String _AssemblyLocation
        { get { return Assembly.GetExecutingAssembly().Location; } }

        /// <summary>
        /// Folder containing the executing Assembly
        /// </summary>
        internal static String _AssemblyFolder
        { get { return Path.GetDirectoryName(_AssemblyLocation); } }

        #endregion Assembly/Class Information
    }

    public class Settings
    {
        // this class stores the DeepFreeze Settings from the config file.
        private const string configNodeName = "JSIAdvTransparentPodsSettings";

        internal bool DebugLogging;
        public bool LoadedInactive;



        internal Settings()
        {
            DebugLogging = false;
        }

        //Settings Functions Follow

        internal void Load(ConfigNode node)
        {
            if (node.HasNode(configNodeName))
            {
                ConfigNode settingsNode = node.GetNode(configNodeName);
                DebugLogging = GetNodeValue(settingsNode, "DebugLogging", DebugLogging);
                LoadedInactive = GetNodeValue(settingsNode, "LoadedInactive", LoadedInactive);

            }
        }

        internal void Save(ConfigNode node)
        {
            ConfigNode settingsNode;
            if (node.HasNode(configNodeName))
            {
                settingsNode = node.GetNode(configNodeName);
                settingsNode.ClearData();
            }
            else
            {
                settingsNode = node.AddNode(configNodeName);
            }
            
            settingsNode.AddValue("DebugLogging", DebugLogging);
            settingsNode.AddValue("LoadedInactive", LoadedInactive);
        }

        internal bool GetNodeValue(ConfigNode confignode, string fieldname, bool defaultValue)
        {
            bool newValue;
            if (confignode.HasValue(fieldname) && Boolean.TryParse(confignode.GetValue(fieldname), out newValue))
            {
                return newValue;
            }
            return defaultValue;
        }
    }
}
