using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Biblioteka;



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

            while (true)
            {
                List<Socket> lista = new List<Socket>() { clientSocket };
                Socket.Select(lista, null, null, 1000); 

                if (lista.Count > 0)
                {
                    byte[] prijemniBafer = new byte[1024];
                    int brBajta = clientSocket.Receive(prijemniBafer);
                    string poruka = Encoding.UTF8.GetString(prijemniBafer, 0, brBajta);

                    if (poruka == "nema zadataka")
                    {
                        Console.WriteLine("Trenutno nema zadataka.");
                        break;
                    }

                    string[] podaci = poruka.Split(';');
                    string tip = podaci[0];
                    string brojAp = podaci.Length > 1 ? podaci[1] : "nepoznat";

                    Console.WriteLine($"Dobili ste zadatak: {tip} za apartman {brojAp}");
                    Console.WriteLine("Unesite 'potvrdjujem' kada završite zadatak:");
                    string odgovor = Console.ReadLine()?.Trim().ToLower();

                    if (odgovor == "potvrdjujem")
                    {
                        string porukaZaServer = $"potvrdjujem;{tip.ToLower().Split(' ')[1]};{brojAp}";
                        clientSocket.Send(Encoding.UTF8.GetBytes(porukaZaServer));
                        Console.WriteLine("Poslata potvrda o izvršenju zadatka.");
                        break;
                    }
                }
            }

            Console.WriteLine("Osoblje zavrsava sa radom");
            clientSocket.Close();
            Console.ReadKey();

        }
    }
}
