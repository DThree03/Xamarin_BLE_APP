using System;

namespace Prototype.Fez.BootloaderUtil
{
    internal class Program
    {
        private static int Main()
        {
            var controller = new FezBootloaderController();
            try
            {
                controller.Open();
                Console.WriteLine("[SYSTEM DEBUG]: Check Bootloader version!");
                //Console.WriteLine("Loader version is {0}", controller.GetLoaderVersion());
                Console.WriteLine("[SYSTEM DEBUG]: Load firmware and transfer!");
                controller.LoadFirmware(
                    //@"C:\Program Files (x86)\GHI Electronics\GHI NETMF v4.1 SDK\USBizi\Firmware\USBizi_CLR.GHI");
                    @"C: \Users\kokon\Desktop\TestFW\TestTeraSend.txt");
                Console.WriteLine("All done.");
            }
            catch (FezBootloaderException e)
            {
                Console.WriteLine(e.Message);
                return 1;
            }

            return 0;
        }
    }
}