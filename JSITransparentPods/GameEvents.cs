using System;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace JSIAdvTransparentPods
{
    class JSIATPGameEvents
    {
        /// <summary>
        /// Fires when JSI ATP Resets the Internal Model on a part.
        /// </summary>
        public static EventData<Part> onATPResetIVA;
        /// <summary>
        /// Fires when the player changes a JSI ATP pod setting ON/OFF/AUTO
        /// </summary>
        public static EventData<Part, string> onATPPodSettingChanged;
    }
}