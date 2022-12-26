namespace TelegramBotCiSharpHelper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                TelegramBotHelper telegramBotHelper = new TelegramBotHelper(token: "5950929953:AAGES3C_z8us1StvDJa02cWoQW4i3f827Fo");
                telegramBotHelper.GetUpdates();
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}