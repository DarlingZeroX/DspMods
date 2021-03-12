using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class ModTranslate
{
    static Dictionary<string, string> enusDict = new Dictionary<string, string>
    {
    //StellarAutoNavigation
        { "驱动引擎等级过低","Thruster Level Too Low" },
        { "机甲能量过低","Mecha Energy Too Low" },
        { "星际自动导航","Stellar Auto Navigation" },
        { "星系自动导航","Galaxy Auto Navigation" },
        { "导航模式结束","Navigation Mode Ended" },
    //BetterStarmap
        { "星图功能","Starmap Features" },
        { "星球细节预览","StarDetailsPreview" },
        { "查看立即模式","ImmediateMod" },
        { "显示星球名称","DisplayStarName" },
        { "探测未知信息","UnknownStarInfo" },
        { "星系显示","Star Filter" },
        { "高光度恒星","HighLuminosityStar" },
        { "黑洞中子星","Blackhole" },
        { "巨星","GiantStar" },
        { "白矮星","WhiteDwarf" },
    };

    public static string ModText(this string text)
    {
        if (Localization.language == Language.zhCN)
        {
            return text;
        }
        else
        {
            return enusDict[text];
        }
    }
}