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
            List<int> ciscenjeZadaci = new List<int>();
            List<int> minibarZadaci = new List<int>();
            DateTime poslednjiUpdate = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(40);
            string racunCeo = "----------------------\n";
           

            while (true)
            {
                byte[] prijemniBafer = new byte[1024];
                if (DateTime.Now - poslednjiUpdate > interval)
                {
                    poslednjiUpdate = DateTime.Now;

                    foreach (Apartman ap in apartmani)
                    {
                        if (ap.BrojNocenja > 0)
                        {
                            ap.BrojNocenja--;
                            Console.WriteLine($"Apartman {ap.BrojAp} ima još {ap.BrojNocenja} noćenja.");

                            if (ap.BrojNocenja == 0)
                            {
                                ap.Stanje = Stanje.PRAZAN;
                                ap.Gosti.Clear();
                                ap.TrenutniBrGostiju = 0;

                                if (!ciscenjeZadaci.Contains(ap.BrojAp))
                                    ciscenjeZadaci.Add(ap.BrojAp);
                                // Slanje poruke gostu sa računom
                                if (apartmaniPoKlijentu.ContainsValue(ap.BrojAp))
                                {
                                    var klijentEP = apartmaniPoKlijentu.FirstOrDefault(p => p.Value == ap.BrojAp).Key;
                                    string racun = $"Boravak završen. Ukupni troškovi su: {ap.Troskovi} EUR. \n{racunCeo} ----------------------";
                                    byte[] racunBytes = Encoding.UTF8.GetBytes(racun);
                                    serverSocketUDP.SendTo(racunBytes, klijentEP);
                                }


                                Console.WriteLine($"Apartman {ap.BrojAp} je sada slobodan i dodat je zadatak čišćenja.");
                            }
                        }
                    }
                }

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
                            if (porukaUDP.StartsWith("rezervisi;"))
                            {
                                string trazenaKlasa = podaci[1].Trim();
                                Klasa k;
                                if (trazenaKlasa == "I")
                                    k = Klasa.I;
                                else if (trazenaKlasa == "II")
                                    k = Klasa.II;
                                else
                                    k = Klasa.III;
                                int trazeniBrojGostiju = int.Parse(podaci[2]);

                                Console.WriteLine($"Klijent traži apartman klase '{trazenaKlasa}' za {trazeniBrojGostiju} gostiju.");

                                List<Apartman> slobodniApartmani = apartmani
                                    .Where(ap => ap.Stanje == Stanje.PRAZAN &&
                                                 ap.Klasa == k &&
                                                 ap.MaxBrGostiju >= trazeniBrojGostiju)
                                    .ToList();

                                if (slobodniApartmani.Count == 0)
                                {
                                    string nema = "Nema dostupnih apartmana za tražene uslove.";
                                    byte[] odgovor = Encoding.UTF8.GetBytes(nema);
                                    serverSocketUDP.SendTo(odgovor, posiljaocEP);
                                }
                                else
                                {
                                    StringBuilder sb = new StringBuilder();
                                    sb.AppendLine("Dostupni apartmani:");
                                    foreach (var ap in slobodniApartmani)
                                    {
                                        sb.AppendLine($"Broj: {ap.BrojAp}, Max gostiju: {ap.MaxBrGostiju}");
                                    }

                                    byte[] odgovor = Encoding.UTF8.GetBytes(sb.ToString());
                                    serverSocketUDP.SendTo(odgovor, posiljaocEP);
                                }
                            }

                            else if (porukaUDP.StartsWith("rez;"))
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
                                            racunCeo += "APARTMAN:                   " + apartmani[i].Troskovi + "\n";
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

                                gosti.Clear();
                                Console.WriteLine("Primljeni gosti:");
                                foreach(Apartman ap in apartmani)
                                {
                                    if(ap.BrojAp == brAp)
                                    {
                                        foreach (Gost g in ap.Gosti)
                                        {
                                            Console.WriteLine($"Ime: {g.Ime}, Prezime: {g.Prezime}, Pol: {g.Pol}, Dat. rodj: {g.DatRodj}, Pasos: {g.BrPasosa}");
                                        }
                                    }
                                }
                               
                                odgovorGostu = "Podaci su uspesno primljeni! Unesite broj noćenja:";
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
                            else if (porukaUDP.StartsWith("minibar;"))
                            {
                                string korisnickiOdgovor = podaci[1].Trim().ToLower();
                                if (korisnickiOdgovor == "da")
                                {
                                    Console.WriteLine("Odabrano korišćenje minibara.");
                                    if (apartmaniPoKlijentu.ContainsKey((IPEndPoint)posiljaocEP))
                                    {
                                        int brAp = apartmaniPoKlijentu[(IPEndPoint)posiljaocEP];
                                        if (!minibarZadaci.Contains(brAp))
                                        {
                                            minibarZadaci.Add(brAp);
                                            Console.WriteLine($"Zadatak za minibar za apartman {brAp} dodat.");
                                        }
                                    }
                                }
                            }

                            else if (porukaUDP.StartsWith("nocenja;"))
                            {
                                string[] delovi = porukaUDP.Split(';');
                                int brojAp = int.Parse(delovi[1]);
                                int brojNocenja = int.Parse(delovi[2]);

                                Apartman ap = apartmani.FirstOrDefault(a => a.BrojAp == brojAp);
                                if (ap != null)
                                {
                                    ap.BrojNocenja = brojNocenja;
                                    ap.Troskovi = ap.Troskovi * brojNocenja ;
                                    racunCeo += "BROJ NOCENJA:                 " + brojNocenja + "\n";
                                    Console.WriteLine($"Postavljen broj noćenja {brojNocenja} za apartman {brojAp}.");

                                    string potvrda = "Broj noćenja je evidentiran.";
                                    byte[] binarnaPotvrda = Encoding.UTF8.GetBytes(potvrda);
                                    serverSocketUDP.SendTo(binarnaPotvrda, posiljaocEP);
                                }
                                else
                                {
                                    string greska = "Apartman nije pronađen.";
                                    byte[] binarnaGreska = Encoding.UTF8.GetBytes(greska);
                                    serverSocketUDP.SendTo(binarnaGreska, posiljaocEP);
                                }
                                // nakon što se pošalje potvrda o noćenjima
                                string porukaAlarm = "Da li zelite da aktivirate alarm?";
                                byte[] binAlarm = Encoding.UTF8.GetBytes(porukaAlarm);
                                serverSocketUDP.SendTo(binAlarm, posiljaocEP);

                                string porukaMinibar = "Da li želite da koristite minibar?";
                                byte[] binMinibar = Encoding.UTF8.GetBytes(porukaMinibar);
                                serverSocketUDP.SendTo(binMinibar, posiljaocEP);

                            }

                            else if (porukaUDP.StartsWith("kartica;"))
                            {
                                string brojKartice = podaci[1];
                                Console.WriteLine($"Uneta kartica za plaćanje: {brojKartice}");

                                string potvrda = "Plaćanje uspešno. Hvala na korišćenju naših usluga!";
                                byte[] potvrdaBytes = Encoding.UTF8.GetBytes(potvrda);
                                serverSocketUDP.SendTo(potvrdaBytes, posiljaocEP);
                            }


                        }
                        else if (s == serverSocketTCP) // ako je stiglo na tcp
                        {
                            Socket noviKlijent = serverSocketTCP.Accept();
                            noviKlijent.Blocking = false;
                            tcpKlijenti.Add(noviKlijent);

                            string zadatak = "nema zadataka";
                            if (alarmZadaci.Count > 0)
                                zadatak = $"Aktiviraj alarm;{alarmZadaci[0]}";
                            else if (ciscenjeZadaci.Count > 0)
                                zadatak = $"Ocisti apartman;{ciscenjeZadaci[0]}";
                            else if (minibarZadaci.Count > 0)
                                zadatak = $"Obnovi minibar;{minibarZadaci[0]}";

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


                    List<Socket> zaUklanjanje = new List<Socket>();

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
                                zaUklanjanje.Add(klijent);
                                continue;
                            }

                            string porukaTCP = Encoding.UTF8.GetString(prijemniBafer, 0, brBajtaTCP);
                            Console.WriteLine($"TCP poruka od {klijent.RemoteEndPoint}: {porukaTCP}");
                            if (porukaTCP.StartsWith("potvrdjujem;"))
                            {
                                string[] delovi = porukaTCP.Split(';');
                                string tipZadatka = delovi[1].ToLower();
                                int brojAp = int.Parse(delovi[2]);

                                Apartman ap = apartmani.FirstOrDefault(a => a.BrojAp == brojAp);
                                if (ap != null)
                                {
                                    if (tipZadatka == "alarm")
                                    {
                                        ap.Alarm = Alarm.AKTIVIRANO;
                                        ap.Troskovi += 20;
                                        racunCeo += "KORISCENJE ALARMA:         20\n";
                                        alarmZadaci.Remove(brojAp);
                                       // Console.WriteLine($"alarmZadaci count: {alarmZadaci.Count}");

                                    }
                                    else if (tipZadatka == "ciscenje")
                                    {
                                        ap.Stanje = Stanje.PRAZAN;
                                        ap.Gosti.Clear();
                                        ap.TrenutniBrGostiju = 0;
                                        ciscenjeZadaci.Remove(brojAp);
                                        //Console.WriteLine($"ciscenjeZadaci count: {ciscenjeZadaci.Count}");
                                    }
                                    else if (tipZadatka == "minibar")
                                    {
                                        ap.Troskovi += 50;
                                        racunCeo += "KORISCENJE MINIBARA:      50\n";
                                        minibarZadaci.Remove(brojAp);
                                    }

                                    Console.WriteLine($"Zadatak '{tipZadatka}' za apartman {brojAp} je izvršen.");
                                    klijent.Send(Encoding.UTF8.GetBytes("Zadatak izvršen."));
                                }
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
                            if (ex.SocketErrorCode == SocketError.ConnectionAborted || ex.SocketErrorCode == SocketError.ConnectionReset)
                            {
                                Console.WriteLine("Konekcija klijenta je zatvorena.");
                            }
                            else
                            {
                                Console.WriteLine($"Greška u TCP komunikaciji: {ex.Message}");
                            }

                            klijent.Close();
                            zaUklanjanje.Add(klijent);
                        }

                    }

                    foreach (var klijentZaUklanjanje in zaUklanjanje)
                    {
                        tcpKlijenti.Remove(klijentZaUklanjanje);
                    }

                }
            }
            }
        }

}