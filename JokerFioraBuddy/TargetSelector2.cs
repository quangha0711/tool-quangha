using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;
using Settings = JokerFioraBuddy.Config.Drawings;

namespace JokerFioraBuddy
{
    public static class TargetSelector2
    {
        private static AIHeroClient _target;
        private static int _lastClick;

        static TargetSelector2()
        {
            Game.OnWndProc += OnWndProc;
        }  

        public static AIHeroClient GetTarget(float range, DamageType type, Vector2 secondaryPos = new Vector2())
        {
            if (TargetSelector.SelectedTarget != null)
                return TargetSelector.SelectedTarget;

            if (_target == null || _target.IsDead || _target.Health <= 0 || !_target.IsValidTarget())
                _target = null;

            if (secondaryPos.IsValid() && _target.Distance(secondaryPos) < range || _target.IsValidTarget(range))
                return _target;

            return TargetSelector.GetTarget(range, type);
        }

        static void OnWndProc(WndEventArgs args)
        {
            if (args.Msg != 0x202) return;
            if (_lastClick + 500 <= Environment.TickCount)
            {
                _target =
                    ObjectManager.Get<AIHeroClient>()
                        .OrderBy(a => a.Distance(ObjectManager.Player))
                        .FirstOrDefault(a => a.IsEnemy && a.Distance(Game.CursorPos) < 200);
                if (_target != null)
                {
                    _lastClick = Environment.TickCount;
                }
            }
        }

        public static void Initialize()
        {

        }
    }
}
