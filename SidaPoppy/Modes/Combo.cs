using EloBuddy;
using EloBuddy.SDK;
using Settings = Sida.Config;
using S = Sida.SpellManager;
using P = Sida.Modes.Prediction;
using SharpDX;
namespace Sida.Modes
{
    public static class Combo 
    {

        public static void Execute()
        {

            var target = TargetSelector.GetTarget(S.E.Range, DamageType.Physical);
            if (target == null || !target.IsValidTarget(S.E.Range)){ return; }
            if (S.E.IsReady()  && Settings.UseECombo && !S.R.IsCharging)
            {
                var finalPosition = target.BoundingRadius + target.Position.Extend(ObjectManager.Player.Position, -360);
                if (finalPosition.IsWall())
                {
                    S.E.Cast(target);
                }
                if (S.Q.IsReady() && Settings.UseQCombo)
                {
                    S.Q.Cast(target);
                }
            }
            if (S.Q.IsReady() && Settings.UseQCombo && !S.R.IsCharging)
            {
                target = TargetSelector.GetTarget(S.Q.Range, DamageType.Physical);
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
                if(Standard.HitChance >= EloBuddy.SDK.Enumerations.HitChance.Medium)
                {
                qwer.Cast(S.Q.GetPrediction(target).CastPosition);
                }
        }
    }
}
