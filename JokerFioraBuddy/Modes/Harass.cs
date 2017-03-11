using EloBuddy;
using EloBuddy.SDK;
using Settings = JokerFioraBuddy.Config.Modes.Harass;

namespace JokerFioraBuddy.Modes
{
    public sealed class Harass : ModeBase
    {
        public override bool ShouldBeExecuted()
        {
            return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass);
        }

        public override void Execute()
        {
            var target = TargetSelector2.GetTarget(Q.Range, DamageType.Physical);

            if (target != null && target.IsValidTarget(Q.Range))
            {

                PassiveManager.CastAutoAttack(target);

                if (Settings.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range) && !target.IsZombie && Player.Instance.ManaPercent > Settings.Mana)
                    SpellManager.CastQ();

                if (Settings.UseTiamatHydra)
                    ItemManager.UseHydra(target);

                if (Settings.UseE && E.IsReady() && target.IsValidTarget(E.Range) && !target.IsZombie && Player.Instance.ManaPercent > Settings.Mana)
                    E.Cast();

                if (Settings.UseR && R.IsReady() && target.IsValidTarget(R.Range) && !target.IsZombie && Player.Instance.ManaPercent > Settings.Mana)
                    SpellManager.CastR();
            }
        }
    }
}
