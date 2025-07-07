using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;


namespace Server
{
    internal class Server
    {
        static void Main(string[] args)
        {
            Socket serverSocketUDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint serverUDPEP = new IPEndPoint(IPAddress.Any, 15001);
            serverSocketUDP.Bind(serverUDPEP);

            Socket serverSocketTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverTCPEP = new IPEndPoint(IPAddress.Any, 15002);
            serverSocketTCP.Bind(serverTCPEP);
            serverSocketTCP.Listen(10);

            Console.WriteLine($"Server je pokrenut.");

            EndPoint posiljaocEP = new IPEndPoint(IPAddress.Any, 0);

            while (true)
            {
                byte[] prijemniBafer = new byte[1024];
                try
                {
                    int brBajtaUDP = serverSocketUDP.ReceiveFrom(prijemniBafer, ref posiljaocEP);
                    string porukaUDP = Encoding.UTF8.GetString(prijemniBafer, 0, brBajtaUDP);
                    string[] podaci = porukaUDP.Split(";");
                    int brAp = int.Parse(podaci[0]);
                    string odgovorGostu = "";
                    if (brAp < 10)
                    {
                        odgovorGostu = "Rezervacija primljena";
                    }else
                    {
                        odgovorGostu = "Rezervacija odbijena";
                    }
                    if (porukaUDP == "kraj")
                        break;
                    Console.WriteLine($"UDP poruka od {posiljaocEP}: {porukaUDP}");
                    byte[] binarniOdgovor = Encoding.UTF8.GetBytes(odgovorGostu);
                    serverSocketUDP.SendTo(binarniOdgovor, posiljaocEP);


                    Socket tcpKlijent = serverSocketTCP.Accept(); // Čekaj konekciju
                    string zadatak = "Ocistiti apartman broj 3";
                    byte[] binarniZadatak = Encoding.UTF8.GetBytes(zadatak);
                    tcpKlijent.Send(binarniZadatak);

                    int brBajtaTCP = tcpKlijent.Receive(prijemniBafer);
                    string porukaTCP = Encoding.UTF8.GetString(prijemniBafer, 0, brBajtaTCP);
                    if (porukaTCP == "kraj")
                        break;
                    Console.WriteLine($"TCP poruka od {tcpKlijent.RemoteEndPoint}: {porukaTCP}");
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Doslo je do greske tokom prijema poruke: \n{ex}");
                }

            }

            Console.WriteLine("Server zavrsava sa radom");
            serverSocketUDP.Close(); // Zatvaramo soket na kraju rada
            serverSocketTCP.Close();
            Console.ReadKey();
        }
    }
}
