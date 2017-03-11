using EloBuddy;
using EloBuddy.SDK;

namespace JokerFioraBuddy
{
    public static class ItemManager
    {

        public static Item RavenousHydra { get; private set; }
        public static Item TitanicHydra { get; private set; }
        public static Item Botrk { get; private set; }
        public static Item Cutl { get; private set; }
        public static Item Tiamat { get; private set; }
        public static Item Yomu { get; private set; }
        public static Item Sheen { get; private set; }
        public static Item TriForce { get; private set; }

        static ItemManager()
        {
            RavenousHydra = new Item((int)ItemId.Ravenous_Hydra_Melee_Only, 400);
            TitanicHydra = new Item((int)ItemId.Titanic_Hydra);
            Botrk = new Item((int)ItemId.Blade_of_the_Ruined_King, 450);
            Cutl = new Item((int)ItemId.Bilgewater_Cutlass, 450);
            Tiamat = new Item((int)ItemId.Tiamat_Melee_Only, 400);
            Yomu = new Item((int)ItemId.Youmuus_Ghostblade);
            Sheen = new Item((int)ItemId.Sheen);
            TriForce = new Item((int)ItemId.Trinity_Force);
        }

        public static void UseHydra(Obj_AI_Base target)
        {
            if (Tiamat.IsOwned() || RavenousHydra.IsOwned() || TitanicHydra.IsOwned())
            {
                if ((Tiamat.IsReady() && Player.Instance.Distance(target) <= RavenousHydra.Range 
                    || RavenousHydra.IsReady()) && Player.Instance.Distance(target) <= RavenousHydra.Range 
                    || TitanicHydra.IsReady())
                {
                    Tiamat.Cast();
                    TitanicHydra.Cast();
                    RavenousHydra.Cast();
                }
            }
        }

        public static void UseHydraNot(Obj_AI_Base target)
        {
            if (Tiamat.IsOwned() || RavenousHydra.IsOwned() || TitanicHydra.IsOwned())
            {
                if (Tiamat.IsReady() || RavenousHydra.IsReady() || TitanicHydra.IsReady())
                {
                    Tiamat.Cast();
                    TitanicHydra.IsReady();
                    RavenousHydra.Cast();
                }
            }
        }

        public static void UseYomu()
        {
            if (Yomu.IsOwned() && Yomu.IsReady())
                Yomu.Cast();
        }

        public static void UseCastables()
        {
            if (Botrk.IsOwned() || Cutl.IsOwned())
            {
                var t = TargetSelector2.GetTarget(Botrk.Range, DamageType.Physical);
                if (t == null || !t.IsValidTarget()) return;

                if (Botrk.IsReady() || Cutl.IsReady())
                {
                    Botrk.Cast(t);
                    Cutl.Cast(t);
                }
            }
        }

        public static void Initialize()
        {
 
        }
    }
}
