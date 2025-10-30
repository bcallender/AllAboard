using System.Collections.Generic;
using AllAboard.System.Utility;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;

namespace AllAboard
{
    [FileLocation(nameof(AllAboard))]
    public class AllAboardSettings : ModSetting
    {   
        public AllAboardSettings(IMod mod) : base(mod)
        {
            SetDefaults();
        }
        public string ModVersion => "0.1.8";

        [SettingsUISlider(min = 0, max = 30, step = 1, unit = "Minutes")]
        public int TrainMaxDwellDelaySlider { get; set; }

        [SettingsUISlider(min = 0, max = 30, step = 1, unit = "Minutes")]
        public int BusMaxDwellDelaySlider { get; set; }

        [SettingsUIButton]
        public bool ApplyButton
        {
            set
            {
                PublicTransportBoardingHelper.TrainMaxAllowedMinutesLate.Data = (uint)TrainMaxDwellDelaySlider;
                PublicTransportBoardingHelper.BusMaxAllowedMinutesLate.Data = (uint)BusMaxDwellDelaySlider;
                AllAboard.log.InfoFormat(
                    "Now max dwell delay: Bus: {0} minutes, Train : {1} minutes.",
                    PublicTransportBoardingHelper.BusMaxAllowedMinutesLate.Data,
                    PublicTransportBoardingHelper.TrainMaxAllowedMinutesLate.Data);
            }
        }

        public override void SetDefaults()
        {
            TrainMaxDwellDelaySlider = 8;
            BusMaxDwellDelaySlider = 8;
        }
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly AllAboardSettings m_AllAboardSettings;

        public LocaleEN(AllAboardSettings allAboardSettings)
        {
            m_AllAboardSettings = allAboardSettings;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors,
            Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_AllAboardSettings.GetSettingsLocaleID(), "All Aboard!" },
                {
                    m_AllAboardSettings.GetOptionLabelLocaleID(nameof(AllAboardSettings.ModVersion)),
                    "Mod Version"
                },
                {
                    m_AllAboardSettings.GetOptionDescLocaleID(nameof(AllAboardSettings.ModVersion)),
                    "This is the version of the mod. "
                },
                {
                    m_AllAboardSettings.GetOptionLabelLocaleID(nameof(AllAboardSettings.TrainMaxDwellDelaySlider)),
                    "Train Maximum Dwell Delay (in-game minutes)"
                },
                {
                    m_AllAboardSettings.GetOptionDescLocaleID(nameof(AllAboardSettings.TrainMaxDwellDelaySlider)),
                    "Maximum amount of (in-game) time to allow a Train (Subway, Tram) to 'dwell' beyond its scheduled departure frame. "
                },

                {
                    m_AllAboardSettings.GetOptionLabelLocaleID(nameof(AllAboardSettings.BusMaxDwellDelaySlider)),
                    "Bus Maximum Dwell Delay (in-game minutes)"
                },
                {
                    m_AllAboardSettings.GetOptionDescLocaleID(nameof(AllAboardSettings.BusMaxDwellDelaySlider)),
                    "Maximum amount of (in-game) time to allow a Bus to 'dwell' beyond its scheduled departure frame. "
                },
                { m_AllAboardSettings.GetOptionLabelLocaleID(nameof(AllAboardSettings.ApplyButton)), "Apply" },
                { m_AllAboardSettings.GetOptionDescLocaleID(nameof(AllAboardSettings.ApplyButton)), "Apply Settings" }
            };
        }

        public void Unload()
        {
        }
    }
}