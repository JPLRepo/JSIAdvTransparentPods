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

namespace JSIAdvTransparentPods
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using KSP.UI.Screens.Flight;
    using UnityEngine;
    
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class Portraits : MonoBehaviour
    {
        /// <summary>
        /// Destroy Portraits for a kerbal and Unregisters them from the KerbalPortraitGallery
        /// </summary>
        /// <param name="kerbal">the Kerbal we want to delete portraits for</param>
        internal static void DestroyPortrait(Kerbal kerbal)
        {
            //First get a list of ActiveCrew where crewMemberName = our kerbal's crewMemberName
            //Should be able to do a straight kerbal match here, but for some reason (other stock code?) there are sometimes ghost/phantom
            // Kerbal entries so they don't match???
            List<Kerbal> matchingKerbalnames =
                KerbalPortraitGallery.Instance.ActiveCrew.FindAll(a => a.crewMemberName == kerbal.crewMemberName);
            // For all the matching ActiveCrew we found cycle through them and de-activate their portrait and UnRegister Them.
            for (int i = 0; i < matchingKerbalnames.Count; i++)
            {
                // set the kerbal InPart to null - this should stop their portrait from re-spawning.
                kerbal.InPart = null;
                //Set them visible in portrait to false
                kerbal.SetVisibleInPortrait(false);
                kerbal.state = Kerbal.States.NO_SIGNAL;
                //Unregister our kerbal.
                KerbalPortraitGallery.Instance.UnregisterActiveCrew(kerbal);
            }

            //Now for some other reason sometimes we have orphan Portraits, so we search those for any with matching crewMemberName's
            // and try to Unregister them as well.
            List<KerbalPortrait> matchingPortraits =
                KerbalPortraitGallery.Instance.Portraits.FindAll(a => a.crewMemberName == kerbal.crewMemberName);
            for (int i = 0; i < matchingPortraits.Count; i++)
            {
                if (matchingPortraits[i].crewMember != null)
                {
                    //Unregister our kerbal.
                    KerbalPortraitGallery.Instance.UnregisterActiveCrew(matchingPortraits[i].crewMember);
                }
            }
        }

        /// <summary>
        /// Restore the Portrait for a kerbal and register them to the KerbalPortraitGallery
        /// </summary>
        /// <param name="kerbal">the kerbal we want restored</param>
        /// <param name="part">the part the kerbal is in</param>
        internal static void RestorePortrait(Part part, Kerbal kerbal)
        {
            
            //See if there isn't already a portrait for our kerbal. If there is no match register our kerbal.
            Kerbal kerbalMatch = KerbalPortraitGallery.Instance.ActiveCrew.FirstOrDefault(a => a == kerbal);
            KerbalPortrait portraitMatch = KerbalPortraitGallery.Instance.Portraits.FirstOrDefault(a => a == a.crewMember);
            
            //We don't process DEAD, Unowned kerbals - Compatibility with DeepFreeze Mod.
            if (kerbal.rosterStatus != ProtoCrewMember.RosterStatus.Dead &&
                kerbal.protoCrewMember.type != ProtoCrewMember.KerbalType.Unowned)
            {
                //Set the Kerbals InPart back to their original InPart that the PortraitTracker class should have tracked and kept for us.
                kerbal.InPart = part;
                //Restore the kerbal's portrait, set their portrait state to ALIVE, set them visibile in portraits, call Kerbal.Start which
                //seems to initialise and do what we want.
                kerbal.state = Kerbal.States.ALIVE;
                kerbal.SetVisibleInPortrait(true);
                if (kerbalMatch == null && portraitMatch == null)
                {
                    kerbal.staticOverlayDuration = 1f;
                    kerbal.randomizeOnStartup = false;
                    kerbal.Start();
                }
                kerbal.state = Kerbal.States.ALIVE;
            }
        }
    }
}
