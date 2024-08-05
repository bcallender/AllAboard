using System.Collections.Generic;
using AllAboard.System.Utility;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;

namespace AllAboard
{
    [FileLocation(nameof(AllAboard))]
    public class Setting : ModSetting
    {
        [SettingsUISlider(min = 0, max = 30, step = 1, unit = "Minutes")] 
        public int MaxDwellDelaySlider { get; set; }

        [SettingsUIButton]
        public bool ApplyButton
        {
            set
            {
                PublicTransportBoardingHelper.MaxAllowedMinutesLate.Data = (uint) MaxDwellDelaySlider;
                AllAboard.log.InfoFormat("Now max dwell delay: {0} minutes",
                    PublicTransportBoardingHelper.MaxAllowedMinutesLate.Data);
            }
        }
        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        public override void SetDefaults()
        {
            MaxDwellDelaySlider = 5;
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
                    _setting.GetOptionLabelLocaleID(nameof(Setting.MaxDwellDelaySlider)),
                    "Maximum Dwell Delay (in-game minutes)"
                },
                {
                    _setting.GetOptionDescLocaleID(nameof(Setting.MaxDwellDelaySlider)),
                    "Maximum amount of (in-game) time to allow a transport vehicle to 'dwell' beyond their scheduled departure frame. "
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