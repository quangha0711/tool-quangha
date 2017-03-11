using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK.Rendering;
using System.Drawing;
using SharpDX;

using Settings = JokerFioraBuddy.Config.Drawings;
namespace JokerFioraBuddy
{
    public static class DamageIndicator
    {
        public delegate float DamageToUnitDelegate(AIHeroClient hero);

        public static int XOffset { get; } = 5;
        private const int YOffset = 20;
        public static int Width { get; } = 103;
        public static int Height { get; } = 8;

        public static DamageToUnitDelegate DamageToUnit { get; set; }
        public static Text TextKillable { get; private set; }
        public static Text TextMinion { get; private set; }

        static DamageIndicator()
        {
            TextKillable = new Text("", new Font(FontFamily.GenericSansSerif, 11, FontStyle.Bold)) { Color = System.Drawing.Color.Red };
            TextMinion = new Text("", new Font(FontFamily.GenericSansSerif, 11, FontStyle.Bold)) { Color = System.Drawing.Color.LimeGreen };

            Drawing.OnDraw += Drawing_OnDraw;
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (Settings.ShowKillable)
            {
                foreach (var unit in ObjectManager.Get<AIHeroClient>().Where(h => h.IsValid && h.IsHPBarRendered && h.IsEnemy))
                {

                    var barPos = unit.HPBarPosition;
                    var damage = DamageToUnit(unit);

                    if (damage > unit.Health)
                    {
                        TextKillable.Position = new Vector2((int)barPos.X - 15, (int)barPos.Y + YOffset + 20);
                        TextKillable.TextValue = "Killable with Combo!";
                        TextKillable.Color = System.Drawing.Color.LimeGreen;
                    }
                    else
                    {
                        TextKillable.Position = new Vector2((int)barPos.X, (int)barPos.Y + YOffset + 20);
                        TextKillable.TextValue = "Not Killable!";
                        TextKillable.Color = System.Drawing.Color.Red;
                    }

                    TextKillable.Draw();
                }
            }
        }

        public static void Initialize()
        {

        }
    }
}
