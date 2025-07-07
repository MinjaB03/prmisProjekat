using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;


namespace TCPClientOsoblje
{
    internal class TCPClientOsoblje
    {
        static void Main(string[] args)
        {
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse("192.168.1.7"), 15002);

            Console.WriteLine("Klijent je spreman za povezivanje sa serverom, kliknite enter");
            Console.ReadKey();
            clientSocket.Connect(serverEP);
            Console.WriteLine("Klijent je uspesno povezan sa serverom!");

            //EndPoint posiljaocEP = new IPEndPoint(IPAddress.Any, 0);

            while (true)
            {
                byte[] prijemniBafer = new byte[1024];

                try
                {
                    int brBajta = clientSocket.Receive(prijemniBafer);
                    string poruka = Encoding.UTF8.GetString(prijemniBafer, 0, brBajta);

                    if (poruka == "nema zadataka")
                    {
                        Console.WriteLine("Trenutno nema zadataka.");
                        break;
                    }
                        
                    Console.WriteLine($"Dobili ste zadatak: {poruka}");

                    Console.WriteLine("Posaljite potvrdu o izvrsenom zadatku.");
                    string odgovor = Console.ReadLine();
                    int brBajtaOdg = clientSocket.Send(Encoding.UTF8.GetBytes(odgovor));
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Doslo je do greske tokom slanja poruke: \n{ex}");
                }
            }
            Console.WriteLine("Osoblje zavrsava sa radom");
            clientSocket.Close(); // Zatvaramo soket na kraju rada
            Console.ReadKey();
        
        }
    }
}
