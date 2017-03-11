using EloBuddy;
using EloBuddy.SDK;
using S = Sida.SpellManager;
using P = Sida.Modes.Prediction;
using Settings = Sida.Config;

namespace Sida.Modes
{
    public static class Harrass
    {

        public static void Execute()
        {
            var target = TargetSelector.GetTarget(S.Q.Range, DamageType.Physical);
            if (target == null || !target.IsValidTarget(S.Q.Range)) { return; }
            if (Settings.UseQHarass && S.Q.IsReady())
            {
                S.Q.Cast(target);
            }
        }
        private static void CastSpell(Spell.Skillshot qwer, Obj_AI_Base target)
        {
            var predInput2 = new PredictionInput
            {
                Speed = qwer.Speed,
                Delay = qwer.CastDelay,
                Range = qwer.Range,
                From = Player.Instance.ServerPosition,
                Radius = qwer.Width,
                Unit = target,
                Type = SkillshotType.SkillshotLine
            };
            var poutput2 = P.GetPrediction(predInput2);
            var Standard = qwer.GetPrediction(target);
            if (poutput2.Hitchance >= HitChance.High)
                qwer.Cast(poutput2.CastPosition);
            else
                if (Standard.HitChance >= EloBuddy.SDK.Enumerations.HitChance.Medium)
                qwer.Cast(P.GetPrediction(target, 150).CastPosition);
        }
    }
}
