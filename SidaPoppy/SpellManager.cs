using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sida
{
    public static class SpellManager
    {
        public static Spell.Skillshot Q { get; private set; }
        public static Spell.Active W { get; private set; }
        public static Spell.Targeted E { get; private set; }
        public static Spell.Chargeable R { get; private set; }

        static SpellManager()
        {

            Q = new Spell.Skillshot(SpellSlot.Q, 430, SkillShotType.Linear, 500, 1700, 100);
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Targeted(SpellSlot.E, 425);
            R = new Spell.Chargeable(SpellSlot.R, 470, 1230, 2589, 300, 1600, 100);
        }
    }
}
