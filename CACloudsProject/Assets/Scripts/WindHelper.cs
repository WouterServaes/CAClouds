
public static class WindHelper
{
    private static WindSettings _WindSettings;

    public static float GetWindSpeedAtHeight(float height)
    {
        float windSpeed = _WindSettings.WindSpeed;
        if(height < 5) return windSpeed / 2;
        if (height < 10) return windSpeed / 1.5f;
        if (height < 15) return windSpeed / 1.75f;
        return windSpeed;
    }

    public static int GetWindSpeedCellDisplacementAtHeight(float height)
    {
        //float windSpeed = GetWindSpeedAtHeight(height);
        return 1;
        //if (windSpeed < 5) return 0;
        //if (windSpeed < 10) return 1;
        //if (windSpeed < 15) return 2;
        //return 3;
    }
}
