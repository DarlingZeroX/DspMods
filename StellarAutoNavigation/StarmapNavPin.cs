using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace AutoNavigate
{
    public class StarmapNavPin
    {
        public StarmapStarPlanet target;
        public UIStarmapStar uiStar;
        public UIStarmapPlanet uiPlanet;
        public bool isPined = false;
        public bool alreadyPin = false;

        public StarmapNavPin()
        {
            target = new StarmapStarPlanet();
        }

        public bool SetPin(UIStarmapStar star)
        {
            ModDebug.Assert(star != null);
            if (star != null)
            {
                target.Set(star);
                return true;
            }
            else
            {
                return false;
            }
            
        }

        public bool SetPin(UIStarmapStar uiStar, StarData star)
        {
            ModDebug.Assert(uiStar != null && star != null);
            if (uiStar != null)
            {
                this.uiStar = uiStar;
                target.Set(star);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetPin(UIStarmapPlanet uiPlanet, PlanetData planet)
        {
            ModDebug.Assert(uiPlanet != null && planet != null);
            if (uiPlanet != null)
            {
                this.uiPlanet = uiPlanet;
                target.Set(planet);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool RecoverName()
        {
            if (target.name != string.Empty)
            {
                if (target.planet != null && uiPlanet != null)
                {
                    target.SetPlanetOverrideName(target.name);
                    Traverse.Create((object)uiPlanet).Field("nameText").GetValue<Text>().text = target.name;
                    return true;
                }
                else if (target.star != null && uiStar != null)
                {
                    target.SetStarOverrideName(target.name);
                    Traverse.Create((object)uiStar).Field("nameText").GetValue<Text>().text = target.name;
                    return true;
                }
                else
                {
                    ModDebug.Assert(false);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static bool SetPlanetName(UIStarmap __instance, string name)
        {
            ModDebug.Assert((__instance.mouseHoverPlanet != null && __instance.mouseHoverPlanet.planet != null));
            if(__instance.mouseHoverPlanet != null && 
                __instance.mouseHoverPlanet.planet != null)
            {
                __instance.mouseHoverPlanet.planet.overrideName = name;
                Traverse.Create((object)__instance.mouseHoverPlanet).Field("nameText").GetValue<Text>().text = name;
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool SetStarName(UIStarmap __instance, string name)
        {
            ModDebug.Assert((__instance.mouseHoverStar != null && __instance.mouseHoverStar.star != null));
            if (__instance.mouseHoverStar != null && 
                __instance.mouseHoverStar.star != null)
            {
                __instance.mouseHoverStar.star.overrideName = name;
                Traverse.Create((object)__instance.mouseHoverStar).Field("nameText").GetValue<Text>().text = name;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Reset()
        {
            target.Reset();
        }
    }
}
