using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace AutoNavigate
{
    [BepInPlugin(__GUID__, __NAME__, "1.02")]
    public class AutoNavigate : BaseUnityPlugin
    {
        public const string __NAME__ = "StellarAutoNavigation";
        public const string __GUID__ = "0x.plugins.dsp." + __NAME__;

        public static AutoNavigate __this;
        public static AutoStellarNavigation s_NavigateInstance;
        public static bool s_IsHistoryNavigated = false;
        public static ConfigEntry<double> s_NavigateMinEnergy;

        private Player player = null;

        private void Start()
        {
            __this = this;
            s_NavigateInstance = new AutoStellarNavigation(GetNavigationConfig());

            new Harmony(__GUID__).PatchAll();
        }

        private void Update()
        {
            //导航开关
            if (Input.GetKeyDown(KeyCode.K) && player != null)
            {
                s_NavigateInstance.ToggleNavigate();
            }
        }

        private AutoStellarNavigation.NavigationConfig GetNavigationConfig()
        {
            var config = new AutoStellarNavigation.NavigationConfig();

            s_NavigateMinEnergy = Config.Bind<double>(
                "AutoStellarNavigation",
                "minAutoNavEnergy",
                50000000.0,
                "开启自动导航最低能量(最低50m)"
                );
            config.speedUpEnergylimit = Config.Bind<double>(
                "AutoStellarNavigation",
                "SpeedUpEnergylimit",
                50000000.0,
                "开启加速最低能量(默认50m)"
                );
            config.wrapEnergylimit = Config.Bind<double>(
                "AutoStellarNavigation",
                "WrapEnergylimit",
                800000000,
                "开启曲率最低能量(默认800m)"
                );
            config.enableLocalWrap = Config.Bind<bool>(
                "AutoStellarNavigation",
                "EnableLocalWrap",
                true,
                "是否开启本地行星曲率飞行"
                );
            config.localWrapMinDistance = Config.Bind<double>(
                "AutoStellarNavigation",
                "LocalWrapMinDistance",
                100000.0,
                "本地行星曲率飞行最短距离"
                );

            if (s_NavigateMinEnergy.Value < 50000000.0)
                s_NavigateMinEnergy.Value = 50000000.0;

            return config;
        }

        /// <summary>
        /// 安全模式，确保不会出现一些未知错误
        /// </summary>
        private class SafeMode
        {
            public static void Reset()
            {
                s_IsHistoryNavigated = false;
                s_NavigateInstance.Reset();
                s_NavigateInstance.target.Reset();
                __this.player = null;
            }

            [HarmonyPatch(typeof(GameMain), "OnDestroy")]
            public class SafeDestroy
            {
                private static void Prefix() =>
                    Reset();
            }

            [HarmonyPatch(typeof(GameMain), "Pause")]
            public class SafePause
            {
                public static void Prefix() =>
                    s_NavigateInstance.Pause();
            }

            [HarmonyPatch(typeof(GameMain), "Resume")]
            public class SafeResume
            {
                public static void Prefix() =>
                    s_NavigateInstance.Resume();
            }
        }

        [HarmonyPatch(typeof(PlayerController), "Init")]
        private class PlayerControllerInit
        {
            private static void Postfix(PlayerController __instance) =>
                __this.player = __instance.player;
        }

        /// <summary>
        /// Navigate Tips
        /// </summary>
        [HarmonyPatch(typeof(UIGeneralTips), "_OnUpdate")]
        private class NavigateTips
        {
            //Tip position offest
            private static Vector2 anchoredPosition = new Vector2(0.0f, 160.0f);

            public static void Postfix(UIGeneralTips __instance)
            {
                if (!s_NavigateInstance.enable)
                    return;

                Text modeText = Traverse.Create((object)__instance).Field("modeText").GetValue<Text>();

                modeText.gameObject.SetActive(true);
                modeText.rectTransform.anchoredPosition = anchoredPosition;

                if (s_NavigateInstance.IsCurNavPlanet)
                    modeText.text = "星际自动导航".ModText();
                else if (s_NavigateInstance.IsCurNavStar)
                    modeText.text = "星系自动导航".ModText();

                s_NavigateInstance.modeText = modeText;
            }
        }

        /// <summary>
        /// Sail speed up
        /// </summary>
        [HarmonyPatch(typeof(VFInput), "_sailSpeedUp", MethodType.Getter)]
        private class SailSpeedUp
        {
            private static void Postfix(ref bool __result)
            {
                if (!s_NavigateInstance.enable)
                    return;

                if (s_NavigateInstance.sailSpeedUp)
                    __result = true;
            }
        }

        /// <summary>
        /// Sail mode
        /// </summary>
        [HarmonyPatch(typeof(PlayerMove_Sail), "GameTick")]
        private class SailMode_AutoNavigate
        {
            private static Quaternion oTargetURot;

            private static void Prefix(PlayerMove_Sail __instance)
            {
                if (!s_NavigateInstance.enable)
                    return;

                if (!__instance.player.sailing && !__instance.player.warping)
                    return;

                ++__instance.controller.input0.y;
                oTargetURot = __instance.sailPoser.targetURot;

                if (s_NavigateInstance.IsCurNavStar)
                    s_NavigateInstance.StarNavigation(__instance);
                else if (s_NavigateInstance.IsCurNavPlanet)
                    s_NavigateInstance.PlanetNavigation(__instance);
            }

            private static void Postfix(PlayerMove_Sail __instance)
            {
                // Sail 默认不加速
                s_NavigateInstance.sailSpeedUp = false;

                if (!s_NavigateInstance.enable)
                    return;

                if (GameMain.localPlanet != null ||
                    s_NavigateInstance.target.IsVaild())
                {
                    __instance.sailPoser.targetURot = oTargetURot;
                    s_NavigateInstance.HandlePlayerInput();
                }
            }
        }

        /// <summary>
        /// Fly mode
        /// </summary>
        [HarmonyPatch(typeof(PlayerMove_Fly), "GameTick")]
        private class FlyMode_TrySwtichToSail
        {
            private static float sailMinAltitude = 49.0f;

            /// <summary>
            /// Fly --> Sail or Arrive
            /// </summary>
            private static void Prefix(PlayerMove_Fly __instance)
            {
                if (!s_NavigateInstance.enable)
                    return;

                if (__instance.player.movementState != EMovementState.Fly)
                    return;

                if (s_NavigateInstance.DetermineArrive())
                {
                    ModDebug.Log("FlyModeArrive");
                    s_NavigateInstance.Arrive();
                }
                else if (__instance.mecha.thrusterLevel < 2)
                {
                    s_NavigateInstance.Arrive("驱动引擎等级过低".ModText());
                }
                else if (__instance.player.mecha.coreEnergy < s_NavigateMinEnergy.Value)
                {
                    s_NavigateInstance.Arrive("机甲能量过低".ModText());
                }
                else
                {
                    ++__instance.controller.input1.y;

                    if (__instance.currentAltitude > sailMinAltitude)
                        AutoStellarNavigation.Fly.TrySwtichToSail(__instance);
                }
            }
        }

        /// <summary>
        /// Walk mode
        /// </summary>
        [HarmonyPatch(typeof(PlayerMove_Walk), "UpdateJump")]
        private class WalkMode_TrySwtichToFly
        {
            /// <summary>
            /// Walk --> Fly or Arrive
            /// </summary>
            private static void Postfix(PlayerMove_Walk __instance, ref bool __result)
            {
                __result = false;

                if (!s_NavigateInstance.enable)
                    return;

                if (!s_NavigateInstance.target.IsVaild())
                    return;

                if (s_NavigateInstance.DetermineArrive())
                {
                    ModDebug.Log("WalkModeArrive");
                    s_NavigateInstance.Arrive();
                }
                else if (__instance.mecha.thrusterLevel < 1)
                {
                    s_NavigateInstance.Arrive("驱动引擎等级过低".ModText());
                }
                else if (__instance.player.mecha.coreEnergy < s_NavigateMinEnergy.Value)
                {
                    s_NavigateInstance.Arrive("机甲能量过低".ModText());
                }
                else
                {
                    AutoStellarNavigation.Walk.TrySwitchToFly(__instance);
                    //切换至Fly Mode 中对 UpdateJump 方法进行拦截
                    __result = true;
                }
            }
        }

        /// --------------------------
        /// Starmap Indicator   游戏内置星球导航指示标
        /// --------------------------
        [HarmonyPatch(typeof(UIStarmap), "OnCursorFunction3Click")]
        private class OnSetIndicatorAstro
        {
            /// <summary>
            /// 根据 Indicator (导航指示标) 设置导航目标
            /// </summary>
            private static void Prefix(UIStarmap __instance)
            {
                PlayerNavigation navigation = GameMain.mainPlayer.navigation;

                if (__instance.focusPlanet != null &&
                    navigation.indicatorAstroId != __instance.focusPlanet.planet.id)
                {
                    s_NavigateInstance.target.SetTarget(__instance.focusPlanet.planet);
                }
                else if (__instance.focusStar != null &&
                    navigation.indicatorAstroId != __instance.focusStar.star.id * 100)
                {
                    s_NavigateInstance.target.SetTarget(__instance.focusStar.star);
                }
            }
        }
    }
}