using System.Globalization;

namespace ChestTracker;

public static class RewardOdds
{
    public const string Source="https://fortnite.gg/treasure-hunt";
    public static int Denominator(string kind)=>kind switch
    {
        "Скин"=>1562500,"Автомобиль" or "Автомобиль и декаль"=>7813,
        "Кирка" or "Украшение на спину"=>568,"Музыкальный трек"=>391,
        "Экран загрузки"=>272,"Жетоны доступа"=>137500,_=>0
    };
    public static double AtLeastOne(long chests,int denominator)
    {
        if(chests<=0||denominator<=0)return 0;
        if(denominator==1)return 1;
        return Math.Clamp(1-Math.Exp(chests*Math.Log(1-1d/denominator)),0,1);
    }
    public static string Percent(double value)
    {
        if(value>=.9999)return ">99,99%";
        double percent=value*100;
        return percent.ToString(percent<.01?"0.0000":percent<1?"0.000":"0.00",CultureInfo.GetCultureInfo("ru-RU"))+"%";
    }
}
