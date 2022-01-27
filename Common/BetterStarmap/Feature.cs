using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Configuration;

public interface IFeature
{
    void DrawGui();
    string Name();
}

public class BaseFeature<T>
{
    private static string mName = "";
    public static bool isEnable = false;
    public static ConfigEntry<bool> configEnable = null;

    protected static void SetFeatureName(string name)
    {
        mName = name;
    }

    public string Name()
    {
        return mName;
    }
}

public class FeaturesManage
{
    public Dictionary<string, IFeature> features = new Dictionary<string, IFeature>();

    public bool AddFeatrue(IFeature feature)
    {
        if (features.ContainsKey(feature.Name()))
        {
            return false;
        }
        else
        {
            features[feature.Name()] = feature;
            return true;
        }
    }

    public IFeature this[string name]
    {
        get
        {
            return features[name];
        }
    }

}
