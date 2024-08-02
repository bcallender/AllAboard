using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;

namespace AllAboard
{
    [FileLocation(nameof(AllAboard))]
    [SettingsUIGroupOrder(groupName, advancedGroupName)]
    [SettingsUIShowGroupName(groupName, advancedGroupName)]
    public class Setting : ModSetting
    {
        public enum SomeEnum
        {
            Value1,
            Value2,
            Value3
        }

        public const string sectionName = "dewllDelayConfiguration";
        public const string sectionDisplayName = "Dwell Delay Configuration (Applies on Game Restart)";

        public const string groupDisplayName = "Maximum Dwell Delay (s) [Beyond Scheduled Departure Time]";
        public const string groupName = "dwellDelayGroup";

        public const string advancedGroupName = "advanced";
        public const string advancedGroupDisplayName = "Advanced Settings";


        public const string carMaxDwellDelay = "carmax";
        public const string trainMaxDwellDelay = "trainmax";

        public const string carMaxDwellDelayDisplayName = "Car Transport Types (Bus)";
        public const string trainMaxDwellDelayDisplayName = "Train Transport Types (Train, Subway, Tram)";

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }


        [SettingsUISlider(min = 0, max = 300, step = 5, scalarMultiplier = 1, unit = "Seconds")]
        [SettingsUISection(sectionName, groupName)]
        public uint CarMaxDwellDelaySlider { get; set; }

        [SettingsUISlider(min = 0, max = 300, step = 5, scalarMultiplier = 1, unit = "Seconds")]
        [SettingsUISection(sectionName, groupName)]
        public uint TrainMaxDwellDelaySlider { get; set; }

        [SettingsUISlider(min = 10, max = 90, step = 1, scalarMultiplier = 1, unit = "Frames per Second")]
        [SettingsUISection(sectionName, advancedGroupName)]
        public uint SimulationFramesPerSecond { get; set; }


        public override void SetDefaults()
        {
            SimulationFramesPerSecond = 30;
            CarMaxDwellDelaySlider = 30;
            TrainMaxDwellDelaySlider = 30;
        }
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors,
            Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "All Aboard! (0.0.2 ALPHA)" },
                { m_Setting.GetOptionTabLocaleID(Setting.sectionName), Setting.sectionDisplayName },

                { m_Setting.GetOptionGroupLocaleID(Setting.groupName), Setting.groupDisplayName },
                { m_Setting.GetOptionGroupLocaleID(Setting.advancedGroupName), Setting.advancedGroupDisplayName },
                { m_Setting.GetOptionGroupLocaleID(Setting.carMaxDwellDelay), Setting.carMaxDwellDelayDisplayName },
                { m_Setting.GetOptionGroupLocaleID(Setting.trainMaxDwellDelay), Setting.trainMaxDwellDelayDisplayName }
            };
        }

        public void Unload()
        {
        }
    }
}