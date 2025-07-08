using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Biblioteka;


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

            serverSocketUDP.Blocking = false; //neblokirajuci rezim
            serverSocketTCP.Blocking = false; //neblokirajuci rezim

            Console.WriteLine($"Server je pokrenut.");

            Dictionary<IPEndPoint, int> apartmaniPoKlijentu = new Dictionary<IPEndPoint, int>();
            List<Socket> tcpKlijenti = new List<Socket>();
            List<Apartman> apartmani = PocetniApartmani.KreirajPocetneApartmane();
            List<Gost> gosti = new List<Gost>();
            BinaryFormatter formatter = new BinaryFormatter();
            EndPoint posiljaocEP = new IPEndPoint(IPAddress.Any, 0);
            List<int> alarmZadaci = new List<int>();

            while (true)
            {
                byte[] prijemniBafer = new byte[1024];
                try
                {
                    List<Socket> readSockets = new List<Socket> { serverSocketUDP, serverSocketTCP}; // dodajem obe uticnice u readSockets listu

                    Socket.Select(readSockets, null, null, 1000); // proveravaj na svaku sekundu da li su stigle poruke

                    foreach (Socket s in readSockets) // prodji kroz listu onih gde su stigle
                    {
                        if (s == serverSocketUDP) // ako je stiglo na udp
                        {
                            int brBajtaUDP = serverSocketUDP.ReceiveFrom(prijemniBafer, ref posiljaocEP);
                            string porukaUDP = Encoding.UTF8.GetString(prijemniBafer, 0, brBajtaUDP);
                            string[] podaci = porukaUDP.Split(';');

                            if (porukaUDP.StartsWith("rez;"))
                            {
                                int brAp = int.Parse(podaci[1]);
                                int brG = int.Parse(podaci[2]);
                                // pamti koji klijent rezerviše koji apartman
                                apartmaniPoKlijentu[(IPEndPoint)posiljaocEP] = brAp;

                                string odgovorGostu = "";
                                for (int i = 0; i < apartmani.Count; i++)
                                {
                                    if (brAp == apartmani[i].BrojAp)
                                    {
                                        if (apartmani[i].Stanje == Stanje.PRAZAN && apartmani[i].MaxBrGostiju >= brG)
                                        {
                                            odgovorGostu = "Rezervacija primljena";
                                            apartmani[i].Stanje = Stanje.ZAUZET;
                                            apartmani[i].TrenutniBrGostiju = brG;
                                        }
                                        else
                                            odgovorGostu = "Rezervacija odbijena";
                                    }
                                }
                                if (odgovorGostu == "")
                                    odgovorGostu = "Ne postoji apartman sa tim brojem.";

                                Console.WriteLine($"Nova poruka od {posiljaocEP}: {porukaUDP}");

                                byte[] binarniOdgovor = Encoding.UTF8.GetBytes(odgovorGostu);
                                serverSocketUDP.SendTo(binarniOdgovor, posiljaocEP);

                                for (int i = 0; i < brG;)
                                {
                                    List<Socket> udpCheck = new List<Socket>() { serverSocketUDP };
                                    Socket.Select(udpCheck, null, null, 1000000);
                                    if (udpCheck.Count > 0)
                                    {
                                        int brBajta = serverSocketUDP.ReceiveFrom(prijemniBafer, ref posiljaocEP);
                                        using (MemoryStream ms = new MemoryStream(prijemniBafer, 0, brBajta))
                                        {
                                            Gost g = (Gost)formatter.Deserialize(ms);
                                            gosti.Add(g);
                                            i++;

                                            foreach (Apartman ap in apartmani)
                                            {
                                                if (ap.BrojAp == brAp)
                                                {
                                                    ap.Gosti.Add(g);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }

                                Console.WriteLine("Primljeni gosti:");
                                foreach (Gost g in gosti)
                                {
                                    Console.WriteLine($"Ime: {g.Ime}, Prezime: {g.Prezime}, Pol: {g.Pol}, Dat. rodj: {g.DatRodj}, Pasos: {g.BrPasosa}");
                                }

                                odgovorGostu = "Podaci su uspesno primljeni! Da li zelite da aktivirate alarm?";
                                binarniOdgovor = Encoding.UTF8.GetBytes(odgovorGostu);
                                serverSocketUDP.SendTo(binarniOdgovor, posiljaocEP);
                            }
                            else if (porukaUDP.StartsWith("alarm;"))
                            {
                                string korisnickiOdgovor = podaci[1].Trim().ToLower();
                                if (korisnickiOdgovor == "da")
                                {
                                    Console.WriteLine("Odabrana je aktivacija alarma.");
                                    if (apartmaniPoKlijentu.ContainsKey((IPEndPoint)posiljaocEP))
                                    {
                                        int brAp = apartmaniPoKlijentu[(IPEndPoint)posiljaocEP];
                                        if (!alarmZadaci.Contains(brAp))
                                        {
                                            alarmZadaci.Add(brAp);
                                            Console.WriteLine($"Zadatak za aktivaciju alarma za apartman {brAp} dodat.");
                                        }
                                    }
                                }
                            }

                        }
                        else if (s == serverSocketTCP) // ako je stiglo na tcp
                        {
                            Socket noviKlijent = serverSocketTCP.Accept();
                            noviKlijent.Blocking = false;
                            tcpKlijenti.Add(noviKlijent);

                            string zadatak = alarmZadaci.Count > 0 ? "Aktiviraj alarm" : "nema zadataka";
                            byte[] binarniZadatak = Encoding.UTF8.GetBytes(zadatak);
                            noviKlijent.Send(binarniZadatak);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                 if (tcpKlijenti.Count > 0)
                {
                    List<Socket> spremniTCP = new List<Socket>(tcpKlijenti);
                    Socket.Select(spremniTCP, null, null, 1000);

                    foreach (Socket klijent in spremniTCP)
                    {
                        try
                        {
                            int brBajtaTCP = klijent.Receive(prijemniBafer);
                            Console.WriteLine($"Primljeno {brBajtaTCP} bajta preko TCP.");

                            if (brBajtaTCP == 0)
                            {
                                Console.WriteLine("Klijent je zatvorio konekciju.");
                                klijent.Close();
                                tcpKlijenti.Remove(klijent);
                                continue;
                            }

                            string porukaTCP = Encoding.UTF8.GetString(prijemniBafer, 0, brBajtaTCP);
                            Console.WriteLine($"TCP poruka od {klijent.RemoteEndPoint}: {porukaTCP}");
                            if (porukaTCP.Trim().ToLower() == "potvrdjujem")
                            {
                                Console.WriteLine($"Broj apartmana u alarmZadaci: {alarmZadaci.Count}");
                                foreach (int brAp in alarmZadaci)
                                {
                                    foreach (Apartman a in apartmani)
                                    {
                                        if (a.BrojAp == brAp)
                                        {
                                            a.Alarm = Alarm.AKTIVIRANO;
                                            Console.WriteLine($"Alarm status za apartman {a.BrojAp} je sada: {a.Alarm}");
                                            Console.WriteLine($"Osoblje je aktiviralo alarm za apartman {a.BrojAp}");
                                            Console.WriteLine("\n--- PODACI O APARTMANU ---");
                                            Console.WriteLine($"Broj apartmana: {a.BrojAp}");
                                            Console.WriteLine($"Stanje: {a.Stanje}");
                                            Console.WriteLine($"Trenutni broj gostiju: {a.TrenutniBrGostiju}");
                                            Console.WriteLine($"Alarm: {a.Alarm}");
                                            Console.WriteLine($"Max broj gostiju: {a.MaxBrGostiju}");
                                            Console.WriteLine("Gosti:");
                                            foreach (Gost g in a.Gosti)
                                            {
                                                Console.WriteLine($"  Ime: {g.Ime}, Prezime: {g.Prezime}, Pol: {g.Pol}, Datum rodj: {g.DatRodj}, Pasos: {g.BrPasosa}");
                                            }
                                            Console.WriteLine("--------------------------\n");

                                        }
                                    }
                                }
                                alarmZadaci.Clear();

                                // Pošalji potvrdu klijentu
                                byte[] potvrda = Encoding.UTF8.GetBytes("Alarm aktiviran");
                                klijent.Send(potvrda);
                            }
                            if (porukaTCP.Trim().ToLower() == "kraj")
                            {
                                Console.WriteLine("Server zavrsava sa radom");
                                serverSocketUDP.Close();
                                serverSocketTCP.Close();
                                Console.ReadKey();
                                Environment.Exit(0);
                            }


                        }
                        catch (SocketException ex)
                        {
                            Console.WriteLine($"Greška u TCP komunikaciji: {ex.Message}");
                        }
                    }
                 }
            }
        }
    }
}
