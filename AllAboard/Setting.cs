using System.Collections.Generic;
using AllAboard.System.Utility;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;

namespace AllAboard
{
    [FileLocation(nameof(Mod))]
    public class Setting : ModSetting
    {
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
                Mod.log.InfoFormat(
                    "Now max dwell delay: Bus: {0} minutes, Train : {1} minutes.",
                    PublicTransportBoardingHelper.BusMaxAllowedMinutesLate.Data,
                    PublicTransportBoardingHelper.TrainMaxAllowedMinutesLate.Data);
            }
        }

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        public override void SetDefaults()
        {
            TrainMaxDwellDelaySlider = 8;
            BusMaxDwellDelaySlider = 8;
        }
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting _setting;

        public LocaleEN(Setting setting)
        {
            _setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors,
            Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { _setting.GetSettingsLocaleID(), "All Aboard!" },
                {
                    _setting.GetOptionLabelLocaleID(nameof(Setting.TrainMaxDwellDelaySlider)),
                    "Train Maximum Dwell Delay (in-game minutes)"
                },
                {
                    _setting.GetOptionDescLocaleID(nameof(Setting.TrainMaxDwellDelaySlider)),
                    "Maximum amount of (in-game) time to allow a Train (Subway, Tram) to 'dwell' beyond its scheduled departure frame. "
                },

                {
                    _setting.GetOptionLabelLocaleID(nameof(Setting.BusMaxDwellDelaySlider)),
                    "Bus Maximum Dwell Delay (in-game minutes)"
                },
                {
                    _setting.GetOptionDescLocaleID(nameof(Setting.BusMaxDwellDelaySlider)),
                    "Maximum amount of (in-game) time to allow a Bus to 'dwell' beyond its scheduled departure frame. "
                },
                { _setting.GetOptionLabelLocaleID(nameof(Setting.ApplyButton)), "Apply" },
                { _setting.GetOptionDescLocaleID(nameof(Setting.ApplyButton)), "Apply Settings" }
            };
        }

        public void Unload()
        {
        }
    }
}