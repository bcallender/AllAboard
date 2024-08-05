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
        public enum SomeEnum
        {
            Value1,
            Value2,
            Value3
        }


        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }


        [SettingsUISlider(min = 0, max = 30, step = 1, scalarMultiplier = 1, unit = "Minutes")]
        public uint MaxDwellDelaySlider { get; set; }

        [SettingsUIButton]
        public bool ApplyButton
        {
            set
            {
                PassengerBoardingChecks.MaxAllowedMinutesLate.Data = MaxDwellDelaySlider;
                Mod.log.InfoFormat("Now max boarding time: {0}", PassengerBoardingChecks.MaxAllowedMinutesLate.Data);
            }
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
                    _setting.GetOptionTabLocaleID(nameof(Setting.MaxDwellDelaySlider)),
                    "Maximum Dwell Delay (in-game minutes)"
                },

                { _setting.GetOptionGroupLocaleID(nameof(Setting.ApplyButton)), "Apply" }
            };
        }

        public void Unload()
        {
        }
    }
}