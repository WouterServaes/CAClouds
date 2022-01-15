
public static class WindHelper
{

    //not tested
    public static float GetWindSpeedAtHeight(float height, float normalWindSpeed)
    {
        float windSpeed = normalWindSpeed;
        if(height < 5) return windSpeed / 2;
        if (height < 10) return windSpeed / 1.5f;
        if (height < 15) return windSpeed / 1.75f;
        return windSpeed;
    }

    //not tested
    public static int GetWindSpeedCellDisplacementAtHeight(float height, float normalWindSpeed)
    {
        float windSpeed = GetWindSpeedAtHeight(height, normalWindSpeed);
        
        if (windSpeed < 5) return 0;
        if (windSpeed < 10) return 1;
        if (windSpeed < 15) return 2;
        return 3;
    }

    public static int GetWindSpeedCellDisplacementAtHeight()
    {
        return 1;
    }
}
