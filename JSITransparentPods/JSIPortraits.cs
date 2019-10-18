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
using UnityEngine;

namespace JSIAdvTransparentPods
{
    
    
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class Portraits : MonoBehaviour
    {
        internal static bool HasPortrait(Kerbal crew)
        {
            for (int i = 0; i < KerbalPortraitGallery.Instance.Portraits.Count; ++i)
            {
                if (KerbalPortraitGallery.Instance.Portraits[i].crewMember == crew)
                    return true;
            }
            return false;
        }

        internal static bool InActiveCrew(Kerbal crew)
        {
            for (int i = 0; i < KerbalPortraitGallery.Instance.ActiveCrewItems.Count; ++i)
            {
                if (KerbalPortraitGallery.Instance.ActiveCrewItems[i].kerbal != null && KerbalPortraitGallery.Instance.ActiveCrewItems[i].kerbal.name == crew.name)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Destroy Portraits for a kerbal and Unregisters them from the KerbalPortraitGallery
        /// </summary>
        /// <param name="kerbal">the Kerbal we want to delete portraits for</param>
        internal static void DestroyPortrait(Kerbal kerbal)
        {

            // set the kerbal InPart to null - this should stop their portrait from re-spawning.
            kerbal.InPart = null;
            //Set them visible in portrait to false
            kerbal.SetVisibleInPortrait(false);
            kerbal.state = Kerbal.States.NO_SIGNAL;
            //Loop through the ActiveCrew portrait List
            for (int i = KerbalPortraitGallery.Instance.ActiveCrew.Count - 1; i >= 0; i--)
            {
                //If we find an ActiveCrew entry where the crewMemberName is equal to our kerbal's
                if (KerbalPortraitGallery.Instance.ActiveCrew[i].crewMemberName == kerbal.crewMemberName)
                {
                    //we Remove them from the list.
                    KerbalPortraitGallery.Instance.ActiveCrew.RemoveAt(i);
                }
            }
            //Portraits List clean-up.
            KerbalPortraitGallery.Instance.DespawnInactivePortraits(); //Despawn any portraits where CrewMember == null
            KerbalPortraitGallery.Instance.DespawnPortrait(kerbal); //Despawn our Kerbal's portrait
            KerbalPortraitGallery.Instance.UIControlsUpdate(); //Update UI controls
        }

        /// <summary>
        /// Restore the Portrait for a kerbal and register them to the KerbalPortraitGallery
        /// </summary>
        /// <param name="kerbal">the kerbal we want restored</param>
        /// <param name="part">the part the kerbal is in</param>
        internal static void RestorePortrait(Part part, Kerbal kerbal)
        {
            //We don't process DEAD, Unowned kerbals - Compatibility with DeepFreeze Mod.
            if (kerbal.rosterStatus != ProtoCrewMember.RosterStatus.Dead &&
                kerbal.protoCrewMember.type != ProtoCrewMember.KerbalType.Unowned)
            {
                //Set the Kerbals InPart back to their part.
                kerbal.InPart = part;
                //Set their portrait state to ALIVE and set their portrait back to visible.
                kerbal.state = Kerbal.States.ALIVE;
                kerbal.SetVisibleInPortrait(true);
                //If they aren't in ActiveCrew and don't have a Portrait them via the kerbal.Start method.
                if (!InActiveCrew(kerbal) && !HasPortrait(kerbal))
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
