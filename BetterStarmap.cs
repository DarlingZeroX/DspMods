using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace DspMod
{
    [BepInPlugin(__GUID__, __NAME__, __VERSION__)]
    public class BetterStarmap : BaseUnityPlugin
    {
        public const string __NAME__ = "betterstarmap";
        public const string __GUID__ = "0x.plugins.dsp." + __NAME__;
        public const string __VERSION__ = "1.1.1";

        static public BetterStarmap self;

        private bool isStarMapOpened = false;

        private PlanetData hoveredPlanet = null;
        private StarData hoveredStar = null;
    
        private bool starMap_DisplayName = true;
        private bool starMap_ImmediateMode = false;
        private bool starMap_DetailsPreview = true;
        private bool starMap_DisplayUnknown = false;
        private bool starMap_HighLuminosity = false;
        private bool starMap_Blackhole = false;
        private bool starMap_GiantStar = false;
        private bool starMap_WhiteDwarf = false;

        public static ConfigEntry< bool > EnableDisplayUnknown;
        public static ConfigEntry<bool> EnableImmediateMode;

        public static ConfigEntry<float> DisplayPositionX;
        public static ConfigEntry<float> DisplayPositionY;

        private int historyUniverseObserveLevel = -1;

        //Legacy
        private UIStarmapStar navigatedStar = null;
        private int navigatedStarID = -1;
        private string navigatedStarName = null;

        void Start()
        {
            self = this;

            EnableDisplayUnknown = Config.Bind<bool>("config", "DisplayUnknown", false, "是否开启探测未知信息功能");
            EnableImmediateMode = Config.Bind<bool>("config", "ImmediateMode", true, "是否开启查看立即模式功能");
            DisplayPositionX = Config.Bind<float>("config", "DisplayPositionX", 0.01f, "UI显示位置X");
            DisplayPositionY = Config.Bind<float>("config", "DisplayPositionY", 0.7f, "UI显示位置Y");


            new Harmony(__GUID__).PatchAll();
        }

        private void OnGUI()
        {
            if (isStarMapOpened)
            {
                if(Localization.language == Language.zhCN)
                {
                    GUILayout.BeginArea(new Rect(Screen.width * DisplayPositionX.Value, Screen.height * DisplayPositionY.Value, 200, 300));
                    GUILayout.Label("星图功能".Translate());
                    starMap_DetailsPreview = GUILayout.Toggle(starMap_DetailsPreview, "星球细节预览".Translate());
                    if (EnableImmediateMode.Value)
                        starMap_ImmediateMode = GUILayout.Toggle(starMap_ImmediateMode, "查看立即模式".Translate());
                    starMap_DisplayName = GUILayout.Toggle(starMap_DisplayName, "显示星球名称".Translate());
                    if (EnableDisplayUnknown.Value)
                        starMap_DisplayUnknown = GUILayout.Toggle(starMap_DisplayUnknown, "探测未知信息".Translate());
                    GUILayout.Label("星系显示".Translate());
                    starMap_HighLuminosity = GUILayout.Toggle(starMap_HighLuminosity, "高光度恒星".Translate());
                    starMap_Blackhole = GUILayout.Toggle(starMap_Blackhole, "黑洞中子星".Translate());
                    starMap_GiantStar = GUILayout.Toggle(starMap_GiantStar, "巨星".Translate());
                    starMap_WhiteDwarf = GUILayout.Toggle(starMap_WhiteDwarf, "白矮星".Translate());
                    GUILayout.EndArea();
                }
                else
                {
                    GUILayout.BeginArea(new Rect(Screen.width * DisplayPositionX.Value, Screen.height * DisplayPositionY.Value, 200, 300));
                    GUILayout.Label("Starmap Features".Translate());
                    starMap_DetailsPreview = GUILayout.Toggle(starMap_DetailsPreview, "DetailsPreview".Translate());
                    if (EnableImmediateMode.Value)
                        starMap_ImmediateMode = GUILayout.Toggle(starMap_ImmediateMode, "ImmediateMod");
                    starMap_DisplayName = GUILayout.Toggle(starMap_DisplayName, "StarName");
                    if (EnableDisplayUnknown.Value)
                        starMap_DisplayUnknown = GUILayout.Toggle(starMap_DisplayUnknown, "UnknownStarInfo");
                    GUILayout.Label("Star Filter");
                    starMap_HighLuminosity = GUILayout.Toggle(starMap_HighLuminosity, "HighLuminosity");
                    starMap_Blackhole = GUILayout.Toggle(starMap_Blackhole, "Blackhole");
                    starMap_GiantStar = GUILayout.Toggle(starMap_GiantStar, "GiantStar");
                    starMap_WhiteDwarf = GUILayout.Toggle(starMap_WhiteDwarf, "WhiteDwarf");
                    GUILayout.EndArea();
                }



                //DisplayUnknown
                if (EnableDisplayUnknown.Value)
                {
                    if (historyUniverseObserveLevel == -1)
                        historyUniverseObserveLevel = GameMain.history.universeObserveLevel;

                    if (starMap_DisplayUnknown)
                        GameMain.history.universeObserveLevel = 4;
                    else
                        GameMain.history.universeObserveLevel = historyUniverseObserveLevel;
                }
            }
        }

        [HarmonyPatch(typeof(UIStarmap), "OnStarClick")]
        private class ImmediateMode_Imp
        {
            private static void Prefix(UIStarmap __instance, ref UIStarmapStar star)
            {
                if (self.starMap_ImmediateMode)
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
            private static bool Prefix(UIStarmap __instance)
            {
                self.hoveredStar = (StarData)null;
                self.hoveredPlanet = (PlanetData)null;

                if (EnableDisplayUnknown.Value)
                {
                    GameMain.history.universeObserveLevel = self.historyUniverseObserveLevel;
                    self.historyUniverseObserveLevel = -1;
                    self.starMap_DisplayUnknown = false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UIGame), "SetPlanetDetail")]
        private class DetailsPreview_Planet_Imp
        {
            private static void Prefix(UIGame __instance, ref PlanetData planet)
            {
                if (__instance.starmap.isFullOpened)
                {
                    if (self.hoveredStar != null)
                    {
                        planet = (PlanetData)null;
                    }
                    else if (self.hoveredPlanet != null)
                    {
                        planet = self.hoveredPlanet;
                        self.hoveredStar = (StarData)null;
                    }
                    else if (planet != null)
                    {
                        self.hoveredStar = (StarData)null;
                    }

                }
            }
        }

        [HarmonyPatch(typeof(UIGame), "SetStarDetail")]
        private class DetailsPreview_Star_Imp
        {
            private static void Prefix(UIGame __instance, ref StarData star)
            {
                if (__instance.starmap.isFullOpened)
                {
                    if (self.hoveredPlanet != null)
                    {
                        star = (StarData)null;
                    }
                    else if (self.hoveredStar != null)
                    {
                        self.hoveredPlanet = (PlanetData)null;
                        star = self.hoveredStar;
                    }
                    else if (star != null)
                    {
                        self.hoveredPlanet = (PlanetData)null;
                    }

                }
            }
        }

        [HarmonyPatch(typeof(UIStarmap), "MouseHoverCheck")]
        private class MouseHoverCheck
        {
            private static void Postfix(UIStarmap __instance)
            {
                self.isStarMapOpened = __instance.isFullOpened;

                if (self.starMap_DetailsPreview)
                {
                    if (__instance.mouseHoverStar)
                    {
                        self.hoveredStar = __instance.mouseHoverStar.star;
                        self.hoveredPlanet = (PlanetData)null;
                    }
                    else if (__instance.mouseHoverPlanet)
                    {
                        self.hoveredPlanet = __instance.mouseHoverPlanet.planet;
                        self.hoveredStar = (StarData)null;
                    }
                }
                else
                {
                    self.hoveredPlanet = (PlanetData)null;
                    self.hoveredStar = (StarData)null;
                }

            }
        }

        [HarmonyPatch(typeof(UIStarmapStar), "_OnLateUpdate")]
        private class StarmapStarHighlight_Imp
        {
            private static void Postfix(UIStarmapStar __instance)
            {
                if (self.starMap_DisplayName)
                {
                    Text nameText = Traverse.Create((object)__instance).Field("nameText").GetValue<Text>();

                    OriginalSetTextActive(__instance,ref nameText);
                    SetNameTextColor(__instance,ref nameText);
                   
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

            private static void SetNameTextColor(UIStarmapStar __instance,ref Text nameText)
            {
                nameText.color = Color.white;

                if (self.starMap_HighLuminosity && __instance.star.dysonLumino > 2.0f)
                    nameText.color = Color.magenta;
                else if (self.starMap_Blackhole && (__instance.star.type == EStarType.BlackHole || __instance.star.type == EStarType.NeutronStar))
                    nameText.color = Color.green;
                else if (self.starMap_GiantStar && (__instance.star.type == EStarType.GiantStar))
                    nameText.color = Color.green;
                else if (self.starMap_WhiteDwarf && (__instance.star.type == EStarType.WhiteDwarf))
                    nameText.color = Color.green;
                //Legacy
                else if (__instance.star.id == self.navigatedStarID && self.navigatedStarID != -1)
                    nameText.color = Color.red;

            }

        }

        [HarmonyPatch(typeof(GameHistoryData), "UnlockTechFunction")]
        private class GameHistoryDataUnlockTechFunction
        {
            private static void Prefix(ref int func, ref double value, ref int level)
            {
                if (EnableDisplayUnknown.Value)
                {
                    int num = value <= 0.0 ? (int)(value - 0.5f) : (int)(value + 0.5f);
                    if (func == 23)
                        self.historyUniverseObserveLevel = num;
                }

            }
        }

        /// <summary>
        /// Legacy
        /// </summary>
        

        [HarmonyPatch(typeof(UIStarmap), "OnScreenClick")]
        private class UIStarmapOnScreenClick
        {
            private static bool Prefix(UIStarmap __instance, ref BaseEventData evtData)
            {
                if (!(evtData is PointerEventData pointerEventData) || pointerEventData.button != PointerEventData.InputButton.Left && pointerEventData.button != PointerEventData.InputButton.Right)
                    return false;
                if ((UnityEngine.Object)__instance.mouseHoverPlanet != (UnityEngine.Object)null)
                {
                    __instance.OnPlanetClick(__instance.mouseHoverPlanet);
                }
                else
                {
                    if (!((UnityEngine.Object)__instance.mouseHoverStar != (UnityEngine.Object)null))
                        return false;
                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                    {
                        UISpaceGuide spaceGuide = UIRoot.instance.uiGame.spaceGuide;
                        if (spaceGuide != null)
                        {
                            if (self.navigatedStarName != null && self.navigatedStar != null)
                            {
                                self.navigatedStar.star.overrideName = self.navigatedStarName;
                                Traverse.Create((object)self.navigatedStar).Field("nameText").GetValue<Text>().text = self.navigatedStarName;
                                self.navigatedStarName = null;
                            }

                            spaceGuide.SetStarPin(self.navigatedStarID, false);
                            if (__instance.mouseHoverStar.star.id == self.navigatedStarID)
                            {
                                self.navigatedStar = null;
                                self.navigatedStarID = -1;
                            }
                            else
                            {
                                self.navigatedStar = __instance.mouseHoverStar;
                                self.navigatedStarID = __instance.mouseHoverStar.star.id;
                                spaceGuide.SetStarPin(__instance.mouseHoverStar.star.id, true);
                                self.navigatedStarName = __instance.mouseHoverStar.star.displayName;
                                if (Localization.language == Language.zhCN)
                                {
                                    __instance.mouseHoverStar.star.overrideName = self.navigatedStarName + "(目标)";
                                }
                                else
                                {
                                    __instance.mouseHoverStar.star.overrideName = self.navigatedStarName + "(Target)";
                                }

                                Traverse.Create((object)__instance.mouseHoverStar).Field("nameText").GetValue<Text>().text = __instance.mouseHoverStar.star.overrideName;
                            }
                        }
                        return false;
                    }

                    __instance.OnStarClick(__instance.mouseHoverStar);
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(UISpaceGuideEntry), "_OnLateUpdate")]
        private class UISpaceGuideEntry_OnLateUpdate
        {
            private static void Postfix(UISpaceGuideEntry __instance)
            {
                Image image = Traverse.Create((object)__instance).Field("markIcon").GetValue<Image>();
                Text nameText = Traverse.Create((object)__instance).Field("nameText").GetValue<Text>();
                if (__instance.guideType == ESpaceGuideType.Star && __instance.objId == self.navigatedStarID && self.navigatedStarID != -1)
                {

                    image.rectTransform.localScale = new Vector3(2.2f, 1.0f, 1.0f);
                    image.color = Color.red;
                    nameText.color = new Color(1.0f, 0.0f, 0.0f, 0.8f);
                }
                else
                {
                    image.rectTransform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    nameText.color = Color.white;
                }


            }
        }

        

    }
}