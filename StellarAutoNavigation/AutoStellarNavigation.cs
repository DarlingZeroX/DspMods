using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BepInEx;
using BepInEx.Configuration;

namespace AutoNavigate
{
    public class AutoStellarNavigation
    {
        private const int THRUSTER_LEVEL_FLY = 1;
        private const int THRUSTER_LEVEL_SAIL = 2;
        private const int THRUSTER_LEVEL_WARP = 3;

        public enum NavigateType
        {
            Null,
            Star,
            Planet
        }

        public class Target
        {
            public static double s_FocusParam = 0.01;
            private PlanetData m_PlanetData = null;
            private StarData m_StarData = null;

            public bool IsVaild() => (TargetStar != null) || (TargetPlanet != null);

            public void Reset()
            {
                m_PlanetData = null;
                m_StarData = null;
            }

            public void SetTarget(StarData star)
            {
                Reset();
                m_StarData = star;
            }

            public void SetTarget(PlanetData planet)
            {
                Reset();
                m_PlanetData = planet;
            }

            public PlanetData TargetPlanet => m_PlanetData;

            public StarData TargetStar => m_StarData;

            public static bool IsFocusing(VectorLF3 lineL, VectorLF3 lineR)
            {
                return IsFocusingNormalized(lineL.normalized, lineR.normalized);
            }

            public static bool IsFocusingNormalized(VectorLF3 dirL, VectorLF3 dirR)
            {
                return (dirL - dirR).magnitude < s_FocusParam;
            }

            public VectorLF3 Position
            {
                get
                {
                    if (TargetPlanet != null)
                    {
                        return TargetPlanet.uPosition;
                    }
                    else if (TargetStar != null)
                    {
                        return TargetStar.uPosition;
                    }
                    else
                    {
                        ModDebug.Error("Get Target Position while no target!!!");
                        return new VectorLF3(0.0, 0.0, 0.0);
                    }
                }
            }

            public double GetDistance(Player __instance)
            {
                double dist = float.MaxValue;

                if (TargetPlanet != null)
                {
                    dist = (TargetPlanet.uPosition - __instance.uPosition).magnitude;
                }
                else if (TargetStar != null)
                {
                    dist = (TargetStar.uPosition - __instance.uPosition).magnitude;
                }
                else
                {
                    ModDebug.Error("GetDistance while no target!!!");
                }

                return dist;
            }

            public VectorLF3 GetDirection(Player __instance)
            {
                VectorLF3 dir = VectorLF3.zero;

                if (TargetPlanet != null)
                {
                    dir = (TargetPlanet.uPosition - __instance.uPosition).normalized;
                }
                else if (TargetStar != null)
                {
                    dir = (TargetStar.uPosition - __instance.uPosition).normalized;
                }
                else
                {
                    ModDebug.Error("GetDirection while no target!!!");
                }

                return dir;
            }

            public bool IsLocalStarPlanet
            {
                get
                {
                    if (GameMain.localStar != null && TargetPlanet != null && TargetPlanet.star == GameMain.localStar)
                        return true;
                    else
                        return false;
                }
            }
        }

        //Class Config
        public class NavigationConfig
        {
            public ConfigEntry<double> speedUpEnergylimit;
            public ConfigEntry<double> wrapEnergylimit;
            public double planetNearastDistance;
            public int sparseStarPlanetCount;
            public double sparseStarPlanetNearastDistance;
            public double focusParam;
            public double longNavUncoverRange;
            public double shortNavUncoverRange;
            public ConfigEntry<bool> enableLocalWrap;
            public ConfigEntry<double> localWrapMinDistance;
        }

        public NavigateType CurNavigateType
        {
            get
            {
                if (IsCurNavStar)
                    return NavigateType.Star;
                else if (IsCurNavPlanet)
                    return NavigateType.Planet;
                else
                    return NavigateType.Null;
            }
        }

        public bool IsCurNavPlanet => target.TargetPlanet != null;

        public bool IsCurNavStar => target.TargetStar != null;

        public Target target;
        public Text modeText;
        private Player player;

        // State
        public bool isHistoryNav = false;

        //private bool __enable;

        public bool enable
        {
            get;
            private set;
        }

        public bool Pause()
        {
            if (enable)
            {
                ModDebug.Log("SafePause");
                enable = false;
                isHistoryNav = true;
                return true;
            }
            return false;
        }

        public bool Resume()
        {
            if (isHistoryNav == true && !enable)
            {
                ModDebug.Log("SafeResume");
                enable = true;
                isHistoryNav = false;
                return true;
            }
            isHistoryNav = false;
            return false;
        }

        public bool sailSpeedUp;

        //Instance Config
        private NavigationConfig config;

        private bool useConfigFile = true;
        private double speedUpEnergylimit;
        private double wrapEnergylimit;
        private double planetNearastDistance;
        private int sparseStarPlanetCount;
        private double sparseStarPlanetNearastDistance;
        private double longNavUncoverRange;
        private double shortNavUncoverRange;
        private bool enableLocalWrap;
        private double localWrapMinDistance;

        public AutoStellarNavigation(NavigationConfig config)
        {
            this.config = config;

            target = new Target();
            Reset();
            target.Reset();
        }

        public bool Navigate()
        {
            if (enable == true)
                return enable;

            if (CurNavigateType == NavigateType.Null)
            {
                Arrive();
                enable = false;
            }
            else if (DetermineArrive())
            {
                Arrive();
                enable = false;
            }
            else
            {
                enable = true;
            }

            return enable;
        }

        public bool ToggleNavigate()
        {
            if (enable == false)
                return Navigate();
            else
                return Arrive();
        }

        public void Reset()
        {
            isHistoryNav = false;
            player = null;
            enable = false;
            sailSpeedUp = false;

            if (useConfigFile)
            {
                speedUpEnergylimit = config.speedUpEnergylimit.Value > 0 ? config.speedUpEnergylimit.Value : 0;
                wrapEnergylimit = config.wrapEnergylimit.Value > 0 ? config.wrapEnergylimit.Value : 0;
                planetNearastDistance = 60000.0;
                sparseStarPlanetCount = 2;
                sparseStarPlanetNearastDistance = 200000.0;
                Target.s_FocusParam = 0.02;
                longNavUncoverRange = 1000.0;
                shortNavUncoverRange = 100.0;
                enableLocalWrap = config.enableLocalWrap.Value;
                localWrapMinDistance = config.localWrapMinDistance.Value > 0 ? config.localWrapMinDistance.Value : 0.0;
            }
            else
            {
                speedUpEnergylimit = 50000000.0;
                wrapEnergylimit = 1000000000;
                planetNearastDistance = 60000.0;
                sparseStarPlanetCount = 2;
                sparseStarPlanetNearastDistance = 200000.0;
                Target.s_FocusParam = 0.02;
                longNavUncoverRange = 1000.0;
                shortNavUncoverRange = 100.0;
                enableLocalWrap = false;
                localWrapMinDistance = 0.0;
            }
        }

        public void HandlePlayerInput()
        {
            if (!enable)
                return;

            if (VFInput._moveForward.onDown ||
                VFInput._moveBackward.onDown ||
                VFInput._moveLeft.onDown ||
                VFInput._moveRight.onDown)
            {
                Arrive();
            }
        }

        public bool Arrive(string extraTip = null)
        {
            string tip = "导航模式结束".ModText();

            if (extraTip != null)
                tip += ("-" + extraTip);

            Reset();
            UIRealtimeTip.Popup(tip);
            if (modeText != null && modeText.IsActive())
            {
                modeText.gameObject.SetActive(false);
                modeText.text = string.Empty;
            }

            return true;
        }

        public bool DetermineArrive()
        {
            if (IsCurNavPlanet &&
                GameMain.localPlanet != null &&
                GameMain.localPlanet.id == target.TargetPlanet.id)
            {
                return true;
            }
            else if (IsCurNavStar &&
                GameMain.localStar != null &&
                GameMain.localStar.id == target.TargetStar.id)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool AdvisePointIfOcclusion(PlayerMove_Sail __instance, ref VectorLF3 advisePoint, double radiusOffest)
        {
            bool navPlanet = false;
            bool navStar = false;

            StarData localStar = GameMain.localStar;
            VectorLF3 srcPoint = __instance.player.uPosition;
            VectorLF3 dstPoint = VectorLF3.zero;

            if (IsCurNavStar)
            {
                navStar = true;
                dstPoint = target.TargetStar.uPosition;
            }
            else if (IsCurNavPlanet)
            {
                navPlanet = true;
                dstPoint = target.TargetPlanet.uPosition;
            }
            else
            {
                ModDebug.Error("AdvisePointIfOcclusion When No Navigate!!!");
                return false;
            }

            bool hit = false;
            Math.Line3D hitPlaneVertical = new Math.Line3D();
            double uncoverRadius = 0;

            Math.Line3D line = new Math.Line3D(srcPoint, dstPoint);
            Math.Plane3D plane = new Math.Plane3D();
            plane.normal = line.dir;

            if (localStar != null)
            {
                //Planet occlusion
                for (int index = 0; index < localStar.planetCount; ++index)
                {
                    PlanetData planet = localStar.planets[index];
                    plane.ponit = planet.uPosition;

                    if (plane.IsParallel(line) == false)
                    {
                        //Target planet
                        if (navPlanet && planet.id == target.TargetPlanet.id)
                            continue;

                        VectorLF3 intersection = plane.GetIntersection(line);

                        double minHitRange = planet.realRadius + radiusOffest;

                        if (intersection.Distance(planet.uPosition) < minHitRange &&
                            intersection.Distance(srcPoint) + intersection.Distance(dstPoint) <= (dstPoint.Distance(srcPoint) + 0.1))
                        {
                            hit = true;

                            //Maximum radius plane
                            if (minHitRange > uncoverRadius)
                            {
                                uncoverRadius = minHitRange;
                                hitPlaneVertical.src = planet.uPosition;

                                if (planet.uPosition != intersection)
                                    hitPlaneVertical.dst = intersection;
                                //Rare case
                                else
                                    hitPlaneVertical.dst = plane.GetAnyPoint();
                            }
                        }
                    }
                }

                ///Star occlusion
                StarData star = localStar;
                plane.ponit = star.uPosition;

                //Target star
                if (navStar && star.id == target.TargetStar.id)
                { }
                else
                {
                    if (plane.IsParallel(line) == false)
                    {
                        VectorLF3 intersection = plane.GetIntersection(line);

                        double minHitRange = star.physicsRadius + radiusOffest;
                        if (intersection.Distance(star.uPosition) < minHitRange)
                        {
                            hit = true;

                            //Maximum radius plane
                            if (minHitRange > uncoverRadius)
                            {
                                uncoverRadius = minHitRange;
                                hitPlaneVertical.src = star.uPosition;

                                if (star.uPosition != intersection)
                                    hitPlaneVertical.dst = intersection;
                                //Rare case
                                else
                                    hitPlaneVertical.dst = plane.GetAnyPoint();
                            }
                        }
                    }
                }
            }

            if (hit)
            {
#if DEBUG
                ModDebug.Log("AdvisePointIfOcclusion Hit");
#endif
                VectorLF3 uncoverOrbitPoint = hitPlaneVertical.src + (hitPlaneVertical.dir * (uncoverRadius + 10));
                Math.Line3D uncoverLine = new Math.Line3D(dstPoint, uncoverOrbitPoint);
                plane.normal = uncoverLine.dir;
                plane.ponit = srcPoint;

                advisePoint = plane.GetIntersection(uncoverLine);
            }

            return hit;
        }

        /// <summary>
        /// 获取当前星系最近行星距离
        /// </summary>
        private double NearestPlanetDistance(PlayerMove_Sail __instance)
        {
            StarData localStar = GameMain.localStar;
            double distance = (localStar.planets[0].uPosition - __instance.player.uPosition).magnitude;

            //Distance set negative when local star is null
            if (localStar == null)
                return -10.0;

            for (int index = 0; index < localStar.planetCount; ++index)
            {
                PlanetData planet = localStar.planets[index];
                double magnitude = (planet.uPosition - __instance.player.uPosition).magnitude;

                if (magnitude < distance)
                    distance = magnitude;
            }

            return distance;
        }

        public bool IsCloseToNearStar(PlayerMove_Sail __instance)
        {
            if (GameMain.localStar == null)
                return false;

            bool closeFlag = false;
            double distance = NearestPlanetDistance(__instance);

            if (distance > 0)
            {
                //星系行星较少的情况
                if (GameMain.localStar.planetCount <= sparseStarPlanetCount &&
                    distance < sparseStarPlanetNearastDistance)
                    return true;

                if (distance < planetNearastDistance)
                    closeFlag = true;
            }
            //靠近本地星系恒星
            else if ((GameMain.localStar.uPosition - __instance.player.uPosition).magnitude < planetNearastDistance)
                closeFlag = true;

            return closeFlag;
        }

        public void StarNavigation(PlayerMove_Sail __instance)
        {
            ModDebug.Assert(IsCurNavStar);
            player = __instance.player;

            if (!IsCurNavStar)
            {
                Arrive();
#if DEBUG
                ModDebug.Error("StarNavigation - Error target");
#endif
                return;
            }

            // 飞离星球
            PlanetData localPlanet = GameMain.localPlanet;
            if (localPlanet != null)
            {
#if DEBUG
                ModDebug.Log("Leave Local Planet");
#endif
                VectorLF3 dir = (__instance.player.uPosition - localPlanet.uPosition).normalized;
                Sail.SetDir(__instance, dir);
                return;
            }

            // 判断是否抵达目的地
            if (DetermineArrive() && IsCloseToNearStar(__instance))
            {
#if DEBUG
                ModDebug.Log("Star Navigation Arrive");
#endif
                Arrive();
                Warp.TryLeaveWarp(__instance);
                return;
            }

            LongDistanceNavigate(__instance);
        }

        private bool NeedLocalLongDistanceNavigate(PlayerMove_Sail __instance)
        {
            double distance = (target.TargetPlanet.uPosition - __instance.player.uPosition).magnitude;

            bool localWarpable = (enableLocalWrap &&
                                  distance > localWrapMinDistance &&
                                  distance > (target.TargetPlanet.realRadius + longNavUncoverRange));

            return localWarpable || target.IsLocalStarPlanet == false;
        }

        public void PlanetNavigation(PlayerMove_Sail __instance)
        {
            ModDebug.Assert(IsCurNavPlanet);
            player = __instance.player;

            if (!IsCurNavPlanet)
            {
                Arrive();
#if DEBUG
                ModDebug.Error("Planet navigation - Error target");
#endif
                return;
            }

            if (NeedLocalLongDistanceNavigate(__instance))
            {
                PlanetData localPlanet = GameMain.localPlanet;
                if (localPlanet != null &&
                    target.TargetPlanet != null &&
                    localPlanet.id != target.TargetPlanet.id)
                {
#if DEBUG
                    ModDebug.Log("Leave Local Planet");
#endif
                    VectorLF3 dir = (__instance.player.uPosition - localPlanet.uPosition).normalized;
                    Sail.SetDir(__instance, dir);
                }
                else
                {
#if DEBUG
                    ModDebug.Log("Local Long Distance Navigation");
#endif
                    LongDistanceNavigate(__instance);
                }

                return;
            }

#if DEBUG
            ModDebug.Log("Local Short Distance Navigation");
#endif
            ShortDistanceNavigate(__instance);
        }

        private void ShortDistanceNavigate(PlayerMove_Sail __instance)
        {
            VectorLF3 dir = target.GetDirection(__instance.player);
            Sail.SetDir(__instance, dir);

            VectorLF3 advisePoint = VectorLF3.zero;
            if (AdvisePointIfOcclusion(__instance, ref advisePoint, shortNavUncoverRange))
            {
#if DEBUG
                ModDebug.Log("Planet Navigate ToAdvisePoint:" + advisePoint);
#endif
                dir = (advisePoint - __instance.player.uPosition).normalized;
                Sail.SetDir(__instance, dir);
                Sail.TrySpeedUp(this, __instance);
            }
            else
            {
                if (Target.IsFocusingNormalized(dir, __instance.player.uVelocity.normalized))
                {
#if DEBUG
                    ModDebug.Log("Short Navigate - Speed Up");
#endif
                    Sail.TrySpeedUp(this, __instance);
                }
                else
                {
#if DEBUG
                    ModDebug.Log("Short Navigate - No Speed Up");
#endif
                }
            }
        }

        private void LongDistanceNavigate(PlayerMove_Sail __instance)
        {
            VectorLF3 dir = target.GetDirection(__instance.player);
            Sail.SetDir(__instance, dir);

            VectorLF3 advisePoint = VectorLF3.zero;
            if (AdvisePointIfOcclusion(__instance, ref advisePoint, longNavUncoverRange))
            {
#if DEBUG
                ModDebug.Log("LongDistanceNavigate - ToAdvisePoint:" + advisePoint);
#endif

                dir = (advisePoint - __instance.player.uPosition).normalized;
                Sail.SetDir(__instance, dir);
                Sail.TrySpeedUp(this, __instance);
            }
            else if (Target.IsFocusingNormalized(dir, __instance.player.uVelocity.normalized) && !__instance.player.warping)
            {
                if (__instance.player.mecha.coreEnergy >= wrapEnergylimit && Warp.TryWrap(this, __instance))
                {
#if DEBUG
                    ModDebug.Log("Enter Wrap");
#endif
                    return;
                }
                else if (IsCurNavPlanet && target.IsLocalStarPlanet == true)
                {
#if DEBUG
                    ModDebug.Log("Local Planet Navigate No Wrap Chance SpeedUp");
#endif
                    Sail.TrySpeedUp(this, __instance);
                    return;
                }
                else if (LongDistanceNavigateNeedSpeedUp())
                {
#if DEBUG
                    ModDebug.Log("Long Distance Navigate Need SpeedUp");
#endif
                    Sail.TrySpeedUp(this, __instance);
                }
                else
                {
#if DEBUG
                    ModDebug.Log("Long Distance Navigate No SpeedUp And Warp");
#endif
                }
            }

            bool LongDistanceNavigateNeedSpeedUp()
            {
                if (__instance.player.mecha.coreEnergy >= speedUpEnergylimit)
                {
                    if (__instance.player.mecha.thrusterLevel < THRUSTER_LEVEL_WARP)
                        return true;
                    //else if (Warp.GetWarperCount(__instance) <= 0)
                    else if (!Warp.HasWarper(__instance))
                        return true;
                    return true;
                }
                //Prepare warp
                if (__instance.player.mecha.coreEnergy < wrapEnergylimit)
                    return false;
                return false;
            }
        }

        public static class Warp
        {
            public static bool HasWarper(PlayerMove_Sail __instance) =>
                 GetWarperCount(__instance) > 0;

            public static int GetWarperCount(PlayerMove_Sail __instance)
            {
                return __instance.player.mecha.warpStorage.GetItemCount(1210);
            }

            public static bool TryWrap(AutoStellarNavigation self, PlayerMove_Sail __instance)
            {
                if (HasWarpChance(self, __instance))
                {
                    TryEnterWarp(__instance);
                    return true;
                }
                return false;
            }

            public static bool HasWarpChance(AutoStellarNavigation self, PlayerMove_Sail __instance)
            {
                bool LocalPlanetWarp()
                {
                    if (GameMain.localPlanet != null && self.target.TargetPlanet.id == GameMain.localPlanet.id)
                        return false;
                    else if ((__instance.player.uPosition - self.target.TargetPlanet.uPosition).magnitude < (self.localWrapMinDistance))
                        return false;
                    else
                        return true;
                }

                if (__instance.player.mecha.thrusterLevel < THRUSTER_LEVEL_WARP)
                    return false;

                if (__instance.mecha.coreEnergy <
                    __instance.mecha.warpStartPowerPerSpeed * (double)__instance.mecha.maxWarpSpeed)
                    return false;

                //if (GetWarperCount(__instance) <= 0)
                if (!HasWarper(__instance))
                    return false;

                if (self.IsCurNavStar || LocalPlanetWarp())
                    return true;

                return false;
            }

            public static bool TryEnterWarp(PlayerMove_Sail __instance)
            {
                if (!__instance.player.warping && __instance.player.mecha.UseWarper())
                {
                    __instance.player.warpCommand = true;
                    VFAudio.Create("warp-begin", __instance.player.transform, Vector3.zero, true);
                    //GameMain.gameScenario.NotifyOnWarpModeEnter();

                    return true;
                }

                return false;
            }

            public static bool TryLeaveWarp(PlayerMove_Sail __instance)
            {
                if (__instance.player.warping)
                {
                    __instance.player.warpCommand = false;
                    VFAudio.Create("warp-end", __instance.player.transform, Vector3.zero, true);

                    return true;
                }

                return false;
            }
        }

        public static class Sail
        {
            public static void SetDir(PlayerMove_Sail __instance, VectorLF3 dir) =>
                __instance.sailPoser.targetURot = Quaternion.LookRotation(dir);

            public static void TrySpeedUp(AutoStellarNavigation __this, PlayerMove_Sail __instance)
            {
                if (__instance.player.mecha.coreEnergy >= __this.speedUpEnergylimit)
                {
                    __this.sailSpeedUp = true;
                }
            }
        }

        public static class Fly
        {
            public static bool TrySwtichToSail(PlayerMove_Fly __instance)
            {
#if DEBUG
                ModDebug.Log("Try Swtich To Sail");
#endif

                if (__instance.mecha.thrusterLevel < THRUSTER_LEVEL_SAIL)
                    return false;

                //取消建造模式
                if (__instance.controller.cmd.type == ECommand.Build)
                    __instance.controller.cmd.type = ECommand.None;

                __instance.controller.movementStateInFrame = EMovementState.Sail;
                __instance.controller.actionSail.ResetSailState();

                GameCamera.instance.SyncForSailMode();
                GameMain.gameScenario.NotifyOnSailModeEnter();

                return true;
            }
        }

        public static class Walk
        {
            public static bool TrySwitchToFly(PlayerMove_Walk __instance)
            {
#if DEBUG
                ModDebug.Log("Try Switch To Fly");
#endif

                //if (__instance.mecha.thrusterLevel < 1)
                if (__instance.mecha.thrusterLevel < THRUSTER_LEVEL_FLY)
                    return false;

                __instance.jumpCoolTime = 0.3f;
                __instance.jumpedTime = 0.0f;

                __instance.flyUpChance = 0.0f;
                __instance.SwitchToFly();

                return true;
            }
        }
    }
}