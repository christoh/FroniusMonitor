namespace De.Hochstaetter.FroniusMonitor.Contracts;

public interface IHaveLcdPanel
{
    public static void SetL123(LcdDisplay lcd, string sumText)
    {
        lcd.Label1 = "L1";
        lcd.Label2 = "L2";
        lcd.Label3 = "L3";
        lcd.LabelSum = sumText;
    }

    public static void SetTwoPhases(LcdDisplay lcd, string sumText)
    {
        lcd.Label1 = "L12";
        lcd.Label2 = "L23";
        lcd.Label3 = "L31";
        lcd.LabelSum = sumText;
    }
}
