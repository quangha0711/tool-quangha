using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy.SDK.Events;
using EloBuddy;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using SharpDX;
namespace Zilean
{
    class Program
    {
        static Menu Menu;
        static Menu comboMenu, harassMenu, clearMenu,miscMenu;
        static Spell.Targeted E, R;
        static Spell.Skillshot Q;
        static Spell.Active W;
        static float QMANA, WMANA, EMANA, RMANA;
        static readonly List<UnitIncomingDamage> IncomingDamageList = new List<UnitIncomingDamage>();
        static readonly List<AIHeroClient> ChampionList = new List<AIHeroClient>();
        private static readonly Dictionary<int, PredictedDamage> ActiveAttacks = new Dictionary<int, PredictedDamage>();
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Game_OnLoad;
        }
        static void Game_OnLoad (EventArgs args)
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 900, SkillShotType.Circular,300, 2000, 150);
            Q.AllowedCollisionCount = int.MaxValue;
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Targeted(SpellSlot.E, 550);
            R = new Spell.Targeted(SpellSlot.R, 900);
            Game.OnUpdate += Game_OnUpdate;
            Obj_AI_Base.OnSpellCast += Obj_AI_Base_OnDoCast;
            GameObject.OnDelete += MissileClient_OnDelete;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Orbwalker.OnPreAttack += BeforeAttack;
            Spellbook.OnStopCast += SpellbookOnStopCast;
            if (Player.Instance.ChampionName != "Zilean") return;
            foreach (var hero in ObjectManager.Get<AIHeroClient>())
            {
                ChampionList.Add(hero);
            }
            Menu = MainMenu.AddMenu("Zilean","Sida's Zilean");
            comboMenu = Menu.AddSubMenu("Combo", "Combo");
            comboMenu.Add("Combo.Q", new CheckBox("Use Q"));
            comboMenu.Add("Combo.E", new CheckBox("Use E"));
            comboMenu.Add("Combo.W", new CheckBox("Use W"));
            harassMenu = Menu.AddSubMenu("Harass", "Harass");
            harassMenu.Add("Harass.Q", new CheckBox("Use Q"));
            harassMenu.Add("Harass.E", new CheckBox("Use E"));
            harassMenu.Add("Harass.W", new CheckBox("Use W"));
            clearMenu = Menu.AddSubMenu("Clear", "Clear");
            clearMenu.Add("Clear.Q", new CheckBox("Use Q"));
            clearMenu.Add("Clear.W", new CheckBox("Use W"));
            miscMenu = Menu.AddSubMenu("Misc", "Misc");
            miscMenu.Add("AutoR",  new CheckBox("Auto Ult"));
        }

        static void Game_OnUpdate(EventArgs args)
        {

            if (Player.Instance.IsDead)
            {
                return;
            }
            ActiveAttacks.ToList()
            .Where(pair => pair.Value.StartTick < GameTimeTickCount - 3000)
            .ToList()
            .ForEach(pair => ActiveAttacks.Remove(pair.Key));
            var time = Game.Time - 2;
            IncomingDamageList.RemoveAll(damage => time < damage.Time);
            SetMana();
            if (miscMenu["AutoR"].Cast<CheckBox>().CurrentValue)
            {
                Ult();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo1();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Harass1();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                LaneClear1();
            }
        }
        static void BeforeAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (args.Target.IsEnemy && !args.Target.IsMe && !args.Target.IsZombie && args.Target.Distance(Player.Instance) >= 450 && args.Target.Health > Damage.GetAutoAttackDamage(Player.Instance,(Obj_AI_Base) args.Target) && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && (Q.IsReady() || W.IsReady()))
            {
                args.Process = false;
            }
            else
            {
                args.Process = true;
            }
        }
        static void Combo1()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var targetE = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            if (target == null || !target.IsValid)
            {
                return;
            }
            var predQ = Q.GetPrediction(target);
            if (Q.IsReady() && predQ.HitChance >= HitChance.Low && CanMove(target) && comboMenu["Combo.Q"].Cast<CheckBox>().CurrentValue && Player.Instance.Mana > QMANA + RMANA)
            {
                Q.Cast(predQ.CastPosition);
            }
            if (Q.IsReady() && !CanMove(target) && comboMenu["Combo.Q"].Cast<CheckBox>().CurrentValue && Player.Instance.Mana > QMANA + RMANA)
            {
                Q.Cast(target.ServerPosition);
            }
            if (W.IsReady() && target.HasBuff("ZileanQEnemyBomb") && comboMenu["Combo.W"].Cast<CheckBox>().CurrentValue && Player.Instance.Mana > QMANA + RMANA )
            {
                W.Cast();
            }
            if (W.IsReady() && !Q.IsReady() && !E.IsReady() && Player.Instance.Mana > WMANA + RMANA)
            {
                W.Cast();
            }
            if (target.Distance(Player.Instance) > 700 && Player.Instance.Mana > QMANA + EMANA + WMANA + RMANA && Player.Instance.HealthPercent > 20 && E.IsReady() && (Q.IsReady() || W.IsReady()))
            {
                E.Cast(Player.Instance);
            }
            if (E.IsReady() && Player.Instance.Mana > QMANA +  EMANA + RMANA + WMANA && !Q.IsReady() && !W.IsReady() && targetE != null && comboMenu["Combo.E"].Cast<CheckBox>().CurrentValue)
            {
                E.Cast(target);
            }
            if (E.IsReady() && Player.Instance.Mana > QMANA + EMANA + RMANA + WMANA && target.Health < GetIncomingDamage(target) && targetE != null && comboMenu["Combo.E"].Cast<CheckBox>().CurrentValue)
            {
                E.Cast(target);
            }
            if (E.IsReady() && Player.Instance.Mana > QMANA + EMANA + RMANA + WMANA && targetE != null && comboMenu["Combo.E"].Cast<CheckBox>().CurrentValue)
            {
                E.Cast(target);
            }
        }
        static void Harass1()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var targetE = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            if (target == null || !target.IsValid)
            {
                return;
            }
            var predQ = Q.GetPrediction(target);
            if (Q.IsReady() && predQ.HitChance >= HitChance.Medium && CanMove(target) && harassMenu["Harass.Q"].Cast<CheckBox>().CurrentValue && Player.Instance.Mana > QMANA + RMANA)
            {
                Q.Cast(predQ.CastPosition);
            }
            if (Q.IsReady() && !CanMove(target) && harassMenu["Harass.Q"].Cast<CheckBox>().CurrentValue && Player.Instance.Mana > QMANA + RMANA)
            {
                Q.Cast(target.ServerPosition);
            }
            if (W.IsReady() && target.HasBuff("ZileanQEnemyBomb") && harassMenu["Harass.W"].Cast<CheckBox>().CurrentValue && Player.Instance.Mana > QMANA + RMANA)
            {
                W.Cast();
            }
            if (W.IsReady() && !Q.IsReady() && !E.IsReady() && Player.Instance.Mana > WMANA + RMANA)
            {
                W.Cast();
            }
            if (E.IsReady() && Player.Instance.Mana > QMANA + EMANA + RMANA + WMANA && !Q.IsReady() && !W.IsReady() && targetE != null && harassMenu["Harass.E"].Cast<CheckBox>().CurrentValue)
            {
                E.Cast(target);
            }
            if (E.IsReady() && Player.Instance.Mana > QMANA + EMANA + RMANA + WMANA && target.Health < GetIncomingDamage(target) && targetE != null && harassMenu["Harass.E"].Cast<CheckBox>().CurrentValue)
            {
                E.Cast(target);
            }
        }
        static void LaneClear1()
        {
            var qMinion = EntityManager.MinionsAndMonsters.Get(EntityManager.MinionsAndMonsters.EntityType.Minion, EntityManager.UnitTeam.Enemy,Player.Instance.ServerPosition,Q.Range);
            var predMinion = EntityManager.MinionsAndMonsters.GetCircularFarmLocation(qMinion, 300, (int) Q.Range, (Vector2) Player.Instance.ServerPosition);
            if (Orbwalker.IsAutoAttacking)
            {
                return;
            }
            if (Q.IsReady()  && (predMinion.HitNumber  >= 2) && Player.Instance.Mana >= RMANA + WMANA + QMANA + QMANA + EMANA && clearMenu["Clear.Q"].Cast<CheckBox>().CurrentValue)
            {
                Q.Cast(predMinion.CastPosition);
            }
            if (!Q.IsReady() && W.IsReady() && Player.Instance.Mana >= RMANA + WMANA + QMANA + QMANA + EMANA && clearMenu["Clear.W"].Cast<CheckBox>().CurrentValue)
            {
                W.Cast();
            }
        }
        static void Ult()
        {
            foreach (var hero in ObjectManager.Get<AIHeroClient>().Where(x => x.IsAlly && x.Distance(Player.Instance) <= R.Range))
            {
                var UltValid = ValidUlt(hero);
                double dmg = GetIncomingDamage(hero, 1);
                if (UltValid && (hero.Health - dmg < hero.Level * 15 || hero.Health <= GetIncomingDamage(hero, 5)))
                {
                    R.Cast(hero);
                }
            }
        }
        public static bool ValidUlt(AIHeroClient target)
        {
            if (target.HasBuffOfType(BuffType.PhysicalImmunity) ||
                target.IsZombie || target.IsInvulnerable 
                || target.HasBuffOfType(BuffType.Invulnerability) ||
                target.HasBuff("kindredrnodeathbuff")
                || target.HasUndyingBuff())
                return false;
            return true;
        }

        static void SetMana()
        {
            if (Player.Instance.HealthPercent < 20)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
                return;
            }

            QMANA = Q.Handle.SData.Mana;
            WMANA = W.Handle.SData.Mana;
            EMANA = E.Handle.SData.Mana;
            if (!R.IsReady())
                RMANA = WMANA - Player.Instance.PARRegenRate * W.Handle.Cooldown;
            else
                RMANA = R.Handle.SData.Mana;
        }
        static bool CanMove(AIHeroClient target)
        {
            if (target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare) ||
                target.HasBuffOfType(BuffType.Knockup) ||
                target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Fear) ||
                target.HasBuffOfType(BuffType.Knockback) ||
                target.HasBuffOfType(BuffType.Taunt) || target.HasBuffOfType(BuffType.Suppression) ||
                target.IsStunned)
            {
                return false;
            }
            return true;
        }
        public static float GetKsDamage(AIHeroClient t, SpellSlot QWER)
        {
            var totalDmg = ObjectManager.Player.GetSpellDamage(t, QWER);
            totalDmg -= t.HPRegenRate;

            if (totalDmg > t.Health)
            {
                if (Player.HasBuff("summonerexhaust"))
                    totalDmg = totalDmg * 0.6f;

                if (t.HasBuff("ferocioushowl"))
                    totalDmg = totalDmg * 0.7f;

                if (t.BaseSkinName == "Blitzcrank" && !t.HasBuff("BlitzcrankManaBarrierCD") && !t.HasBuff("ManaBarrier"))
                {
                    totalDmg -= t.Mana / 2f;
                }
            }

            totalDmg += (float)GetIncomingDamage(t);
            return totalDmg;
        }
        static double GetIncomingDamage(AIHeroClient target, float time = 0.5f, bool skillshots = true)
        {
            double totalDamage = 0;

            foreach (
                var damage in
                    IncomingDamageList.Where(
                        damage => damage.TargetNetworkId == target.NetworkId && Game.Time - time < damage.Time))
            {
                if (skillshots)
                {
                    totalDamage += damage.Damage;
                }
                else
                {
                    if (!damage.Skillshot)
                        totalDamage += damage.Damage;
                }
            }

            return totalDamage;
        }
        static void Obj_AI_Base_OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.Target != null && args.SData != null)
            {
                if (args.Target.Type == GameObjectType.obj_AI_Base && !sender.IsMelee &&
                    args.Target.Team != sender.Team )
                {
                    IncomingDamageList.Add(new UnitIncomingDamage
                    {
                        Damage = DamageLibrary.GetSpellDamage((AIHeroClient)sender, (AIHeroClient)args.Target, Extensions.GetSpellSlotFromName((AIHeroClient)args.Target, args.SData.Name)),
                        TargetNetworkId = args.Target.NetworkId,
                        Time = Game.Time,
                        Skillshot = false
                    });
                }
            }
            if (ActiveAttacks.ContainsKey(sender.NetworkId) && sender.IsMelee)
            {
                ActiveAttacks[sender.NetworkId].Processed = true;
            }
        }
        public static float GetHealthPrediction(Obj_AI_Base unit, int time, int delay = 70)
        {
            var predictedDamage = 0f;

            foreach (var attack in ActiveAttacks.Values)
            {
                var attackDamage = 0f;
                if (!attack.Processed && attack.Source.IsValidTarget(float.MaxValue) &&
                    attack.Target.IsValidTarget(float.MaxValue) && attack.Target.NetworkId == unit.NetworkId)
                {
                    var landTime = attack.StartTick + attack.Delay +
                                   1000 * Math.Max(0, unit.Distance(attack.Source) - attack.Source.BoundingRadius) /
                                   attack.ProjectileSpeed + delay;

                    if (landTime < GameTimeTickCount + time)
                    {
                        attackDamage = attack.Damage;
                    }
                }

                predictedDamage += attackDamage;
            }

            return unit.Health - predictedDamage;
        }
        static void MissileClient_OnDelete(GameObject sender, EventArgs args)
        {
            var missile = sender as MissileClient;
            if (missile != null && missile.SpellCaster != null)
            {
                var casterNetworkId = missile.SpellCaster.NetworkId;
                foreach (var activeAttack in ActiveAttacks)
                {
                    if (activeAttack.Key == casterNetworkId)
                    {
                        ActiveAttacks[casterNetworkId].Processed = true;
                    }
                }
            }
        }
        private static void SpellbookOnStopCast(Obj_AI_Base sender, SpellbookStopCastEventArgs args)
        {
            if (sender.IsValid && args.StopAnimation)
            {
                if (ActiveAttacks.ContainsKey(sender.NetworkId))
                {
                    ActiveAttacks.Remove(sender.NetworkId);
                }
            }
        }
        public static Obj_AI_Base GetAggroTurret(Obj_AI_Minion minion)
        {
            var ActiveTurret = ActiveAttacks.Values
                .FirstOrDefault(m => m.Source is Obj_AI_Turret && m.Target.NetworkId == minion.NetworkId);
            return ActiveTurret != null ? ActiveTurret.Source : null;
        }
        public static bool HasMinionAggro(Obj_AI_Minion minion)
        {
            return ActiveAttacks.Values.Any(m => m.Source is Obj_AI_Minion && m.Target.NetworkId == minion.NetworkId);
        }
        public static bool HasTurretAggro(Obj_AI_Minion minion)
        {
            return ActiveAttacks.Values.Any(m => m.Source is Obj_AI_Turret && m.Target.NetworkId == minion.NetworkId);
        }
        public static int TurretAggroStartTick(Obj_AI_Minion minion)
        {
            var ActiveTurret = ActiveAttacks.Values
                .FirstOrDefault(m => m.Source is Obj_AI_Turret && m.Target.NetworkId == minion.NetworkId);
            return ActiveTurret != null ? ActiveTurret.StartTick : 0;
        }
        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.SData == null)
            {
                return;
            }
            var targed = args.Target as Obj_AI_Base;

            if (targed != null)
            {
                if (targed.Type == GameObjectType.AIHeroClient && targed.Team != sender.Team && sender.IsMelee)
                {
                    IncomingDamageList.Add(new UnitIncomingDamage
                    {
                        Damage = DamageLibrary.GetSpellDamage((AIHeroClient)sender, (AIHeroClient)targed, Extensions.GetSpellSlotFromName((AIHeroClient)args.Target, args.SData.Name)),
                        TargetNetworkId = args.Target.NetworkId,
                        Time = Game.Time,
                        Skillshot = false
                    });
                }
                if (sender.Team != ObjectManager.Player.Team || !sender.IsValidTarget(3000) ||
                !EloBuddy.SDK.Constants.AutoAttacks.IsAutoAttack(args.SData.Name) || !(args.Target is Obj_AI_Base))
                {
                    return;
                }
                var target = (Obj_AI_Base)args.Target;
                ActiveAttacks.Remove(sender.NetworkId);

                var attackData = new PredictedDamage(
                    sender,
                    target,
                    GameTimeTickCount - Game.Ping / 2,
                    sender.AttackCastDelay * 1000,
                    sender.AttackDelay * 1000 - (sender is Obj_AI_Turret ? 70 : 0),
                    sender.IsMelee ? int.MaxValue : (int)args.SData.MissileSpeed,
                    sender.GetAutoAttackDamage(target, true));
                ActiveAttacks.Add(sender.NetworkId, attackData);
            }
            else
            {
                foreach (
                    var champion in
                        ChampionList.Where(
                            champion =>
                                !champion.IsDead && champion.IsVisible && champion.Team != sender.Team &&
                                champion.Distance(sender) < 2000))
                {
                    if (CanHitSkillShot(champion, args))
                    {
                        IncomingDamageList.Add(new UnitIncomingDamage
                        {
                            Damage = DamageLibrary.GetSpellDamage((AIHeroClient)sender, (AIHeroClient)targed, Extensions.GetSpellSlotFromName((AIHeroClient)champion, args.SData.Name)),
                            TargetNetworkId = champion.NetworkId,
                            Time = Game.Time,
                            Skillshot = true
                        });
                    }
                }
            }
        }
        static int GameTimeTickCount
        {
            get { return (int)(Game.Time * 1000); }
        }
        static bool CanHitSkillShot(Obj_AI_Base target, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.Target == null && target.IsValidTarget(float.MaxValue))
            {
                int Collide = 0;
                if (args.SData.LineMissileEndsAtTargetPoint)
                {
                    Collide = 0;
                }
                else
                {
                    Collide = 1;
                }
                var pred = Prediction.Position.PredictLinearMissile(target, args.SData.CastRange, (int)args.SData.CastRadius, (int)args.SData.CastTime, args.SData.MissileSpeed, Collide).CastPosition;
                if (pred == null)
                    return false;

                if (args.SData.LineWidth > 0)
                {
                    var powCalc = Math.Pow(args.SData.LineWidth + target.BoundingRadius, 2);
                    if (pred.To2D().Distance(args.End.To2D(), args.Start.To2D(), true, true) <= powCalc ||
                        target.ServerPosition.To2D().Distance(args.End.To2D(), args.Start.To2D(), true, true) <= powCalc)
                    {
                        return true;
                    }
                }
                else if (target.Distance(args.End) < 50 + target.BoundingRadius ||
                         pred.Distance(args.End) < 50 + target.BoundingRadius)
                {
                    return true;
                }
            }
            return false;
        }
        class UnitIncomingDamage
        {
            public int TargetNetworkId { get; set; }
            public float Time { get; set; }
            public double Damage { get; set; }
            public bool Skillshot { get; set; }
        }
        private class PredictedDamage
        {
            /// <summary>
            ///     The animation time
            /// </summary>
            public readonly float AnimationTime;

            /// <summary>
            ///     Initializes a new instance of the <see cref="PredictedDamage" /> class.
            /// </summary>
            /// <param name="source">The source.</param>
            /// <param name="target">The target.</param>
            /// <param name="startTick">The start tick.</param>
            /// <param name="delay">The delay.</param>
            /// <param name="animationTime">The animation time.</param>
            /// <param name="projectileSpeed">The projectile speed.</param>
            /// <param name="damage">The damage.</param>
            public PredictedDamage(Obj_AI_Base source,
                Obj_AI_Base target,
                int startTick,
                float delay,
                float animationTime,
                int projectileSpeed,
                float damage)
            {
                Source = source;
                Target = target;
                StartTick = startTick;
                Delay = delay;
                ProjectileSpeed = projectileSpeed;
                Damage = damage;
                AnimationTime = animationTime;
            }

            /// <summary>
            ///     Gets or sets the damage.
            /// </summary>
            /// <value>
            ///     The damage.
            /// </value>
            public float Damage { get; private set; }

            /// <summary>
            ///     Gets or sets the delay.
            /// </summary>
            /// <value>
            ///     The delay.
            /// </value>
            public float Delay { get; private set; }

            /// <summary>
            ///     Gets or sets the projectile speed.
            /// </summary>
            /// <value>
            ///     The projectile speed.
            /// </value>
            public int ProjectileSpeed { get; private set; }

            /// <summary>
            ///     Gets or sets the source.
            /// </summary>
            /// <value>
            ///     The source.
            /// </value>
            public Obj_AI_Base Source { get; private set; }

            /// <summary>
            ///     Gets or sets the start tick.
            /// </summary>
            /// <value>
            ///     The start tick.
            /// </value>
            public int StartTick { get; internal set; }

            /// <summary>
            ///     Gets or sets the target.
            /// </summary>
            /// <value>
            ///     The target.
            /// </value>
            public Obj_AI_Base Target { get; private set; }

            /// <summary>
            ///     Gets or sets a value indicating whether this <see cref="PredictedDamage" /> is processed.
            /// </summary>
            /// <value>
            ///     <c>true</c> if processed; otherwise, <c>false</c>.
            /// </value>
            public bool Processed { get; internal set; }
        }
    }
}
