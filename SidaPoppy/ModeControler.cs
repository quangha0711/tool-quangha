using System;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using Settings = Sida.Config;
using S = Sida.SpellManager;
namespace Sida
{
    public static class ModeControler
    {
        public static void ModeManager()
        {
            Game.OnTick += OnTick;
            if (Settings.UseWMisc)
            {
                Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            }
        }

        private static void OnTick(EventArgs args)
        {
            Modes.AutoActive.Execute();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Modes.Combo.Execute();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                Modes.JungleClear.Execute();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                Modes.LaneClear.Execute();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Modes.Harrass.Execute();
            }
        }
        private static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs t)
        {
            if (sender.IsMe || sender.IsDead || sender.IsAlly || sender == null) { return; }
            if (t.End.Distance(Player.Instance) <= 400)
            {
                S.W.Cast();
            }
        }
    }
}
