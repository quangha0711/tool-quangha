using EloBuddy;
using EloBuddy.SDK;
using System.Linq;
using S = Sida.SpellManager;
using P = Sida.Modes.Prediction;
using Settings = Sida.Config;

namespace Sida.Modes
{
    public static class JungleClear
    {

        public static void Execute()
        {
            var target = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position, S.E.Range).FirstOrDefault(it => it.Health > Player.Instance.GetAutoAttackDamage(it));

            if (target == null || !target.IsValidTarget(S.E.Range)) { return; }
            var finalPosition = target.BoundingRadius + target.Position.Extend(ObjectManager.Player.Position, -360);
            if (S.Q.IsReady() && Settings.UseQJungleClear)
            {
                S.Q.Cast(target);
            }
            if (S.E.IsReady() && Settings.UseEJungleClear && finalPosition.IsWall())
            {
                S.E.Cast(target);
            }
        }
        public static void CastSpell(Spell.Skillshot qwer, Obj_AI_Base target)
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
            if (poutput2.Hitchance >= HitChance.Low)
                qwer.Cast(poutput2.CastPosition);
        }
    }
}