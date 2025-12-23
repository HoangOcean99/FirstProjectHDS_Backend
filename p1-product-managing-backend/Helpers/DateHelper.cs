public class DateHelper()
{
    public static string formatNumbertoDateString(int date)
    {
        string dateString = date.ToString();
        return dateString.Substring(0, 4) + "/" + dateString.Substring(4, 2) + "/" + dateString.Substring(6, 2);
    }
    public static string FormatNumber(long number)
    {
        return number.ToString("#,##0");
    }

}