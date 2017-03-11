using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;

namespace JokerFioraBuddy
{
    public static class SpellManager
    {
        public static Spell.Skillshot Q { get; private set; }
        public static Spell.Skillshot W { get; private set; }
        public static Spell.Active E { get; private set; }
        public static Spell.Targeted R { get; private set; }
        public static Spell.Targeted Ig { get; private set; }

        public static float QSkillshotRange = 400;
        public static float QCircleRadius = 350;
        static SpellManager()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, (uint)(QSkillshotRange + QCircleRadius), SkillShotType.Linear, 250, 500, 0);
            W = new Spell.Skillshot(SpellSlot.W, 750, SkillShotType.Linear, 500, 3200, 70);
            E = new Spell.Active(SpellSlot.E, 200);
            E.CastDelay = 0;

            R = new Spell.Targeted(SpellSlot.R, 500);
            R.CastDelay = (int).066f;

            if (ObjectManager.Player.GetSpellSlotFromName("summonerdot") != SpellSlot.Unknown)
                Ig = new Spell.Targeted(ObjectManager.Player.GetSpellSlotFromName("summonerdot"), 600);

        }

        public static void CastQ()
        {
            var target = TargetSelector2.GetTarget(400, DamageType.Physical);
            if (target.IsValidTarget() && !target.IsZombie)
            {
                PassiveManager.CastQhelper(target);
            }
            else
            {
                target = TargetSelector2.GetTarget(400 + Player.Instance.GetAutoAttackRange(), DamageType.Physical);
                {
                    if (target.IsValidTarget() && !target.IsZombie)
                    {
                        PassiveManager.CastQhelper(target);
                    }
                    else
                    {
                        target = TargetSelector2.GetTarget(400 + 350, DamageType.Physical);
                        if (target.IsValidTarget() && !target.IsZombie)
                        {
                            PassiveManager.CastQhelper(target);
                        }
                    }
                }
            }
        }

        public static void CastR()
        {
            var target = TargetSelector2.GetTarget(R.Range, DamageType.Physical);

            if (target.IsValidTarget(500) && !target.IsZombie && R.IsReady())
            {
                R.Cast(target);
            }
        }

        public static void CastW()
        {
            var target = TargetSelector2.GetTarget(W.Range, DamageType.Physical);

            if (target.IsValidTarget() && !target.IsZombie && W.IsReady())
            {
                W.Cast(target);
            }
        }

        public static void Initialize()
        { 

        }
    }
}
