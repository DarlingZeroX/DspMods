namespace AutoNavigate
{
    public class StarmapStarPlanet
    {
        public StarData star;
        public UIStarmapStar UIstar;
        public PlanetData planet;
        public int id;
        public string name;

        public StarmapStarPlanet()
        {
            Reset();
        }

        public void Set(UIStarmapStar star)
        {
            planet = (PlanetData)null;
            this.UIstar = star;
            this.star = this.UIstar.star;
            id = star.star.id;
            name = star.star.displayName;
        }

        public void Set(StarData star)
        {
            planet = (PlanetData)null;
            UIstar = (UIStarmapStar)null;
            this.star = star;
            id = star.id;
            name = star.displayName;
        }

        public void Set(PlanetData planet)
        {
            this.planet = planet;
            UIstar = (UIStarmapStar)null;
            star = (StarData)null;
            id = planet.id;
            name = planet.displayName;
        }

        public void Reset()
        {
            star = (StarData)null;
            UIstar = (UIStarmapStar)null;
            planet = (PlanetData)null;
            id = -1;
            name = string.Empty;
        }

        public bool SetPlanetOverrideName(string overrideName)
        {
            if (planet != (PlanetData)null)
            {
                planet.overrideName = overrideName;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetStarOverrideName(string overrideName)
        {
            if (star != (StarData)null)
            {
                star.overrideName = overrideName;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
