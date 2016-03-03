using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using WCFConsoleApp.ServiceReference1;

namespace WCFConsoleApp
{
   class Program
   {

      static void Main(string[] args)
      {
         var user = WindowsIdentity.GetCurrent();

         if (user != null) Console.WriteLine($"Welcome {user.Name}");
         Console.WriteLine("Connecting to service.");
         var client = new SlipMapServiceClient();
         client.Open();
         Console.WriteLine("Connected");
         Console.WriteLine(client.GetData(2));
         Console.WriteLine(client.UpdateStarSystem(new StarSystem()));
         Console.ReadLine();
         client.Close();
         Console.WriteLine("ConnectionClosed");
      }
   }
}
