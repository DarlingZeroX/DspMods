using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace BetterStarmap
{
    class DetailsPreview_Impl : BaseFeature<DetailsPreview_Impl>, IFeature
    {
        private PlanetData hoveredPlanet = null;
        private StarData hoveredStar = null;

        public DetailsPreview_Impl()
        {
            SetFeatureName( "DetailsPreview" );
        }

        public void DrawGui()
        {
            isEnable = GUILayout.Toggle(isEnable, "星球细节预览".ModText());
        }

        void Reset()
        {
            hoveredStar = (StarData)null;
            hoveredPlanet = (PlanetData)null;
        }

        void SetHover(StarData star)
        {
            hoveredStar = star;
            hoveredPlanet = (PlanetData)null;
        }

        void SetHover(PlanetData planet)
        {
            hoveredPlanet = planet;
            hoveredStar = (StarData)null;
        }

        bool IsHoverStar => hoveredStar != null;
        bool IsHoverPlanet => hoveredPlanet != null;

        public void OnMouseHover(UIStarmap __instance)
        {
            if (isEnable)
                if (__instance.mouseHoverStar)
                    SetHover( __instance.mouseHoverStar.star );
                else if (__instance.mouseHoverPlanet)
                    SetHover(__instance.mouseHoverPlanet.planet);
            else
                Reset();
        }

        public void OnStarmapClose()
        {
            Reset();
        }

        public void OnSetPlanetDetail(ref PlanetData planet)
        {
            if (IsHoverStar)
            {
                planet = (PlanetData)null;
            }
            else if (IsHoverPlanet)
            {
                planet = hoveredPlanet;
                hoveredStar = (StarData)null;
            }
            else if (planet != null)
            {
                hoveredStar = (StarData)null;
            }
        }

        public void OnSetStarDetail(ref StarData star)
        {
            if (IsHoverPlanet)
            {
                star = (StarData)null;
            }
            else if (IsHoverStar)
            {
                hoveredPlanet = (PlanetData)null;
                star = hoveredStar;
            }
            else if (star != null)
            {
                hoveredPlanet = (PlanetData)null;
            }
        }
    }

    class ImmediateMode_Impl : BaseFeature<ImmediateMode_Impl>, IFeature
    {
        public ImmediateMode_Impl(ConfigEntry<bool> enable)
        {
            SetFeatureName("ImmediateMode");
            configEnable = enable;
        }
        public void DrawGui()
        {
            if (configEnable.Value)
                isEnable = GUILayout.Toggle(isEnable, "查看立即模式".ModText());
        }
    }

    class DisplayStarName_Impl : BaseFeature<DisplayStarName_Impl>, IFeature
    {
        public DisplayStarName_Impl()
        {
            SetFeatureName("DisplayStarName");
        }
        public void DrawGui()
        {
             isEnable = GUILayout.Toggle(isEnable, "显示星球名称".ModText());
        }
    }

    class DisplayUnknown_Impl : BaseFeature<DisplayUnknown_Impl>, IFeature
    {
        public static int historyUniverseObserveLevel = -1;

        public DisplayUnknown_Impl(ConfigEntry<bool> enable)
        {
            SetFeatureName("DisplayUnknown");
            configEnable = enable;
        }
        public void DrawGui()
        {
            if (configEnable.Value)
                isEnable = GUILayout.Toggle(isEnable, "探测未知信息".ModText());

            CheckDisplayUnknown();
        }

        private void CheckDisplayUnknown()
        {
            if (configEnable.Value)
            {
                if (historyUniverseObserveLevel == -1)
                    historyUniverseObserveLevel = GameMain.history.universeObserveLevel;
                if (isEnable)
                    GameMain.history.universeObserveLevel = 4;
                else
                    GameMain.history.universeObserveLevel = historyUniverseObserveLevel;
            }
        }

        public void OnStarmapClose()
        {
            if (configEnable.Value)
            {
                if(historyUniverseObserveLevel != -1)
                    GameMain.history.universeObserveLevel = historyUniverseObserveLevel;
                DisplayUnknown_Impl.historyUniverseObserveLevel = -1;
                DisplayUnknown_Impl.isEnable = false;
            }
        }

        public void OnTechUnlock(ref int func, ref double value, ref int level)
        {
            if (configEnable.Value)
            {
                int num = value <= 0.0 ? (int)(value - 0.5f) : (int)(value + 0.5f);
                if (func == 23)
                    historyUniverseObserveLevel = num;
            }
        }
    }

    class StarHighlight_Impl : BaseFeature<DisplayStarName_Impl>, IFeature
    {
        public interface IStarHighlight
        {
            void DrawGui();
            void SetStarColor(UIStarmapStar __instance, ref Text nameText);
        }

        Queue<IStarHighlight> hignLightQueue = new Queue<IStarHighlight>();

        public void AddFeature(IStarHighlight feature)
        {
            hignLightQueue.Enqueue(feature);
        }

        public StarHighlight_Impl()
        {
            SetFeatureName("StarHighlight");
        }
        public void DrawGui()
        {
            foreach(var feature in hignLightQueue)
            {
                feature.DrawGui();
            }
        }

        public void SetStarColor(UIStarmapStar __instance, ref Text nameText)
        {
            foreach (var feature in hignLightQueue)
            {
                feature.SetStarColor(__instance,ref nameText);
            }
        }

        public class HighLuminosity : IStarHighlight
        {
            bool enable = false;

            public void DrawGui()
            {
                enable = GUILayout.Toggle(enable, "高光度恒星".ModText());
            }

            public void SetStarColor(UIStarmapStar __instance, ref Text nameText)
            {
                if (enable && __instance.star.dysonLumino > 2.0f)
                    nameText.color = Color.magenta;
            }
        }

        public class Blackhole : IStarHighlight
        {
            bool enable = false;

            public void DrawGui()
            {
                enable = GUILayout.Toggle(enable, "黑洞中子星".ModText());
            }

            public void SetStarColor(UIStarmapStar __instance, ref Text nameText)
            {
                if (enable && (__instance.star.type == EStarType.BlackHole || __instance.star.type == EStarType.NeutronStar))
                    nameText.color = Color.green;
            }
        }

        public class GiantStar : IStarHighlight
        {
            bool enable = false;

            public void DrawGui()
            {
                enable = GUILayout.Toggle(enable, "巨星".ModText());
            }

            public void SetStarColor(UIStarmapStar __instance, ref Text nameText)
            {
                if (enable && (__instance.star.type == EStarType.GiantStar))
                    nameText.color = Color.green;
            }
        }

        public class WhiteDwarf : IStarHighlight
        {
            bool enable = false;

            public void DrawGui()
            {
                enable = GUILayout.Toggle(enable, "白矮星".ModText());
            }

            public void SetStarColor(UIStarmapStar __instance, ref Text nameText)
            {
                if (enable && (__instance.star.type == EStarType.WhiteDwarf))
                    nameText.color = Color.green;
            }
        }
    }

    [BepInPlugin(__GUID__, __NAME__, "1.1.1")]
    public class BetterStarmap : BaseUnityPlugin
    {
        public const string __NAME__ = "betterstarmap";
        public const string __GUID__ = "0x.plugins.dsp." + __NAME__;
        
        static public BetterStarmap self;
        private bool isStarMapOpened = false;

        public static ConfigEntry<float> DisplayPositionX;
        public static ConfigEntry<float> DisplayPositionY;

        static FeaturesManage mainFeatures = new FeaturesManage();
        static StarHighlight_Impl g_StarHighLight = new StarHighlight_Impl();

        void Start()
        {
            //Add Features
            mainFeatures.AddFeatrue(new DetailsPreview_Impl());
            mainFeatures.AddFeatrue(
                new ImmediateMode_Impl( Config.Bind<bool>("config", "ImmediateMode", true, "是否开启查看立即模式功能") 
                ));
            mainFeatures.AddFeatrue(new DisplayStarName_Impl());
            mainFeatures.AddFeatrue(
                new DisplayUnknown_Impl(Config.Bind<bool>("config", "DisplayUnknown", true, "是否开启探测未知信息功能")
                ));

            //Star HighLight
            g_StarHighLight.AddFeature(new StarHighlight_Impl.HighLuminosity());
            g_StarHighLight.AddFeature(new StarHighlight_Impl.Blackhole());
            g_StarHighLight.AddFeature(new StarHighlight_Impl.GiantStar());
            g_StarHighLight.AddFeature(new StarHighlight_Impl.WhiteDwarf());

            //Get Display Position
            DisplayPositionX = Config.Bind<float>("config", "DisplayPositionX", 0.01f, "UI显示位置X");
            DisplayPositionY = Config.Bind<float>("config", "DisplayPositionY", 0.7f, "UI显示位置Y");

            self = this;
            new Harmony(__GUID__).PatchAll();
        }
        
        private void OnGUI()
        {
            if (isStarMapOpened)
            {
                GUILayout.BeginArea(new Rect(Screen.width * DisplayPositionX.Value, Screen.height * DisplayPositionY.Value, 200, 300));

                GUILayout.Label("星图功能".ModText());

                foreach(var feature in mainFeatures.features)
                {
                    feature.Value.DrawGui();
                }
                GUILayout.Label("星系显示".ModText());
                g_StarHighLight.DrawGui();

                GUILayout.EndArea();
            }
        }

        [HarmonyPatch(typeof(UIStarmap), "OnStarClick")]
        private class ImmediateMode
        {
            private static void Postfix(UIStarmap __instance, ref UIStarmapStar star)
            {
                if (ImmediateMode_Impl.isEnable)
                {
                    if (__instance.viewStar == star.star)
                        return;
                    __instance.screenCameraController.SetViewTarget((PlanetData)null, star.star, (Player)null, VectorLF3.zero, (double)star.star.physicsRadius * 0.899999976158142 * 0.00025, (double)star.star.physicsRadius * (double)Mathf.Pow(star.star.radius * 0.4f, -0.4f) * 3.0 * 0.00025, true, true);

                }
            }
        }

        [HarmonyPatch(typeof(UIStarmap), "_OnClose")]
        private class SafeClose
        {
            private static void Prefix(UIStarmap __instance)
            {
                ((DetailsPreview_Impl)mainFeatures["DetailsPreview"]).OnStarmapClose();
                ((DisplayUnknown_Impl)mainFeatures["DisplayUnknown"]).OnStarmapClose();
            }
        }

        [HarmonyPatch(typeof(UIGame), "SetPlanetDetail")]
        private class DetailsPreview_Planet_Impl
        {
            private static void Prefix(UIGame __instance, ref PlanetData planet)
            {
                if (__instance.starmap.isFullOpened)
                {
                    ((DetailsPreview_Impl)mainFeatures["DetailsPreview"]).OnSetPlanetDetail(ref planet);
                }
            }
        }

        [HarmonyPatch(typeof(UIGame), "SetStarDetail")]
        private class DetailsPreview_Star_Impl
        {
            private static void Prefix(UIGame __instance, ref StarData star)
            {
                if (__instance.starmap.isFullOpened)
                {
                    ((DetailsPreview_Impl)mainFeatures["DetailsPreview"]).OnSetStarDetail(ref star);
                }
            }
        }

        [HarmonyPatch(typeof(UIStarmap), "MouseHoverCheck")]
        private class MouseHoverCheck
        {
            private static void Postfix(UIStarmap __instance)
            {
                self.isStarMapOpened = __instance.isFullOpened;

                ((DetailsPreview_Impl)mainFeatures["DetailsPreview"]).OnMouseHover(__instance);
            }
        }
        
        [HarmonyPatch(typeof(UIStarmapStar), "_OnLateUpdate")]
        private class StarmapStarHighlight_Imp
        {
            private static void Prefix(UIStarmapStar __instance)
            {
                Text nameText = Traverse.Create((object)__instance).Field("nameText").GetValue<Text>();
                nameText.color = Color.white;
            }

            private static void Postfix(UIStarmapStar __instance)
            {
                if (DisplayStarName_Impl.isEnable)
                {
                    Text nameText = Traverse.Create((object)__instance).Field("nameText").GetValue<Text>();

                    OriginalSetTextActive(__instance,ref nameText);
                    g_StarHighLight.SetStarColor(__instance, ref nameText);
                }

            }

            private static void OriginalSetTextActive(UIStarmapStar __instance, ref Text nameText)
            {
                GameHistoryData historyData = Traverse.Create((object)__instance).Field("gameHistory").GetValue<GameHistoryData>();

                Vector2 rectPoint = Vector2.zero;
                bool flag = __instance.starmap.WorldPointIntoScreen(__instance.starObject.vpos, out rectPoint) && ((UnityEngine.Object)__instance.starmap.mouseHoverStar == (UnityEngine.Object)__instance || historyData.HasFeatureKey(1001001) || historyData.HasFeatureKey(1010000 + __instance.star.id));

                float num = Mathf.Max(1f, __instance.starObject.vdist / __instance.starObject.vscale.x);
                __instance.projectedCoord = rectPoint;
                rectPoint.x += (float)(8.0f + 600f / (float)num);
                rectPoint.y += 4.0f;
                nameText.rectTransform.anchoredPosition = rectPoint;

                __instance.projected = true;
                nameText.gameObject.SetActive(true);
            }
        }

        [HarmonyPatch(typeof(GameHistoryData), "UnlockTechFunction")]
        private class GameHistoryDataUnlockTechFunction
        {
            private static void Prefix(ref int func, ref double value, ref int level)
            {
                ((DisplayUnknown_Impl)mainFeatures["DisplayUnknown"]).OnTechUnlock(ref func,ref value,ref level);
            }
        }
        

    }
}