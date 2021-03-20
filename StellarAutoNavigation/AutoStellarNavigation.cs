using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BepInEx;
using BepInEx.Configuration;

namespace AutoNavigate
{
    public class AutoStellarNavigation
    {
        public class Target
        {
            static public double focusParam = 0.01;
            private PlanetData planetData = null;
            private StarData starData = null;

            public bool IsVaild() => (TargetStar != null) || (TargetPlanet != null);

            public void Reset()
            {
                planetData = null;
                starData = null;
            }

            public void SetTarget(UIStarmapStar star)
            {
                if(star.star != null)
                {
                    starData = star.star;
                }
            }

            public void SetTarget(StarData star)
            {
                Reset();
                starData = star;
            }

            public void SetTarget(PlanetData planet)
            {
                Reset(); 
                planetData = planet;
            }

            public PlanetData TargetPlanet
            {
                get
                {
                    return planetData;
                }
            }

            public StarData TargetStar
            {
                get
                {
                    return starData;
                }
            }

            public static bool IsFocusing(VectorLF3 lineL, VectorLF3 lineR)
            {
                return IsFocusingNormalized(lineL.normalized, lineR.normalized);
            }

            public static bool IsFocusingNormalized(VectorLF3 dirL, VectorLF3 dirR)
            {
                return (dirL - dirR).magnitude < focusParam;
            }

            public VectorLF3 GetPos()
            {
                if(TargetPlanet != null)
                {
                    return TargetPlanet.uPosition;
                }
                else if(TargetStar != null)
                {
                    return TargetStar.uPosition;
                }
                else
                {
                    ModDebug.Error("GetTartgetPos While No Target!!!");
                    return new VectorLF3(0.0, 0.0, 0.0);
                }
            }

            public VectorLF3 GetDir(AutoStellarNavigation __this, Player __instance)
            {
                VectorLF3 dir = VectorLF3.zero;

                if (__this.IsCurNavStar())
                {
                    dir = (TargetStar.uPosition - __instance.uPosition).normalized;
                }
                else if (__this.IsCurNavPlanet())
                {
                    dir = (TargetPlanet.uPosition - __instance.uPosition).normalized;
                }
                else
                {
                    ModDebug.Error("GetDir While No Target!!!");
                }

                return dir;
            }

            public bool IsLocalStarPlanet()
            {
                if (GameMain.localStar != null && TargetPlanet != null && TargetPlanet.star == GameMain.localStar)
                    return true;
                else
                    return false;
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

        public enum NavType
        {
            Null,
            Star,
            Planet
        }
        public NavType curNavType
        {
            get
            {
                if (IsCurNavStar())
                    return NavType.Star;
                else if (IsCurNavPlanet())
                    return NavType.Planet;
                else
                    return NavType.Null;
            }
        }


        public bool IsCurNavPlanet() => target.TargetPlanet != null;
        public bool IsCurNavStar() =>  target.TargetStar != null;

        public Target target;
        public Text modeText;
        private Player player;

        // State
        public bool isHistoryNav = false;
        private bool __enable;
        public bool enable
        {
            get
            {
                return __enable;
            }
            private set
            {
                __enable = value;
            }
        }
        public bool pause() {
            if (enable)
            {
                ModDebug.Log("SafePause");
                enable = false;
                isHistoryNav = true;
                return true;
            }
            return false;
        }
        public bool resume()
        {
            if(isHistoryNav == true && !enable)
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
            if (enable == false)
            {
                if (curNavType == NavType.Null)
                {
                    Arrive();
                    enable = false;
                }
                else if(DetermineArrive())
                {
                    Arrive();
                    enable = false;
                }
                else
                {
                    enable = true;
                }
            }

            return enable;
        }

        public bool ToggleNav()
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
                Target.focusParam = 0.02;
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
                Target.focusParam = 0.02;
                longNavUncoverRange = 1000.0;
                shortNavUncoverRange = 100.0;
                enableLocalWrap = false;
                localWrapMinDistance = 0.0;
            }       
        }

        public void HandlePlayerInput()
        {

            if (enable &&
                VFInput._moveForward ||
                VFInput._moveBackward ||
                VFInput._moveLeft ||
                VFInput._moveRight)
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
            if (IsCurNavPlanet() && 
                GameMain.localPlanet != null && 
                GameMain.localPlanet.id == target.TargetPlanet.id)
            {
                return true;
            }
            else if (IsCurNavStar() && 
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

            if (IsCurNavStar())
            {
                navStar = true;
                dstPoint = target.TargetStar.uPosition;
            }
            else if (IsCurNavPlanet())
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
                VectorLF3 vectorLf3 = planet.uPosition - __instance.player.uPosition;
                double magnitude = vectorLf3.magnitude;
                if (magnitude < distance)
                    distance = magnitude;
            }

            return distance;
        }

        public bool IsCloseToNearStar(PlayerMove_Sail __instance)
        {
            bool closeFlag = false;
            double distance = NearestPlanetDistance(__instance);
            if (distance > 0)
            {
                if (GameMain.localStar != null && 
                    GameMain.localStar.planetCount <= sparseStarPlanetCount && 
                    distance < sparseStarPlanetNearastDistance
                    )
                    closeFlag = true;
                if (distance < planetNearastDistance)
                    closeFlag = true;
            }
            else if (GameMain.localStar != null && 
                (GameMain.localStar.uPosition - __instance.player.uPosition).magnitude < planetNearastDistance
                )
                closeFlag = true;

            return closeFlag;
        }

        public void StarNavigation(PlayerMove_Sail __instance)
        {
            ModDebug.Assert(IsCurNavStar());
            player = __instance.player;

            if (IsCurNavStar())
            {
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
                else
                {
                    if (DetermineArrive() && IsCloseToNearStar(__instance))
                    {
#if DEBUG
                        ModDebug.Log("StarNavigation Arrive");
#endif
                        Arrive();
                        Warp.TryLeaveWarp(__instance);
                        return;
                    }
                    else
                    {
                        LongDistanceNavigate(__instance);
                        return;
                    }
                }
            }
            else
            {
                Arrive();
#if DEBUG
                ModDebug.Error("StarNavigation - No Target");
#endif
            }
        }

        public void PlanetNavigation(PlayerMove_Sail __instance)
        {
            ModDebug.Assert(IsCurNavPlanet());
            player = __instance.player;

            if (IsCurNavPlanet())
            {
                double distance = (target.TargetPlanet.uPosition - __instance.player.uPosition).magnitude;

                if ((enableLocalWrap &&
                    distance > localWrapMinDistance &&
                    distance > (target.TargetPlanet.realRadius + longNavUncoverRange))
                    ||
                    target.IsLocalStarPlanet() == false)
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
                }
                else
                {
#if DEBUG
                    ModDebug.Log("Local Short Distance Navigation");
#endif
                    ShortDistanceNavigate(__instance);
                }
            }
            else
            {
                Arrive();
#if DEBUG
                ModDebug.Error("PlanetNavigation - No Target");
#endif
            }

        }

        private void ShortDistanceNavigate(PlayerMove_Sail __instance)
        {
            VectorLF3 dir = target.GetDir(this,__instance.player);
            Sail.SetDir(__instance,dir);

            VectorLF3 advisePoint = VectorLF3.zero;
            if (AdvisePointIfOcclusion(__instance, ref advisePoint, shortNavUncoverRange))
            {
#if DEBUG
                ModDebug.Log("PlanetNav ToAdvisePoint:" + advisePoint);
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
                    ModDebug.Log("ShortNav - Speed Up");
#endif
                    Sail.TrySpeedUp(this, __instance);
                }
                else
                {
#if DEBUG
                    ModDebug.Log("ShortNav - No Speed Up");
#endif
                }
            }
        }

        private void LongDistanceNavigate(PlayerMove_Sail __instance)
        {
            VectorLF3 dir = target.GetDir(this, __instance.player);
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
                else if (IsCurNavPlanet() && target.IsLocalStarPlanet() == true)
                {
#if DEBUG
                    ModDebug.Log("Local Planet Nav No Wrap Chance SpeedUp");
#endif
                    Sail.TrySpeedUp(this, __instance);
                    return;
                }
                else if (LongDistanceNavigateNeedSpeedUp())
                {
#if DEBUG
                    ModDebug.Log("Long Distance Navigate Need SpeedUp");
#endif
                    Sail.TrySpeedUp(this,__instance);
                }
                else
                {
#if DEBUG
                    ModDebug.Log("Long Distance Navigate No SpeedUp And Wrap");
#endif
                }
            }

            bool LongDistanceNavigateNeedSpeedUp()
            {
                if (__instance.player.mecha.coreEnergy >= speedUpEnergylimit)
                {
                    if (__instance.player.mecha.thrusterLevel < 3)
                        return true;
                    else if (Warp.GetWraperCount(__instance) <= 0)
                        return true;
                }
                //Prepare wrap
                if (__instance.player.mecha.coreEnergy < wrapEnergylimit)
                    return false;
                return false;
            }
        }

        public static class Warp
        {
            public static int GetWraperCount(PlayerMove_Sail __instance)
            {
                return __instance.player.mecha.warpStorage.GetItemCount(1210);
            }

            public static bool TryWrap(AutoStellarNavigation self, PlayerMove_Sail __instance)
            {
                if (HasWrapChance(self,__instance))
                {
                    TryEnterWarp(__instance);
                    return true;
                }
                return false;
            }

            public static bool HasWrapChance(AutoStellarNavigation self,PlayerMove_Sail __instance)
            {
                bool LocalPlanetWarp()
                {
                    if(GameMain.localPlanet != null && self.target.TargetPlanet.id == GameMain.localPlanet.id)
                        return false;
                    else if ((__instance.player.uPosition - self.target.TargetPlanet.uPosition).magnitude < (self.localWrapMinDistance))
                       return false;
                    else
                        return true;
                }


                if (self.IsCurNavStar() || LocalPlanetWarp())
                {
                    if (__instance.player.mecha.thrusterLevel >= 3)
                    {
                        if (__instance.mecha.coreEnergy > __instance.mecha.warpStartPowerPerSpeed * (double)__instance.mecha.maxWarpSpeed)
                        {
                            if (GetWraperCount(__instance) <= 0)
                                return false;
                            else
                                return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }                
                }
                else
                {
                    return false;
                }               
            }

            public static bool TryEnterWarp(PlayerMove_Sail __instance)
            {
                if (!__instance.player.warping && __instance.player.mecha.UseWarper())
                {
                    __instance.player.warpCommand = true;
                    VFAudio.Create("warp-begin", __instance.player.transform, Vector3.zero, true);
                    GameMain.gameScenario.NotifyOnWarpModeEnter();

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
            public static void SetDir(PlayerMove_Sail __instance, VectorLF3 dir)
            {
                __instance.sailPoser.targetURot = Quaternion.LookRotation(dir);
            }

            public static void TrySpeedUp(AutoStellarNavigation __this, PlayerMove_Sail __instance)
            {
                if( __instance.player.mecha.coreEnergy >= __this.speedUpEnergylimit)
                {
                    __this.sailSpeedUp = true;
                }
            }
        }

        public static class Walk
        {
            public static bool TrySwitchToFly(PlayerMove_Walk __instance)
            {
#if DEBUG
                ModDebug.Log("TrySwitchToFly");
#endif

                if (__instance.mecha.thrusterLevel >= 1)
                {
                    __instance.jumpCoolTime = 0.3f;
                    __instance.jumpedTime = 0.0f;
                
                    __instance.flyUpChance = 0.0f;
                    __instance.SwitchToFly();

                    return true;
                }
                else
                {
                    return false;
                }             
            }
        }

        public static class Fly
        {
            public static bool TrySwtichToSail(PlayerMove_Fly __instance)
            {
#if DEBUG
                ModDebug.Log("TrySwtichToSail");
#endif

                if (__instance.mecha.thrusterLevel >= 2)
                {
                    if (__instance.controller.cmd.type == ECommand.Build)
                        __instance.controller.cmd.type = ECommand.None;
                    __instance.controller.movementStateInFrame = EMovementState.Sail;
                    __instance.controller.actionSail.ResetSailState();
                    GameCamera.instance.SyncForSailMode();
                    GameMain.gameScenario.NotifyOnSailModeEnter();

                    return true;
                }
                else
                {
                    return false;
                }      
            }
        }


    }
}
