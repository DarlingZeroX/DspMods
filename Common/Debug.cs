using UnityEngine;

class ModDebug
{
    public static void Assert(bool condition)
    {
        UnityEngine.Assertions.Assert.IsTrue(condition);
    }

    public static void Log(object message)
    {
        UnityEngine.Debug.Log(message);
    }
    public static void Error(object message)
    {
        UnityEngine.Debug.LogError(message);
    }

    public static void LogPlanetType(PlanetData planet)
    {
        //Gas planet range
        switch (planet.type)
        {
            case EPlanetType.Gas:
                ModDebug.Log("Gas");
                break;
            case EPlanetType.Desert:
                ModDebug.Log("Desert");
                break;
            case EPlanetType.Ice:
                ModDebug.Log("Ice");
                break;
            case EPlanetType.Ocean:
                ModDebug.Log("Ocean");
                break;
            case EPlanetType.Vocano:
                ModDebug.Log("Vocano");
                break;
            case EPlanetType.None:
                ModDebug.Log("None");
                break;
        };
    }

    public static void LogCmdMode(int mode)
    {
        //Gas planet range
        switch (mode)
        {
            case -1:
                ModDebug.Log("CmdMode: Destruct Mode");
                break;
            case -2:
                ModDebug.Log("CmdMode: Upgrade Mode");
                break;
            case 1:
                ModDebug.Log("CmdMode: Normal Build Mode");
                break;
            case 2:
                ModDebug.Log("CmdMode: Build Mode - Belt");
                break;
            case 3:
                ModDebug.Log("CmdMode: Build Mode - Inserter");
                break;        
            case 4:
                ModDebug.Log("CmdMode: Build Mode - Ground");
                break;
        };
    }
}
