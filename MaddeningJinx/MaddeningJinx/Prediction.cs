using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using EloBuddy;
using EloBuddy.SDK;
namespace MaddeningJinx
{
    public enum HitChance
    {
        Immobile = 8,
        Dashing = 7,
        VeryHigh = 6,
        High = 5,
        Medium = 4,
        Low = 3,
        Impossible = 2,
        OutOfRange = 1,
        Collision = 0
    }

    public enum SkillshotType
    {
        SkillshotLine,
        SkillshotCircle,
        SkillshotCone
    }

    public enum CollisionableObjects
    {
        Minions,
        Heroes,
        YasuoWall,
        Walls
    }

    public class PredictionInput
    {

        private Vector3 _from;
        private Vector3 _rangeCheckFrom;

        /// <summary>
        ///     Set to true make the prediction hit as many enemy heroes as posible.
        /// </summary>
        public bool Aoe = false;

        /// <summary>
        ///     Set to true if the unit collides with units.
        /// </summary>
        public bool Collision = false;

        /// <summary>
        ///     Array that contains the unit types that the skillshot can collide with.
        /// </summary>
        public CollisionableObjects[] CollisionObjects =
        {
            CollisionableObjects.Minions, CollisionableObjects.YasuoWall
        };

        /// <summary>
        ///     The skillshot delay in seconds.
        /// </summary>
        public float Delay;

        /// <summary>
        ///     The skillshot width's radius or the angle in case of the cone skillshots.
        /// </summary>
        public float Radius = 1f;

        /// <summary>
        ///     The skillshot range in units.
        /// </summary>
        public float Range = float.MaxValue;

        /// <summary>
        ///     The skillshot speed in units per second.
        /// </summary>
        public float Speed = float.MaxValue;

        /// <summary>
        ///     The skillshot type.
        /// </summary>
        public SkillshotType Type = SkillshotType.SkillshotLine;

        /// <summary>
        ///     The unit that the prediction will made for.
        /// </summary>
        public Obj_AI_Base Unit = ObjectManager.Player;

        /// <summary>
        ///     Source unit for the prediction 
        /// </summary>
        public Obj_AI_Base Source = ObjectManager.Player;

        /// <summary>
        ///     Set to true to increase the prediction radius by the unit bounding radius.
        /// </summary>
        public bool UseBoundingRadius = true;

        /// <summary>
        ///     The position from where the skillshot missile gets fired.
        /// </summary>
        public Vector3 From
        {
            get { return _from.To2D().IsValid() ? _from : ObjectManager.Player.ServerPosition; }
            set { _from = value; }
        }

        /// <summary>
        ///     The position from where the range is checked.
        /// </summary>
        public Vector3 RangeCheckFrom
        {
            get
            {
                return _rangeCheckFrom.To2D().IsValid()
                    ? _rangeCheckFrom
                    : (From.To2D().IsValid() ? From : ObjectManager.Player.ServerPosition);
            }
            set { _rangeCheckFrom = value; }
        }

        internal float RealRadius
        {
            get { return UseBoundingRadius ? Radius + Unit.BoundingRadius : Radius; }
        }
    }
    public class PredictionOutput
    {
        internal int _aoeTargetsHitCount;
        private Vector3 _castPosition;
        private Vector3 _unitPosition;

        /// <summary>
        ///     The list of the targets that the spell will hit (only if aoe was enabled).
        /// </summary>
        public List<AIHeroClient> AoeTargetsHit = new List<AIHeroClient>();

        /// <summary>
        ///     The list of the units that the skillshot will collide with.
        /// </summary>
        public List<Obj_AI_Base> CollisionObjects = new List<Obj_AI_Base>();

        /// <summary>
        ///     Returns the hitchance.
        /// </summary>
        public HitChance Hitchance = HitChance.Impossible;

        internal PredictionInput Input;

        /// <summary>
        ///     The position where the skillshot should be casted to increase the accuracy.
        /// </summary>
        public Vector3 CastPosition
        {
            get
            {
                return _castPosition.IsValid() && _castPosition.To2D().IsValid()
                    ? _castPosition.SetZ()
                    : Input.Unit.ServerPosition;
            }
            set { _castPosition = value; }
        }

        /// <summary>
        ///     The number of targets the skillshot will hit (only if aoe was enabled).
        /// </summary>
        public int AoeTargetsHitCount
        {
            get { return Math.Max(_aoeTargetsHitCount, AoeTargetsHit.Count); }
        }

        /// <summary>
        ///     The position where the unit is going to be when the skillshot reaches his position.
        /// </summary>
        public Vector3 UnitPosition
        {
            get { return _unitPosition.To2D().IsValid() ? _unitPosition.SetZ() : Input.Unit.ServerPosition; }
            set { _unitPosition = value; }
        }
    }

    /// <summary>
    ///     Class used for calculating the position of the given unit after a delay.
    /// </summary>
    public static class Prediction
    {
        public static PredictionOutput GetPrediction(Obj_AI_Base unit, float delay)
        {
            return GetPrediction(new PredictionInput { Unit = unit, Delay = delay });
        }

        public static PredictionOutput GetPrediction(Obj_AI_Base unit, float delay, float radius)
        {
            return GetPrediction(new PredictionInput { Unit = unit, Delay = delay, Radius = radius });
        }

        public static PredictionOutput GetPrediction(Obj_AI_Base unit, float delay, float radius, float speed)
        {
            return GetPrediction(new PredictionInput { Unit = unit, Delay = delay, Radius = radius, Speed = speed });
        }

        public static PredictionOutput GetPrediction(Obj_AI_Base unit,
            float delay,
            float radius,
            float speed,
            CollisionableObjects[] collisionable)
        {
            return
                GetPrediction(
                    new PredictionInput
                    {
                        Unit = unit,
                        Delay = delay,
                        Radius = radius,
                        Speed = speed,
                        CollisionObjects = collisionable
                    });
        }

        public static PredictionOutput GetPrediction(PredictionInput input)
        {
            return GetPrediction(input, true, true);
        }

        internal static PredictionOutput GetPrediction(PredictionInput input, bool ft, bool checkCollision)
        {
            PredictionOutput result = null;

            if (!input.Unit.IsValidTarget(float.MaxValue, false))
            {
                return new PredictionOutput();
            }

            if (ft)
            {
                //Increase the delay due to the latency and server tick:
                input.Delay += Game.Ping / 2000f + 0.06f;

                if (input.Aoe)
                {
                    return AoePrediction.GetPrediction(input);
                }
            }

            //Target too far away.
            if (Math.Abs(input.Range - float.MaxValue) > float.Epsilon &&
                input.Unit.Distance(input.RangeCheckFrom, true) > Math.Pow(input.Range * 1.5, 2))
            {
                return new PredictionOutput { Input = input };
            }

            //Unit is dashing.
            if (EloBuddy.SDK.Events.Dash.IsDashing(input.Unit))
            {
                result = GetDashingPrediction(input);
            }
            else
            {
                //Unit is immobile.
                var remainingImmobileT = UnitIsImmobileUntil(input.Unit);
                if (remainingImmobileT >= 0d)
                {
                    result = GetImmobilePrediction(input, remainingImmobileT);
                }
            }

            //Normal prediction
            if (result == null)
            {
                result = GetPositionOnPath(input, input.Unit.GetWaypoints(), input.Unit.MoveSpeed);
            }

            //Check if the unit position is in range
            if (Math.Abs(input.Range - float.MaxValue) > float.Epsilon)
            {
                if (result.Hitchance >= HitChance.High &&
                    input.RangeCheckFrom.Distance(input.Unit.Position, true) >
                    Math.Pow(input.Range + input.RealRadius * 3 / 4, 2))
                {
                    result.Hitchance = HitChance.Medium;
                }

                if (input.RangeCheckFrom.Distance(result.UnitPosition, true) >
                    Math.Pow(input.Range + (input.Type == SkillshotType.SkillshotCircle ? input.RealRadius : 0), 2))
                {
                    result.Hitchance = HitChance.OutOfRange;
                }

                /* This does not need to be handled for the updated predictions, but left as a reference.*/
                if (input.RangeCheckFrom.Distance(result.CastPosition, true) > Math.Pow(input.Range, 2))
                {
                    if (result.Hitchance != HitChance.OutOfRange)
                    {
                        result.CastPosition = input.RangeCheckFrom +
                                              input.Range *
                                              (result.UnitPosition - input.RangeCheckFrom).To2D().Normalized().To3D();
                    }
                    else
                    {
                        result.Hitchance = HitChance.OutOfRange;
                    }
                }
            }

            //Set hit chance
            if (result.Hitchance == HitChance.High)
            {
                result = WayPointAnalysis(result, input);
                //.debug(input.Unit.BaseSkinName + result.Hitchance);
            }

            //Check for collision
            if (checkCollision && input.Collision && result.Hitchance > HitChance.Impossible)
            {
                var positions = new List<Vector3> { result.CastPosition };
                var originalUnit = input.Unit;
                if (Collision.GetCollision(positions, input))
                    result.Hitchance = HitChance.Collision;
            }
            return result;
        }
        internal static class WaypointTracker
        {
            public static readonly Dictionary<int, List<Vector2>> StoredPaths = new Dictionary<int, List<Vector2>>();
            public static readonly Dictionary<int, int> StoredTick = new Dictionary<int, int>();
        }
        public static List<Vector2> CutPath(this List<Vector2> path, float distance)
        {
            var result = new List<Vector2>();
            var Distance = distance;
            if (distance < 0)
            {
                path[0] = path[0] + distance * (path[1] - path[0]).Normalized();
                return path;
            }

            for (var i = 0; i < path.Count - 1; i++)
            {
                var dist = path[i].Distance(path[i + 1]);
                if (dist > Distance)
                {
                    result.Add(path[i] + Distance * (path[i + 1] - path[i]).Normalized());
                    for (var j = i + 1; j < path.Count; j++)
                    {
                        result.Add(path[j]);
                    }

                    break;
                }
                Distance -= dist;
            }
            return result.Count > 0 ? result : new List<Vector2> { path.Last() };
        }
        public static List<Vector2> GetWaypoints(this Obj_AI_Base unit)
        {
            var result = new List<Vector2>();

            if (unit.IsVisible)
            {
                result.Add(unit.ServerPosition.To2D());
                var path = unit.Path;
                if (path.Length > 0)
                {
                    var first = path[0].To2D();
                    if (first.Distance(result[0], true) > 40)
                    {
                        result.Add(first);
                    }

                    for (int i = 1; i < path.Length; i++)
                    {
                        result.Add(path[i].To2D());
                    }
                }
            }
            else if (WaypointTracker.StoredPaths.ContainsKey(unit.NetworkId))
            {
                var path = WaypointTracker.StoredPaths[unit.NetworkId];
                var timePassed = (UnitTracker.TickCount - WaypointTracker.StoredTick[unit.NetworkId]) / 1000f;
                if (path.PathLength() >= unit.MoveSpeed * timePassed)
                {
                    result = CutPath(path, (int)(unit.MoveSpeed * timePassed));
                }
            }

            return result;
        }
        public static float PathLength(this List<Vector2> path)
        {
            var distance = 0f;
            for (var i = 0; i < path.Count - 1; i++)
            {
                distance += path[i].Distance(path[i + 1]);
            }
            return distance;
        }
        internal static PredictionOutput WayPointAnalysis(PredictionOutput result, PredictionInput input)
        {
            if (!input.Unit.IsValid() || input.Radius == 1)
            {
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }

            // CAN'T MOVE SPELLS ///////////////////////////////////////////////////////////////////////////////////

            if (UnitTracker.GetSpecialSpellEndTime(input.Unit) > 100 || input.Unit.HasBuff("Recall") || (UnitTracker.GetLastStopMoveTime(input.Unit) < 100 && input.Unit.IsRooted))
            {
                debug("CAN'T MOVE SPELLS");
                result.Hitchance = HitChance.VeryHigh;
                result.CastPosition = input.Unit.Position;
                return result;
            }

            // NEW VISABLE ///////////////////////////////////////////////////////////////////////////////////

            if (UnitTracker.GetLastVisableTime(input.Unit) < 100)
            {
                debug("PRED: NEW VISABLE");
                result.Hitchance = HitChance.Medium;
                return result;
            }


            // PREPARE MATH ///////////////////////////////////////////////////////////////////////////////////

            result.Hitchance = HitChance.Medium;
            var lastWaypiont = input.Unit.GetWaypoints().Last().To3D();
            var distanceUnitToWaypoint = lastWaypiont.Distance(input.Unit.ServerPosition);
            var distanceFromToUnit = input.From.Distance(input.Unit.ServerPosition);
            var distanceFromToWaypoint = lastWaypiont.Distance(input.From);
            var getAngle = GetAngle(input.From, input.Unit);
            float speedDelay = distanceFromToUnit / input.Speed;

            if (Math.Abs(input.Speed - float.MaxValue) < float.Epsilon)
                speedDelay = 0;

            float totalDelay = speedDelay + input.Delay;
            float moveArea = input.Unit.MoveSpeed * totalDelay;
            float fixRange = moveArea * 0.35f;
            float pathMinLen = 800 + moveArea;

            double angleMove = 32;

            if (input.Type == SkillshotType.SkillshotCircle)
            {
                fixRange -= input.Radius / 2;
            }

            // FIX RANGE ///////////////////////////////////////////////////////////////////////////////////
            if (distanceFromToWaypoint <= distanceFromToUnit)
            {
                if (distanceFromToUnit > input.Range - fixRange)
                {
                    result.Hitchance = HitChance.Medium;
                    return result;
                }
            }

            var points = CirclePoints(15, 450, input.Unit.Position).Where(x => x.IsWall());

            if (points.Count() > 2)
            {
                var runOutWall = true;
                foreach (var point in points)
                {
                    if (input.Unit.Position.Distance(point) > lastWaypiont.Distance(point))
                    {
                        runOutWall = false;
                    }
                }
                if (runOutWall)
                {
                    debug("PRED: RUN OUT WALL");
                    result.Hitchance = HitChance.VeryHigh;
                    return result;
                }
            }

            if (input.Unit.GetWaypoints().Count == 1)
            {
                if (UnitTracker.GetLastStopMoveTime(input.Unit) < 600)
                {
                    //OktwCommon.debug("PRED: STOP HIGH");
                    result.Hitchance = HitChance.High;
                    return result;
                }
                else
                {
                    debug("PRED: STOP LOGIC");
                    result.Hitchance = HitChance.VeryHigh;
                }
            }

            // SPAM POSITION ///////////////////////////////////////////////////////////////////////////////////

            if (UnitTracker.SpamSamePlace(input.Unit))
            {
                debug("PRED: SPAM POSITION");
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }

            // SPECIAL CASES ///////////////////////////////////////////////////////////////////////////////////

            if (distanceFromToUnit < 250)
            {
                debug("PRED: SPECIAL CASES NEAR");
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }
            else if (input.Unit.MoveSpeed < 250)
            {
                debug("PRED: SPECIAL CASES SLOW");
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }
            else if (distanceFromToWaypoint < 250)
            {
                debug("PRED: SPECIAL CASES ON WAY");
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }

            // LONG CLICK DETECTION ///////////////////////////////////////////////////////////////////////////////////

            if (distanceUnitToWaypoint > pathMinLen)
            {
                debug("PRED: LONG CLICK DETECTION");
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }

            // LOW HP DETECTION ///////////////////////////////////////////////////////////////////////////////////

            if (input.Unit.HealthPercent < 20 || ObjectManager.Player.HealthPercent < 20)
            {
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }

            // RUN IN LANE DETECTION /////////////////////////////////////////////////////////////////////////////////// 

            if (getAngle < angleMove && UnitTracker.GetLastNewPathTime(input.Unit) < 100)
            {
                debug(GetAngle(input.From, input.Unit) + " PRED: ANGLE " + angleMove + " DIS " + distanceUnitToWaypoint);
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }

            // CIRCLE NEW PATH ///////////////////////////////////////////////////////////////////////////////////

            if (input.Type == SkillshotType.SkillshotCircle)
            {
                if (UnitTracker.GetLastNewPathTime(input.Unit) < 100 && distanceUnitToWaypoint > fixRange)
                {
                    debug("PRED: CIRCLE NEW PATH");
                    result.Hitchance = HitChance.VeryHigh;
                    return result;
                }
            }
            //Program.debug("PRED: NO DETECTION");
            return result;
        }
        public static List<Vector3> CirclePoints(float CircleLineSegmentN, float radius, Vector3 position)
        {
            List<Vector3> points = new List<Vector3>();
            for (var i = 1; i <= CircleLineSegmentN; i++)
            {
                var angle = i * 2 * Math.PI / CircleLineSegmentN;
                var point = new Vector3(position.X + radius * (float)Math.Cos(angle), position.Y + radius * (float)Math.Sin(angle), position.Z);
                points.Add(point);
            }
            return points;
        }
        public static void debug(string msg)
        {
            if (true)
            {
                Console.WriteLine(msg);
            }
            if (false)
            {
                Chat.Print(msg);
            }
        }
        internal static PredictionOutput GetDashingPrediction(PredictionInput input)
        {
            var dashData = input.Unit.GetDashInfo();
            var result = new PredictionOutput { Input = input };
            //Normal dashes.
            if (!dashData.IsBlink)
            {
                //Mid air:
                var endP = dashData.Path.Last();
                var dashPred = GetPositionOnPath(
                    input, new List<Vector2> { input.Unit.ServerPosition.To2D(), endP }, dashData.Speed);
                if (dashPred.Hitchance >= HitChance.High && dashPred.UnitPosition.To2D().Distance(input.Unit.Position.To2D(), endP, true) < 200)
                {
                    dashPred.CastPosition = dashPred.UnitPosition;
                    dashPred.Hitchance = HitChance.Dashing;
                    return dashPred;
                }

                //At the end of the dash:
                if (dashData.Path.PathLength() > 200)
                {
                    var timeToPoint = input.Delay / 2f + input.From.To2D().Distance(endP) / input.Speed - 0.25f;
                    if (timeToPoint <=
                        input.Unit.Distance(endP) / dashData.Speed + input.RealRadius / input.Unit.MoveSpeed)
                    {
                        return new PredictionOutput
                        {
                            CastPosition = endP.To3D(),
                            UnitPosition = endP.To3D(),
                            Hitchance = HitChance.Dashing
                        };
                    }
                }
                result.CastPosition = dashData.Path.Last().To3D();
                result.UnitPosition = result.CastPosition;

                //Figure out where the unit is going.
            }

            return result;
        }
        private static readonly Dictionary<int, DashItem> DetectedDashes = new Dictionary<int, DashItem>();

        public static DashItem GetDashInfo(this Obj_AI_Base unit)
        {
            return DetectedDashes.ContainsKey(unit.NetworkId) ? DetectedDashes[unit.NetworkId] : new DashItem();
        }
        public class DashItem
        {
            /// <summary>
            /// The duration
            /// </summary>
            public int Duration;

            /// <summary>
            /// The end position
            /// </summary>
            public Vector2 EndPos;

            /// <summary>
            /// The end tick
            /// </summary>
            public int EndTick;

            /// <summary>
            /// The path
            /// </summary>
            public List<Vector2> Path;

            /// <summary>
            /// The speed
            /// </summary>
            public float Speed;

            /// <summary>
            /// The start position
            /// </summary>
            public Vector2 StartPos;

            /// <summary>
            /// The start tick
            /// </summary>
            public int StartTick;

            /// <summary>
            /// The unit
            /// </summary>
            public Obj_AI_Base Unit;

            /// <summary>
            /// <c>true</c> if the dash was a blink, else <c>false</c>
            /// </summary>
            public bool IsBlink;
        }
        public static List<Vector2> To2D(this List<Vector3> path)
        {
            return path.Select(point => point.To2D()).ToList();
        }
        private static void ObjAiHeroOnOnNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            if (sender.IsValid())
            {
                if (!DetectedDashes.ContainsKey(sender.NetworkId))
                {
                    DetectedDashes.Add(sender.NetworkId, new DashItem());
                }

                if (args.IsDash)
                {
                    var path = new List<Vector2> { sender.ServerPosition.To2D() };
                    path.AddRange(args.Path.ToList().To2D());

                    DetectedDashes[sender.NetworkId].StartTick = UnitTracker.TickCount;
                    DetectedDashes[sender.NetworkId].Speed = args.Speed;
                    DetectedDashes[sender.NetworkId].StartPos = sender.ServerPosition.To2D();
                    DetectedDashes[sender.NetworkId].Unit = sender;
                    DetectedDashes[sender.NetworkId].Path = path;
                    DetectedDashes[sender.NetworkId].EndPos = DetectedDashes[sender.NetworkId].Path.Last();
                    DetectedDashes[sender.NetworkId].EndTick = DetectedDashes[sender.NetworkId].StartTick +
                                                           (int)
                                                               (1000 *
                                                                (DetectedDashes[sender.NetworkId].EndPos.Distance(
                                                                    DetectedDashes[sender.NetworkId].StartPos) / DetectedDashes[sender.NetworkId].Speed));
                    DetectedDashes[sender.NetworkId].Duration = DetectedDashes[sender.NetworkId].EndTick - DetectedDashes[sender.NetworkId].StartTick;

                    TriggerOnDash(DetectedDashes[sender.NetworkId].Unit, DetectedDashes[sender.NetworkId]);
                }
                else
                {
                    DetectedDashes[sender.NetworkId].EndTick = 0;
                }
            }
        }
        public static event OnDashed OnDash;
        public delegate void OnDashed(Obj_AI_Base sender, DashItem args);
        public static void TriggerOnDash(Obj_AI_Base sender, DashItem args)
        {
            var dashHandler = OnDash;
            if (dashHandler != null)
            {
                dashHandler(sender, args);
            }
        }

        internal static PredictionOutput GetImmobilePrediction(PredictionInput input, double remainingImmobileT)
        {
            var timeToReachTargetPosition = input.Delay + input.Unit.Distance(input.From) / input.Speed;

            if (timeToReachTargetPosition <= remainingImmobileT + input.RealRadius / input.Unit.MoveSpeed)
            {
                return new PredictionOutput
                {
                    CastPosition = input.Unit.ServerPosition,
                    UnitPosition = input.Unit.Position,
                    Hitchance = HitChance.Immobile
                };
            }

            return new PredictionOutput
            {
                Input = input,
                CastPosition = input.Unit.ServerPosition,
                UnitPosition = input.Unit.ServerPosition,
                Hitchance = HitChance.High
                /*timeToReachTargetPosition - remainingImmobileT + input.RealRadius / input.Unit.MoveSpeed < 0.4d ? HitChance.High : HitChance.Medium*/
            };
        }



        internal static double GetAngle(Vector3 from, Obj_AI_Base target)
        {
            var C = target.ServerPosition.To2D();
            var A = target.GetWaypoints().Last();

            if (C == A)
                return 60;

            var B = from.To2D();

            var AB = Math.Pow(A.X - B.X, 2) + Math.Pow(A.Y - B.Y, 2);
            var BC = Math.Pow(B.X - C.X, 2) + Math.Pow(B.Y - C.Y, 2);
            var AC = Math.Pow(A.X - C.X, 2) + Math.Pow(A.Y - C.Y, 2);

            return Math.Cos((AB + BC - AC) / (2 * Math.Sqrt(AB) * Math.Sqrt(BC))) * 180 / Math.PI;
        }

        internal static double UnitIsImmobileUntil(Obj_AI_Base unit)
        {
            var result =
                unit.Buffs.Where(
                    buff =>
                        buff.IsActive && Game.Time <= buff.EndTime &&
                        (buff.Type == BuffType.Charm || buff.Type == BuffType.Knockup || buff.Type == BuffType.Stun ||
                         buff.Type == BuffType.Suppression || buff.Type == BuffType.Snare || buff.Type == BuffType.Fear
                         || buff.Type == BuffType.Taunt || buff.Type == BuffType.Knockback))
                    .Aggregate(0d, (current, buff) => Math.Max(current, buff.EndTime));
            return (result - Game.Time);
        }

        internal static PredictionOutput GetPositionOnPath(PredictionInput input, List<Vector2> path, float speed = -1)
        {
            speed = (Math.Abs(speed - (-1)) < float.Epsilon) ? input.Unit.MoveSpeed : speed;

            if (path.Count <= 1 || (!EloBuddy.SDK.Events.Dash.IsDashing(input.Unit)))
            {
                return new PredictionOutput
                {
                    Input = input,
                    UnitPosition = input.Unit.ServerPosition,
                    CastPosition = input.Unit.ServerPosition,
                    Hitchance = HitChance.High
                };
            }

            var pLength = path.PathLength();

            //Skillshots with only a delay
            if (pLength >= input.Delay * speed - input.RealRadius && Math.Abs(input.Speed - float.MaxValue) < float.Epsilon)
            {
                var tDistance = input.Delay * speed - input.RealRadius;

                for (var i = 0; i < path.Count - 1; i++)
                {
                    var a = path[i];
                    var b = path[i + 1];
                    var d = a.Distance(b);

                    if (d >= tDistance)
                    {
                        var direction = (b - a).Normalized();

                        var cp = a + direction * tDistance;
                        var p = a +
                                direction *
                                ((i == path.Count - 2)
                                    ? Math.Min(tDistance + input.RealRadius, d)
                                    : (tDistance + input.RealRadius));

                        return new PredictionOutput
                        {
                            Input = input,
                            CastPosition = cp.To3D(),
                            UnitPosition = p.To3D(),
                            Hitchance = HitChance.High
                        };
                    }

                    tDistance -= d;
                }
            }

            //Skillshot with a delay and speed.
            if (pLength >= input.Delay * speed - input.RealRadius &&
                Math.Abs(input.Speed - float.MaxValue) > float.Epsilon)
            {
                var d = input.Delay * speed - input.RealRadius;
                if (input.Type == SkillshotType.SkillshotLine || input.Type == SkillshotType.SkillshotCone)
                {
                    if (input.From.Distance(input.Unit.ServerPosition, true) < 200 * 200)
                    {
                        d = input.Delay * speed;
                    }
                }

                path = path.CutPath(d);
                var tT = 0f;
                for (var i = 0; i < path.Count - 1; i++)
                {
                    var a = path[i];
                    var b = path[i + 1];
                    var tB = a.Distance(b) / speed;
                    var direction = (b - a).Normalized();
                    a = a - speed * tT * direction;
                    var sol = Geometry.VectorMovementCollision(a, b, speed, input.From.To2D(), input.Speed, tT);
                    var t = (float)sol[0];
                    var pos = (Vector2)sol[1];

                    if (pos.IsValid() && t >= tT && t <= tT + tB)
                    {
                        if (pos.Distance(b, true) < 20)
                            break;
                        var p = pos + input.RealRadius * direction;

                        if (input.Type == SkillshotType.SkillshotLine && false)
                        {
                            var alpha = (input.From.To2D() - p).AngleBetween(a - b);
                            if (alpha > 30 && alpha < 180 - 30)
                            {
                                var beta = (float)Math.Asin(input.RealRadius / p.Distance(input.From));
                                var cp1 = input.From.To2D() + (p - input.From.To2D()).Rotated(beta);
                                var cp2 = input.From.To2D() + (p - input.From.To2D()).Rotated(-beta);

                                pos = cp1.Distance(pos, true) < cp2.Distance(pos, true) ? cp1 : cp2;
                            }
                        }

                        return new PredictionOutput
                        {
                            Input = input,
                            CastPosition = pos.To3D(),
                            UnitPosition = p.To3D(),
                            Hitchance = HitChance.High
                        };
                    }
                    tT += tB;
                }
            }

            var position = path.Last();
            return new PredictionOutput
            {
                Input = input,
                CastPosition = position.To3D(),
                UnitPosition = position.To3D(),
                Hitchance = HitChance.Medium
            };
        }


    }
    internal static class AoePrediction
    {
        public static PredictionOutput GetPrediction(PredictionInput input)
        {
            switch (input.Type)
            {
                case SkillshotType.SkillshotCircle:
                    return Circle.GetPrediction(input);
                case SkillshotType.SkillshotCone:
                    return Cone.GetPrediction(input);
                case SkillshotType.SkillshotLine:
                    return Line.GetPrediction(input);
            }
            return new PredictionOutput();
        }

        internal static List<PossibleTarget> GetPossibleTargets(PredictionInput input)
        {
            var result = new List<PossibleTarget>();
            var originalUnit = input.Unit;
            foreach (var enemy in
                EntityManager.Heroes.Enemies.FindAll(
                    h =>
                        h.NetworkId != originalUnit.NetworkId &&
                        h.IsValidTarget((input.Range + 200 + input.RealRadius), true, input.RangeCheckFrom)))
            {
                input.Unit = enemy;
                var prediction = Prediction.GetPrediction(input, false, false);
                if (prediction.Hitchance >= HitChance.High)
                {
                    result.Add(new PossibleTarget { Position = prediction.UnitPosition.To2D(), Unit = enemy });
                }
            }
            return result;
        }

        public static class Circle
        {
            public static PredictionOutput GetPrediction(PredictionInput input)
            {
                var mainTargetPrediction = Prediction.GetPrediction(input, false, true);
                var posibleTargets = new List<PossibleTarget>
                {
                    new PossibleTarget { Position = mainTargetPrediction.UnitPosition.To2D(), Unit = input.Unit }
                };

                if (mainTargetPrediction.Hitchance >= HitChance.Medium)
                {
                    //Add the posible targets  in range:
                    posibleTargets.AddRange(GetPossibleTargets(input));
                }

                while (posibleTargets.Count > 1)
                {
                    var mecCircle = GetMec(posibleTargets.Select(h => h.Position).ToList());

                    if (mecCircle.Radius <= input.RealRadius - 10 &&
                        Vector2.DistanceSquared(mecCircle.Center, input.RangeCheckFrom.To2D()) <
                        input.Range * input.Range)
                    {
                        return new PredictionOutput
                        {
                            AoeTargetsHit = posibleTargets.Select(h => (AIHeroClient)h.Unit).ToList(),
                            CastPosition = mecCircle.Center.To3D(),
                            UnitPosition = mainTargetPrediction.UnitPosition,
                            Hitchance = mainTargetPrediction.Hitchance,
                            Input = input,
                            _aoeTargetsHitCount = posibleTargets.Count
                        };
                    }

                    float maxdist = -1;
                    var maxdistindex = 1;
                    for (var i = 1; i < posibleTargets.Count; i++)
                    {
                        var distance = Vector2.DistanceSquared(posibleTargets[i].Position, posibleTargets[0].Position);
                        if (distance > maxdist || maxdist.CompareTo(-1) == 0)
                        {
                            maxdistindex = i;
                            maxdist = distance;
                        }
                    }
                    posibleTargets.RemoveAt(maxdistindex);
                }

                return mainTargetPrediction;
            }
        }

        public static MecCircle GetMec(List<Vector2> points)
        {
            var center = new Vector2();
            float radius;

            var ConvexHull = MakeConvexHull(points);
            FindMinimalBoundingCircle(ConvexHull, out center, out radius);
            return new MecCircle(center, radius);
        }
        public struct MecCircle
        {
            /// <summary>
            /// The center
            /// </summary>
            public Vector2 Center;

            /// <summary>
            /// The radius
            /// </summary>
            public float Radius;

            /// <summary>
            /// Initializes a new instance of the <see cref="MecCircle"/> struct.
            /// </summary>
            /// <param name="center">The center.</param>
            /// <param name="radius">The radius.</param>
            public MecCircle(Vector2 center, float radius)
            {
                Center = center;
                Radius = radius;
            }
        }
        public static void FindMinimalBoundingCircle(List<Vector2> points, out Vector2 center, out float radius)
        {
            // Find the convex hull.
            var hull = MakeConvexHull(points);

            // The best solution so far.
            var best_center = points[0];
            var best_radius2 = float.MaxValue;

            // Look at pairs of hull points.
            for (var i = 0; i < hull.Count - 1; i++)
            {
                for (var j = i + 1; j < hull.Count; j++)
                {
                    // Find the circle through these two points.
                    var test_center = new Vector2((hull[i].X + hull[j].X) / 2f, (hull[i].Y + hull[j].Y) / 2f);
                    var dx = test_center.X - hull[i].X;
                    var dy = test_center.Y - hull[i].Y;
                    var test_radius2 = dx * dx + dy * dy;

                    // See if this circle would be an improvement.
                    if (test_radius2 < best_radius2)
                    {
                        // See if this circle encloses all of the points.
                        if (CircleEnclosesPoints(test_center, test_radius2, points, i, j, -1))
                        {
                            // Save this solution.
                            best_center = test_center;
                            best_radius2 = test_radius2;
                        }
                    }
                } // for i
            } // for j

            // Look at triples of hull points.
            for (var i = 0; i < hull.Count - 2; i++)
            {
                for (var j = i + 1; j < hull.Count - 1; j++)
                {
                    for (var k = j + 1; k < hull.Count; k++)
                    {
                        // Find the circle through these three points.
                        Vector2 test_center;
                        float test_radius2;
                        FindCircle(hull[i], hull[j], hull[k], out test_center, out test_radius2);

                        // See if this circle would be an improvement.
                        if (test_radius2 < best_radius2)
                        {
                            // See if this circle encloses all of the points.
                            if (CircleEnclosesPoints(test_center, test_radius2, points, i, j, k))
                            {
                                // Save this solution.
                                best_center = test_center;
                                best_radius2 = test_radius2;
                            }
                        }
                    } // for k
                } // for i
            } // for j

            center = best_center;
            if (best_radius2 == float.MaxValue)
            {
                radius = 0;
            }
            else
            {
                radius = (float)Math.Sqrt(best_radius2);
            }
        }
        private static bool CircleEnclosesPoints(Vector2 center,
            float radius2,
            List<Vector2> points,
            int skip1,
            int skip2,
            int skip3)
        {
            return (from point in points.Where((t, i) => (i != skip1) && (i != skip2) && (i != skip3))
                    let dx = center.X - point.X
                    let dy = center.Y - point.Y
                    select dx * dx + dy * dy).All(test_radius2 => !(test_radius2 > radius2));
        }
        public static List<Vector2> MakeConvexHull(List<Vector2> points)
        {
            // Cull.
            points = HullCull(points);

            // Find the remaining point with the smallest Y value.
            // if (there's a tie, take the one with the smaller X value.
            Vector2[] best_pt = { points[0] };
            foreach (
                var pt in points.Where(pt => (pt.Y < best_pt[0].Y) || ((pt.Y == best_pt[0].Y) && (pt.X < best_pt[0].X)))
                )
            {
                best_pt[0] = pt;
            }

            // Move this point to the convex hull.
            var hull = new List<Vector2> { best_pt[0] };
            points.Remove(best_pt[0]);

            // Start wrapping up the other points.
            float sweep_angle = 0;
            for (;;)
            {
                // If all of the points are on the hull, we're done.
                if (points.Count == 0)
                {
                    break;
                }

                // Find the point with smallest AngleValue
                // from the last point.
                var X = hull[hull.Count - 1].X;
                var Y = hull[hull.Count - 1].Y;
                best_pt[0] = points[0];
                float best_angle = 3600;

                // Search the rest of the points.
                foreach (var pt in points)
                {
                    var test_angle = AngleValue(X, Y, pt.X, pt.Y);
                    if ((test_angle >= sweep_angle) && (best_angle > test_angle))
                    {
                        best_angle = test_angle;
                        best_pt[0] = pt;
                    }
                }

                // See if the first point is better.
                // If so, we are done.
                var first_angle = AngleValue(X, Y, hull[0].X, hull[0].Y);
                if ((first_angle >= sweep_angle) && (best_angle >= first_angle))
                {
                    // The first point is better. We're done.
                    break;
                }

                // Add the best point to the convex hull.
                hull.Add(best_pt[0]);
                points.Remove(best_pt[0]);

                sweep_angle = best_angle;
            }

            return hull;
        }
        private static float AngleValue(float x1, float y1, float x2, float y2)
        {
            float t;

            var dx = x2 - x1;
            var ax = Math.Abs(dx);
            var dy = y2 - y1;
            var ay = Math.Abs(dy);
            if (ax + ay == 0)
            {
                // if (the two points are the same, return 360.
                t = 360f / 9f;
            }
            else
            {
                t = dy / (ax + ay);
            }
            if (dx < 0)
            {
                t = 2 - t;
            }
            else if (dy < 0)
            {
                t = 4 + t;
            }
            return t * 90;
        }
        public static Vector2[] g_MinMaxCorners;
        public static RectangleF g_MinMaxBox;
        public static Vector2[] g_NonCulledPoints;
        private static List<Vector2> HullCull(List<Vector2> points)
        {
            // Find a culling box.
            var culling_box = GetMinMaxBox(points);

            // Cull the points.
            var results =
                points.Where(
                    pt =>
                        pt.X <= culling_box.Left || pt.X >= culling_box.Right || pt.Y <= culling_box.Top ||
                        pt.Y >= culling_box.Bottom).ToList();

            g_NonCulledPoints = new Vector2[results.Count]; // For debugging.
            results.CopyTo(g_NonCulledPoints); // For debugging.
            return results;
        }
        private static RectangleF GetMinMaxBox(List<Vector2> points)
        {
            // Find the MinMax quadrilateral.
            Vector2 ul = new Vector2(0, 0), ur = ul, ll = ul, lr = ul;
            GetMinMaxCorners(points, ref ul, ref ur, ref ll, ref lr);

            // Get the coordinates of a box that lies inside this quadrilateral.
            var xmin = ul.X;
            var ymin = ul.Y;

            var xmax = ur.X;
            if (ymin < ur.Y)
            {
                ymin = ur.Y;
            }

            if (xmax > lr.X)
            {
                xmax = lr.X;
            }
            var ymax = lr.Y;

            if (xmin < ll.X)
            {
                xmin = ll.X;
            }
            if (ymax > ll.Y)
            {
                ymax = ll.Y;
            }

            var result = new RectangleF(xmin, ymin, xmax - xmin, ymax - ymin);
            g_MinMaxBox = result; // For debugging.
            return result;
        }
        private static void GetMinMaxCorners(List<Vector2> points,
            ref Vector2 ul,
            ref Vector2 ur,
            ref Vector2 ll,
            ref Vector2 lr)
        {
            // Start with the first point as the solution.
            ul = points[0];
            ur = ul;
            ll = ul;
            lr = ul;

            // Search the other points.
            foreach (var pt in points)
            {
                if (-pt.X - pt.Y > -ul.X - ul.Y)
                {
                    ul = pt;
                }
                if (pt.X - pt.Y > ur.X - ur.Y)
                {
                    ur = pt;
                }
                if (-pt.X + pt.Y > -ll.X + ll.Y)
                {
                    ll = pt;
                }
                if (pt.X + pt.Y > lr.X + lr.Y)
                {
                    lr = pt;
                }
            }

            g_MinMaxCorners = new[] { ul, ur, lr, ll }; // For debugging.
        }

        private static void FindCircle(Vector2 a, Vector2 b, Vector2 c, out Vector2 center, out float radius2)
        {
            // Get the perpendicular bisector of (x1, y1) and (x2, y2).
            var x1 = (b.X + a.X) / 2;
            var y1 = (b.Y + a.Y) / 2;
            var dy1 = b.X - a.X;
            var dx1 = -(b.Y - a.Y);

            // Get the perpendicular bisector of (x2, y2) and (x3, y3).
            var x2 = (c.X + b.X) / 2;
            var y2 = (c.Y + b.Y) / 2;
            var dy2 = c.X - b.X;
            var dx2 = -(c.Y - b.Y);

            // See where the lines intersect.
            var cx = (y1 * dx1 * dx2 + x2 * dx1 * dy2 - x1 * dy1 * dx2 - y2 * dx1 * dx2) / (dx1 * dy2 - dy1 * dx2);
            var cy = (cx - x1) * dy1 / dx1 + y1;
            center = new Vector2(cx, cy);

            var dx = cx - a.X;
            var dy = cy - a.Y;
            radius2 = dx * dx + dy * dy;
        }
        public static class Cone
        {
            internal static int GetHits(Vector2 end, double range, float angle, List<Vector2> points)
            {
                return (from point in points
                        let edge1 = end.Rotated(-angle / 2)
                        let edge2 = edge1.Rotated(angle)
                        where
                            point.Distance(new Vector2(), true) < range * range && edge1.CrossProduct(point) > 0 &&
                            point.CrossProduct(edge2) > 0
                        select point).Count();
            }

            public static PredictionOutput GetPrediction(PredictionInput input)
            {
                var mainTargetPrediction = Prediction.GetPrediction(input, false, true);
                var posibleTargets = new List<PossibleTarget>
                {
                    new PossibleTarget { Position = mainTargetPrediction.UnitPosition.To2D(), Unit = input.Unit }
                };

                if (mainTargetPrediction.Hitchance >= HitChance.Medium)
                {
                    //Add the posible targets  in range:
                    posibleTargets.AddRange(GetPossibleTargets(input));
                }

                if (posibleTargets.Count > 1)
                {
                    var candidates = new List<Vector2>();

                    foreach (var target in posibleTargets)
                    {
                        target.Position = target.Position - input.From.To2D();
                    }

                    for (var i = 0; i < posibleTargets.Count; i++)
                    {
                        for (var j = 0; j < posibleTargets.Count; j++)
                        {
                            if (i != j)
                            {
                                var p = (posibleTargets[i].Position + posibleTargets[j].Position) * 0.5f;
                                if (!candidates.Contains(p))
                                {
                                    candidates.Add(p);
                                }
                            }
                        }
                    }

                    var bestCandidateHits = -1;
                    var bestCandidate = new Vector2();
                    var positionsList = posibleTargets.Select(t => t.Position).ToList();

                    foreach (var candidate in candidates)
                    {
                        var hits = GetHits(candidate, input.Range, input.Radius, positionsList);
                        if (hits > bestCandidateHits)
                        {
                            bestCandidate = candidate;
                            bestCandidateHits = hits;
                        }
                    }

                    bestCandidate = bestCandidate + input.From.To2D();

                    if (bestCandidateHits > 1 && input.From.To2D().Distance(bestCandidate, true) > 50 * 50)
                    {
                        return new PredictionOutput
                        {
                            Hitchance = mainTargetPrediction.Hitchance,
                            _aoeTargetsHitCount = bestCandidateHits,
                            UnitPosition = mainTargetPrediction.UnitPosition,
                            CastPosition = bestCandidate.To3D(),
                            Input = input
                        };
                    }
                }
                return mainTargetPrediction;
            }
        }

        public static class Line
        {
            internal static IEnumerable<Vector2> GetHits(Vector2 start, Vector2 end, double radius, List<Vector2> points)
            {
                return points.Where(p => p.Distance(start, end, true, true) <= radius * radius);
            }

            internal static Vector2[] GetCandidates(Vector2 from, Vector2 to, float radius, float range)
            {
                var middlePoint = (from + to) / 2;
                var intersections = CircleCircleIntersection(
                    from, middlePoint, radius, from.Distance(middlePoint));

                if (intersections.Length > 1)
                {
                    var c1 = intersections[0];
                    var c2 = intersections[1];

                    c1 = from + range * (to - c1).Normalized();
                    c2 = from + range * (to - c2).Normalized();

                    return new[] { c1, c2 };
                }

                return new Vector2[] { };
            }
            public static Vector2[] CircleCircleIntersection(Vector2 center1, Vector2 center2, float radius1, float radius2)
            {
                var D = center1.Distance(center2);
                //The Circles dont intersect:
                if (D > radius1 + radius2 || (D <= Math.Abs(radius1 - radius2)))
                {
                    return new Vector2[] { };
                }

                var A = (radius1 * radius1 - radius2 * radius2 + D * D) / (2 * D);
                var H = (float)Math.Sqrt(radius1 * radius1 - A * A);
                var Direction = (center2 - center1).Normalized();
                var PA = center1 + A * Direction;
                var S1 = PA + H * Direction.Perpendicular();
                var S2 = PA - H * Direction.Perpendicular();
                return new[] { S1, S2 };
            }
            public static PredictionOutput GetPrediction(PredictionInput input)
            {
                var mainTargetPrediction = Prediction.GetPrediction(input, false, true);
                var posibleTargets = new List<PossibleTarget>
                {
                    new PossibleTarget { Position = mainTargetPrediction.UnitPosition.To2D(), Unit = input.Unit }
                };
                if (mainTargetPrediction.Hitchance >= HitChance.Medium)
                {
                    //Add the posible targets  in range:
                    posibleTargets.AddRange(GetPossibleTargets(input));
                }

                if (posibleTargets.Count > 1)
                {
                    var candidates = new List<Vector2>();
                    foreach (var target in posibleTargets)
                    {
                        var targetCandidates = GetCandidates(
                            input.From.To2D(), target.Position, (input.Radius), input.Range);
                        candidates.AddRange(targetCandidates);
                    }

                    var bestCandidateHits = -1;
                    var bestCandidate = new Vector2();
                    var bestCandidateHitPoints = new List<Vector2>();
                    var positionsList = posibleTargets.Select(t => t.Position).ToList();

                    foreach (var candidate in candidates)
                    {
                        if (
                            GetHits(
                                input.From.To2D(), candidate, (input.Radius + input.Unit.BoundingRadius / 3 - 10),
                                new List<Vector2> { posibleTargets[0].Position }).Count() == 1)
                        {
                            var hits = GetHits(input.From.To2D(), candidate, input.Radius, positionsList).ToList();
                            var hitsCount = hits.Count;
                            if (hitsCount >= bestCandidateHits)
                            {
                                bestCandidateHits = hitsCount;
                                bestCandidate = candidate;
                                bestCandidateHitPoints = hits.ToList();
                            }
                        }
                    }

                    if (bestCandidateHits > 1)
                    {
                        float maxDistance = -1;
                        Vector2 p1 = new Vector2(), p2 = new Vector2();

                        //Center the position
                        for (var i = 0; i < bestCandidateHitPoints.Count; i++)
                        {
                            for (var j = 0; j < bestCandidateHitPoints.Count; j++)
                            {
                                var startP = input.From.To2D();
                                var endP = bestCandidate;
                                var proj1 = positionsList[i].ProjectOn(startP, endP);
                                var proj2 = positionsList[j].ProjectOn(startP, endP);
                                var dist = Vector2.DistanceSquared(bestCandidateHitPoints[i], proj1.LinePoint) +
                                           Vector2.DistanceSquared(bestCandidateHitPoints[j], proj2.LinePoint);
                                if (dist >= maxDistance &&
                                    (proj1.LinePoint - positionsList[i]).AngleBetween(
                                        proj2.LinePoint - positionsList[j]) > 90)
                                {
                                    maxDistance = dist;
                                    p1 = positionsList[i];
                                    p2 = positionsList[j];
                                }
                            }
                        }

                        return new PredictionOutput
                        {
                            Hitchance = mainTargetPrediction.Hitchance,
                            _aoeTargetsHitCount = bestCandidateHits,
                            UnitPosition = mainTargetPrediction.UnitPosition,
                            CastPosition = ((p1 + p2) * 0.5f).To3D(),
                            Input = input
                        };
                    }
                }

                return mainTargetPrediction;
            }
        }

        internal class PossibleTarget
        {
            public Vector2 Position;
            public Obj_AI_Base Unit;
        }
    }
    public enum MinionTypes
    {
        /// <summary>
        /// Ranged minions.
        /// </summary>
        Ranged,

        /// <summary>
        /// Melee minions.
        /// </summary>
        Melee,

        /// <summary>
        /// Any minion
        /// </summary>
        All,

        /// <summary>
        /// Any wards. (TODO)
        /// </summary>
        [Obsolete("Wards have not been implemented yet in the minion manager.")]
        Wards
    }
    public enum MinionTeam
    {
        /// <summary>
        /// The minion is not on either team.
        /// </summary>
        Neutral,

        /// <summary>
        /// The minions is an ally
        /// </summary>
        Ally,

        /// <summary>
        /// The minions is an enemy
        /// </summary>
        Enemy,

        /// <summary>
        /// The minion is not an ally
        /// </summary>
        NotAlly,

        /// <summary>
        /// The minions is not an ally for the enemy
        /// </summary>
        NotAllyForEnemy,

        /// <summary>
        /// Any minion.
        /// </summary>
        All
    }
    public enum MinionOrderTypes
    {
        /// <summary>
        /// No order.
        /// </summary>
        None,

        /// <summary>
        /// Ordered by the current health of the minion. (Least to greatest)
        /// </summary>
        Health,

        /// <summary>
        /// Ordered by the maximum health of the minions. (Greatest to least)
        /// </summary>
        MaxHealth
    }
    public static class Collision
    {
        static Collision()
        {

        }

        /// <summary>
        ///     Returns the list of the units that the skillshot will hit before reaching the set positions.
        /// </summary>
        /// 
        private static bool MinionIsDead(PredictionInput input, Obj_AI_Base minion, float distance)
        {
            float delay = (distance / input.Speed) + input.Delay;

            if (Math.Abs(input.Speed - float.MaxValue) < float.Epsilon)
                delay = input.Delay;

            int convert = (int)(delay * 1000);

            if (LaneClearHealthPrediction(minion, convert, 0) <= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        internal class PredictedDamage
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
            public float Damage { get; }

            /// <summary>
            ///     Gets or sets the delay.
            /// </summary>
            /// <value>
            ///     The delay.
            /// </value>
            public float Delay { get; }

            /// <summary>
            ///     Gets or sets the projectile speed.
            /// </summary>
            /// <value>
            ///     The projectile speed.
            /// </value>
            public int ProjectileSpeed { get; }

            /// <summary>
            ///     Gets or sets the source.
            /// </summary>
            /// <value>
            ///     The source.
            /// </value>
            public Obj_AI_Base Source { get; }

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
            public Obj_AI_Base Target { get; }

            /// <summary>
            ///     Gets or sets a value indicating whether this <see cref="PredictedDamage" /> is processed.
            /// </summary>
            /// <value>
            ///     <c>true</c> if processed; otherwise, <c>false</c>.
            /// </value>
            public bool Processed { get; internal set; }
        }

        public static List<Obj_AI_Base> GetMinions(Vector3 from,
                     float range,
                     MinionTypes type = MinionTypes.All,
                     MinionTeam team = MinionTeam.Enemy,
                     MinionOrderTypes order = MinionOrderTypes.Health)
        {
            var result = (from minion in ObjectManager.Get<Obj_AI_Minion>()
                          where minion.IsValidTarget(range, false, @from)
                          let minionTeam = minion.Team
                          where
                              team == MinionTeam.Neutral && minionTeam == GameObjectTeam.Neutral ||
                              team == MinionTeam.Ally &&
                              minionTeam ==
                              (ObjectManager.Player.Team == GameObjectTeam.Chaos ? GameObjectTeam.Chaos : GameObjectTeam.Order) ||
                              team == MinionTeam.Enemy &&
                              minionTeam ==
                              (ObjectManager.Player.Team == GameObjectTeam.Chaos ? GameObjectTeam.Order : GameObjectTeam.Chaos) ||
                              team == MinionTeam.NotAlly && minionTeam != ObjectManager.Player.Team ||
                              team == MinionTeam.NotAllyForEnemy &&
                              (minionTeam == ObjectManager.Player.Team || minionTeam == GameObjectTeam.Neutral) ||
                              team == MinionTeam.All
                          where
                              !minion.IsRanged && type == MinionTypes.Melee || minion.IsRanged && type == MinionTypes.Ranged ||
                              type == MinionTypes.All
                          where IsMinion(minion) || minionTeam == GameObjectTeam.Neutral && minion.MaxHealth > 5 && minion.IsHPBarRendered
                          select minion).Cast<Obj_AI_Base>().ToList();

            switch (order)
            {
                case MinionOrderTypes.Health:
                    result = result.OrderBy(o => o.Health).ToList();
                    break;
                case MinionOrderTypes.MaxHealth:
                    result = result.OrderByDescending(o => o.MaxHealth).ToList();
                    break;
            }

            return result;
        }
        public static bool IsMinion(Obj_AI_Minion minion, bool includeWards = false)
        {
            return minion.Name.Contains("Minion") || includeWards && Extensions.IsWard(minion);
        }
        private static readonly Dictionary<int, PredictedDamage> ActiveAttacks = new Dictionary<int, PredictedDamage>();
        public static float LaneClearHealthPrediction(Obj_AI_Base unit, int time, int delay = 70)
        {
            var predictedDamage = 0f;

            foreach (var attack in ActiveAttacks.Values)
            {
                var n = 0;
                if (GameTimeTickCount - 100 <= attack.StartTick + attack.AnimationTime &&
                    attack.Target.IsValidTarget(float.MaxValue, false) &&
                    attack.Source.IsValidTarget(float.MaxValue, false) && attack.Target.NetworkId == unit.NetworkId)
                {
                    var fromT = attack.StartTick;
                    var toT = GameTimeTickCount + time;

                    while (fromT < toT)
                    {
                        if (fromT >= GameTimeTickCount &&
                            (fromT + attack.Delay + Math.Max(0, unit.Distance(attack.Source) - attack.Source.BoundingRadius) / attack.ProjectileSpeed < toT))
                        {
                            n++;
                        }
                        fromT += (int)attack.AnimationTime;
                    }
                }
                predictedDamage += n * attack.Damage;
            }

            return unit.Health - predictedDamage;
        }
        public static int GameTimeTickCount
        {
            get
            {
                return (int)(Game.Time * 1000);
            }
        }
        public static bool GetCollision(List<Vector3> positions, PredictionInput input)
        {

            foreach (var position in positions)
            {
                foreach (var objectType in input.CollisionObjects)
                {
                    switch (objectType)
                    {
                        case CollisionableObjects.Minions:
                            foreach (var minion in GetMinions2(input.From, Math.Min(input.Range + input.Radius + 100, 2000)))
                            {
                                input.Unit = minion;

                                var distanceFromToUnit = minion.ServerPosition.Distance(input.From);

                                if (distanceFromToUnit < input.Radius + minion.BoundingRadius)
                                {
                                    if (MinionIsDead(input, minion, distanceFromToUnit))
                                        continue;
                                    else
                                        return true;
                                }
                                else if (minion.ServerPosition.Distance(position) < input.Radius + minion.BoundingRadius)
                                {
                                    if (MinionIsDead(input, minion, distanceFromToUnit))
                                        continue;
                                    else
                                        return true;
                                }
                                else
                                {
                                    var minionPos = minion.ServerPosition;
                                    int bonusRadius = 20;
                                    if (minion.IsMoving)
                                    {
                                        minionPos = Prediction.GetPrediction(input, false, false).CastPosition;
                                        bonusRadius = 60 + (int)input.Radius;
                                    }

                                    if (minionPos.To2D().Distance(input.From.To2D(), position.To2D(), true, true) <= Math.Pow((input.Radius + bonusRadius + minion.BoundingRadius), 2))
                                    {
                                        if (MinionIsDead(input, minion, distanceFromToUnit))
                                            continue;
                                        else
                                            return true;
                                    }
                                }
                            }
                            break;
                        case CollisionableObjects.Heroes:
                            foreach (var hero in
                                EntityManager.Heroes.Enemies.FindAll(
                                    hero =>
                                        hero.IsValidTarget(
                                            Math.Min(input.Range + input.Radius + 100, 2000), true, input.RangeCheckFrom))
                                )
                            {
                                input.Unit = hero;
                                var prediction = Prediction.GetPrediction(input, false, false);
                                if (
                                    prediction.UnitPosition.To2D()
                                        .Distance(input.From.To2D(), position.To2D(), true, true) <=
                                    Math.Pow((input.Radius + 50 + hero.BoundingRadius), 2))
                                {
                                    return true;
                                }
                            }
                            break;

                        case CollisionableObjects.Walls:
                            var step = position.Distance(input.From) / 20;
                            for (var i = 0; i < 20; i++)
                            {
                                var p = input.From.To2D().Extend(position.To2D(), step * i);
                                if (NavMesh.GetCollisionFlags(p.X, p.Y).HasFlag(CollisionFlags.Wall))
                                {
                                    return true;
                                }
                            }
                            break;
                    }
                }
            }
            return false;
        }
        public static void Obj_AI_Base_OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (ActiveAttacks.ContainsKey(sender.NetworkId) && sender.IsMelee)
            {
                ActiveAttacks[sender.NetworkId].Processed = true;
            }
        }
        public static List<Obj_AI_Base> AllMinionsObj = new List<Obj_AI_Base>();
        public static List<Obj_AI_Base> MinionsListEnemy = new List<Obj_AI_Base>();
        public static List<Obj_AI_Base> MinionsListAlly = new List<Obj_AI_Base>();
        public static List<Obj_AI_Base> MinionsListNeutral = new List<Obj_AI_Base>();
        public static List<Obj_AI_Base> GetMinions2(Vector3 from, float range = float.MaxValue, MinionTeam team = MinionTeam.Enemy)
        {
            if (team == MinionTeam.Enemy)
            {

                return MinionsListEnemy.FindAll(minion => CanReturn(minion, from, range));
            }
            else if (team == MinionTeam.Ally)
            {

                return MinionsListAlly.FindAll(minion => CanReturn(minion, from, range));
            }
            else if (team == MinionTeam.Neutral)
            {

                return MinionsListNeutral.Where(minion => CanReturn(minion, from, range)).OrderByDescending(minion => minion.MaxHealth).ToList();
            }
            else
            {
                return AllMinionsObj.FindAll(minion => CanReturn(minion, from, range));
            }
        }
        private static bool CanReturn(Obj_AI_Base minion, Vector3 from, float range)
        {

            if (minion != null && minion.IsValid && !minion.IsDead && minion.IsVisible && minion.IsTargetable)
            {
                if (range == float.MaxValue)
                    return true;
                else if (range == 0)
                {
                    if (minion.IsInRange(ObjectManager.Player, ObjectManager.Player.AttackRange) && ObjectManager.Player.IsMe)
                        return true;
                    else
                        return false;
                }
                else if (Vector2.DistanceSquared((@from).To2D(), minion.Position.To2D()) < range * range)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }
    }
}


internal class PathInfo
{
    public Vector2 Position { get; set; }
    public float Time { get; set; }
}

internal class Spells
{
    public string name { get; set; }
    public double duration { get; set; }
}

internal class UnitTrackerInfo
{
    public int NetworkId { get; set; }
    public int AaTick { get; set; }
    public int NewPathTick { get; set; }
    public int StopMoveTick { get; set; }
    public int LastInvisableTick { get; set; }
    public int SpecialSpellFinishTick { get; set; }
    public List<PathInfo> PathBank = new List<PathInfo>();
}

internal static class UnitTracker
{
    public static List<UnitTrackerInfo> UnitTrackerInfoList = new List<UnitTrackerInfo>();
    private static List<AIHeroClient> Champion = new List<AIHeroClient>();
    private static List<Spells> spells = new List<Spells>();
    private static List<PathInfo> PathBank = new List<PathInfo>();
    static UnitTracker()
    {
        spells.Add(new Spells() { name = "katarinar", duration = 1 }); //Katarinas R
        spells.Add(new Spells() { name = "drain", duration = 1 }); //Fiddle W
        spells.Add(new Spells() { name = "crowstorm", duration = 1 }); //Fiddle R
        spells.Add(new Spells() { name = "consume", duration = 0.5 }); //Nunu Q
        spells.Add(new Spells() { name = "absolutezero", duration = 1 }); //Nunu R
        spells.Add(new Spells() { name = "staticfield", duration = 0.5 }); //Blitzcrank R
        spells.Add(new Spells() { name = "cassiopeiapetrifyinggaze", duration = 0.5 }); //Cassio's R
        spells.Add(new Spells() { name = "ezrealtrueshotbarrage", duration = 1 }); //Ezreal's R
        spells.Add(new Spells() { name = "galioidolofdurand", duration = 1 }); //Ezreal's R                                                                   
        spells.Add(new Spells() { name = "luxmalicecannon", duration = 1 }); //Lux R
        spells.Add(new Spells() { name = "reapthewhirlwind", duration = 1 }); //Jannas R
        spells.Add(new Spells() { name = "jinxw", duration = 0.6 }); //jinxW
        spells.Add(new Spells() { name = "jinxr", duration = 0.6 }); //jinxR
        spells.Add(new Spells() { name = "missfortunebullettime", duration = 1 }); //MissFortuneR
        spells.Add(new Spells() { name = "shenstandunited", duration = 1 }); //ShenR
        spells.Add(new Spells() { name = "threshe", duration = 0.4 }); //ThreshE
        spells.Add(new Spells() { name = "threshrpenta", duration = 0.75 }); //ThreshR
        spells.Add(new Spells() { name = "threshq", duration = 0.75 }); //ThreshQ
        spells.Add(new Spells() { name = "infiniteduress", duration = 1 }); //Warwick R
        spells.Add(new Spells() { name = "meditate", duration = 1 }); //yi W
        spells.Add(new Spells() { name = "alzaharnethergrasp", duration = 1 }); //Malza R
        spells.Add(new Spells() { name = "lucianq", duration = 0.5 }); //Lucian Q
        spells.Add(new Spells() { name = "caitlynpiltoverpeacemaker", duration = 0.5 }); //Caitlyn Q
        spells.Add(new Spells() { name = "velkozr", duration = 0.5 }); //Velkoz R 
        spells.Add(new Spells() { name = "jhinr", duration = 2 }); //Velkoz R 

        foreach (var hero in ObjectManager.Get<AIHeroClient>())
        {
            Champion.Add(hero);
            UnitTrackerInfoList.Add(new UnitTrackerInfo() { NetworkId = hero.NetworkId, AaTick = TickCount, StopMoveTick = TickCount, NewPathTick = TickCount, SpecialSpellFinishTick = TickCount, LastInvisableTick = TickCount });
        }

        Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        Obj_AI_Base.OnNewPath += Obj_AI_Hero_OnNewPath;
        Obj_AI_Base.OnProcessSpellCast += MaddeningJinx.Collision.Obj_AI_Base_OnDoCast;
        Obj_AI_Base.OnUpdatePosition += EnterLocalVisiblityClient;
    }

    private static void EnterLocalVisiblityClient(AttackableUnit sender, EventArgs args)
    {
        if (sender is AIHeroClient && sender.IsVisible)
            UnitTrackerInfoList.Find(x => x.NetworkId == sender.NetworkId).LastInvisableTick = TickCount;
    }
    private static void Obj_AI_Hero_OnNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
    {
        if (sender is AIHeroClient)
        {
            if (args.Path.Count() == 1) // STOP MOVE DETECTION
                UnitTrackerInfoList.Find(x => x.NetworkId == sender.NetworkId).StopMoveTick = TickCount;
            else
            {
                UnitTrackerInfoList.Find(x => x.NetworkId == sender.NetworkId).NewPathTick = TickCount;
                UnitTrackerInfoList.Find(x => x.NetworkId == sender.NetworkId).PathBank.Add(new PathInfo() { Position = args.Path.Last().To2D(), Time = TickCount });

            }

            if (UnitTrackerInfoList.Find(x => x.NetworkId == sender.NetworkId).PathBank.Count > 3)
                UnitTrackerInfoList.Find(x => x.NetworkId == sender.NetworkId).PathBank.Remove(UnitTrackerInfoList.Find(x => x.NetworkId == sender.NetworkId).PathBank.First());
        }
    }
    private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
    {
        if (sender is AIHeroClient)
        {
            if (EloBuddy.SDK.Constants.AutoAttacks.IsAutoAttack(args.SData))
                UnitTrackerInfoList.Find(x => x.NetworkId == sender.NetworkId).AaTick = TickCount;
            else
            {
                var foundSpell = spells.Find(x => args.SData.Name.ToLower() == x.name.ToLower());
                if (foundSpell != null)
                {
                    UnitTrackerInfoList.Find(x => x.NetworkId == sender.NetworkId).SpecialSpellFinishTick = TickCount + (int)(foundSpell.duration * 1000);
                }
            }
        }
    }
    public static bool SpamSamePlace(Obj_AI_Base unit)
    {
        var TrackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);
        if (TrackerUnit.PathBank.Count < 3)
            return false;

        if (TrackerUnit.PathBank[2].Time - TrackerUnit.PathBank[1].Time < 200 && TickCount - TrackerUnit.PathBank[2].Time < 100)
        {
            var C = TrackerUnit.PathBank[1].Position;
            var A = TrackerUnit.PathBank[2].Position;

            var B = unit.Position.To2D();

            var AB = Math.Pow(A.X - B.X, 2) + Math.Pow(A.Y - B.Y, 2);
            var BC = Math.Pow(B.X - C.X, 2) + Math.Pow(B.Y - C.Y, 2);
            var AC = Math.Pow(A.X - C.X, 2) + Math.Pow(A.Y - C.Y, 2);

            if (TrackerUnit.PathBank[1].Position.Distance(TrackerUnit.PathBank[2].Position) < 150)
                return true;
            else if (Math.Cos((AB + BC - AC) / (2 * Math.Sqrt(AB) * Math.Sqrt(BC))) * 180 / Math.PI < 31)
                return true;
            else
                return false;
        }
        else
            return false;
    }

    public static List<Vector2> GetPathWayCalc(Obj_AI_Base unit)
    {
        var TrackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);
        List<Vector2> points = new List<Vector2>();
        points.Add(unit.ServerPosition.To2D());
        return points;
    }
    public static double GetSpecialSpellEndTime(Obj_AI_Base unit)
    {
        var TrackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);
        return TrackerUnit.SpecialSpellFinishTick - TickCount;
    }

    public static double GetLastAutoAttackTime(Obj_AI_Base unit)
    {
        var TrackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);
        return TickCount - TrackerUnit.AaTick;
    }

    public static double GetLastNewPathTime(Obj_AI_Base unit)
    {
        var TrackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);
        return TickCount - TrackerUnit.NewPathTick;
    }

    public static double GetLastVisableTime(Obj_AI_Base unit)
    {
        var TrackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);

        return TickCount - TrackerUnit.LastInvisableTick;
    }

    public static double GetLastStopMoveTime(Obj_AI_Base unit)
    {
        var TrackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);

        return TickCount - TrackerUnit.StopMoveTick;
    }
    public static int TickCount
    {
        get
        {
            return Environment.TickCount & int.MaxValue;
        }
    }
}