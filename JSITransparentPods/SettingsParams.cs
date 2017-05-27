using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JSIAdvTransparentPods
{
    public class JSIATP_SettingsParms : GameParameters.CustomParameterNode

    {
        public override string Title
        {
            get { return "JSI Advanced Transparent Pods Options"; }
        }

        public override GameParameters.GameMode GameMode
        {
            get { return GameParameters.GameMode.ANY; }
        }

        public override bool HasPresets
        {
            get { return true; }
        }

        public override string Section
        {
            get { return "JSI Adv Trans Pods"; }
        }

        public override string DisplaySection
        {
            get { return "JSI Adv Trans Pods"; }
        }

        public override int SectionOrder
        {
            get { return 1; }
        }

        [GameParameters.CustomParameterUI("Extra Debug Logging",
            toolTip = "Turn this On to capture lots of extra information into the KSP log for reporting a problem.")] public bool DebugLogging = false;

        [GameParameters.CustomParameterUI("Loaded vessels have Transparent Pods",
            toolTip =
                "Turn this On to have transparent Pods on all Loaded vessels (not just the active vessel), if Off only the Active Vessel will be transparent."
            )] public bool LoadedInactive = true;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {

        }
    }
}
