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
using UnityEngine;

namespace JSIAdvTransparentPods
{
    public class JSIZFighter //: MonoBehaviour
    {

        // Use this for initialization
        private static Vector3 lastLocalPosition;
        private static Vector3 lastCamPos;
        private static Vector3 lastPos;

        internal void Start(Transform transform)
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                lastLocalPosition = transform.localPosition;
                lastPos = transform.position;
                lastCamPos = JSIAdvPodsUtil.GetCameraByName("Main Camera").transform.position - Vector3.up; // just to force update on first frame 
            }
        }

        internal void Update(Transform transform)
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                Vector3 camPos = JSIAdvPodsUtil.GetCameraByName("Main Camera").transform.position;
                if (camPos != lastCamPos || transform.position != lastPos)
                {
                    lastCamPos = camPos;
                    transform.localPosition = lastLocalPosition + (camPos - transform.position)*0.001f;
                    lastPos = transform.position;
                }
            }
        }
    }
}
