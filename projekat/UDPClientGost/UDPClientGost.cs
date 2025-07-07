using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UDPClientGost
{
    internal class UDPClientGost
    {
        static void Main(string[] args)
        {
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint destinationEP = new IPEndPoint(IPAddress.Parse("192.168.1.7"), 15001);

            EndPoint posiljaocEP = new IPEndPoint(IPAddress.Any, 0);

            while(true)
            {
                byte[] prijemniBafer = new byte[1024];
                Console.WriteLine("Popunite podatke o zahtevu za rezervaciju(ili kraj za izlaz):");
                Console.WriteLine("Broj apartmana: ");
                string brAp = Console.ReadLine();
                if (brAp.ToLower() == "kraj")
                    break;

                Console.WriteLine("Broj gostiju: ");
                string brGost = Console.ReadLine();
                Console.WriteLine("Broj noci: ");
                string brN = Console.ReadLine();
                string rezervacija = brAp+";"+brGost+";"+brN+";";


                byte[] binarnaPoruka = Encoding.UTF8.GetBytes(rezervacija);

                try
                {
                    int brBajta = clientSocket.SendTo(binarnaPoruka, 0, binarnaPoruka.Length, SocketFlags.None, destinationEP); // Poruka koju saljemo u binarnom zapisu, pocetak poruke, duzina, flegovi, odrediste
                    Console.WriteLine($"Uspesno poslato {brBajta} ka {destinationEP}");
                    Console.WriteLine("Ceka se odgovor.");

                    brBajta = clientSocket.ReceiveFrom(prijemniBafer, ref posiljaocEP);
                    string odgovor = Encoding.UTF8.GetString(prijemniBafer, 0, brBajta);
                    Console.WriteLine($"Stigao je odgovor od {posiljaocEP}, poruka:\n{odgovor}");
        
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Doslo je do greske tokom slanja poruke: \n{ex}");
                }
            }
            Console.WriteLine("Gost zavrsava sa radom");
            clientSocket.Close(); // Zatvaramo soket na kraju rada
            Console.ReadKey();
        }
    }
}
