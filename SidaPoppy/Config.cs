#region
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

#endregion

namespace Sida
{
    public static class Config
    {
        private static CheckBox _useQCombo;
        private static CheckBox _useWCombo;
        private static CheckBox _useECombo;
        private static KeyBind _useRCombo;
        public static bool UseQCombo => _useQCombo.CurrentValue;
        public static bool UseWCombo => _useWCombo.CurrentValue;
        public static bool UseECombo => _useECombo.CurrentValue;
        public static bool UseRCombo => _useRCombo.CurrentValue;
        private static CheckBox _useQHarass;
        public static bool UseQHarass => _useQHarass.CurrentValue;
        private static CheckBox _useQJungleClear;
        private static CheckBox _useEJungleClear;
        public static bool UseQJungleClear => _useQJungleClear.CurrentValue;
        public static bool UseEJungleClear => _useEJungleClear.CurrentValue;
        private static CheckBox _useQLaneClear;
        public static bool UseQLaneClear => _useQLaneClear.CurrentValue;
        private static CheckBox _useWMisc;
        public static bool UseWMisc => _useWMisc.CurrentValue;

        private static Menu _main, _combo, _harass, _laneClear, _lastHit,_jungleClear, _misc;
       
        public static void MyMenu()
        {
            // Initialize the menu
            _main = MainMenu.AddMenu("Sida's Poppy", "Poppy");
            _combo = _main.AddSubMenu("Combo");
            _harass = _main.AddSubMenu("Harass");
            _laneClear = _main.AddSubMenu("LaneClear");
            _jungleClear = _main.AddSubMenu("JungleClear");
            _misc = _main.AddSubMenu("Misc");

            _combo.AddGroupLabel("Combo");
            _useQCombo = _combo.Add("comboUseQ", new CheckBox("Use Q"));
            _useWCombo = _combo.Add("comboUseW", new CheckBox("Use W"));
            _useECombo = _combo.Add("comboUseE", new CheckBox("Use E"));

            _harass.AddGroupLabel("Harass");
            _useQHarass = _harass.Add("harassUseQ", new CheckBox("Use Q"));

            _jungleClear.AddLabel("JungleClear");
            _useQJungleClear = _jungleClear.Add("jungleClearQ", new CheckBox("Use Q"));
            _useEJungleClear = _jungleClear.Add("jungleClearE", new CheckBox("UseE"));
            
            _useWMisc = _misc.Add("autoW", new CheckBox("Auto W"));
        }
        }
    }
