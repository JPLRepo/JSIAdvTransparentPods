using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace JSIAdvTransparentPods
{
    public class JSIATP_SettingsParms : GameParameters.CustomParameterNode

    {
        public override string Title { get { return Localizer.Format("#autoLOC_JISATP_00006"); } } //#autoLOC_JISATP_00006 = JSI Advanced Transparent Pods Options

        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; }}

        public override bool HasPresets { get { return true; }}

        public override string Section { get { return "JSI Adv Trans Pods"; }}

        public override string DisplaySection { get { return Localizer.Format("#autoLOC_JISATP_00007"); } } //#autoLOC_JISATP_00007 = JSI Adv Trans Pods

        public override int SectionOrder { get { return 1; }}

        [GameParameters.CustomParameterUI("#autoLOC_JISATP_00008", //#autoLOC_JISATP_00008 = Extra Debug Logging
            toolTip = "#autoLOC_JISATP_00009")] public bool DebugLogging = false; //#autoLOC_JISATP_00009 = Turn this On to capture lots of extra information into the KSP log for reporting a problem.

        [GameParameters.CustomParameterUI("#autoLOC_JISATP_00010", //#autoLOC_JISATP_00010 = Loaded vessels have Transparent Pods
            toolTip =
                "#autoLOC_JISATP_00011" //#autoLOC_JISATP_00011 = Turn this On to have transparent Pods on all Loaded vessels (not just the active vessel), if Off only the Active Vessel will be transparent.
            )] public bool LoadedInactive = true;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {

        }
    }
}
