using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Biblioteka;


namespace UDPClientGost
{
    internal class UDPClientGost
    {
        static void Main(string[] args)
        {
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint destinationEP = new IPEndPoint(IPAddress.Parse("192.168.1.7"), 15001);

            EndPoint posiljaocEP = new IPEndPoint(IPAddress.Any, 0);
            BinaryFormatter formatter = new BinaryFormatter();

            
            byte[] prijemniBafer = new byte[1024];
            Console.WriteLine("---------HOTEL---------\n Odaberite 1 - za pokusaj rezervacije ili 2 - za izlaz:");
            if (Console.ReadLine() == "2")
            {
                Console.WriteLine("Gost zavrsava sa radom");
                clientSocket.Close(); 
                Console.ReadKey();
            }
            Console.WriteLine("Odabrali ste POKUSAJ REZERVACIJE");
            Console.WriteLine("Unesite klasu apartmana (I, II, III): ");
            string klasa = Console.ReadLine().Trim();
            Console.WriteLine("Unesite broj gostiju: ");
            int brojGostiju = int.Parse(Console.ReadLine());
            string rezervacija = $"rezervisi;{klasa};{brojGostiju};";
            try
            {
                byte[] binarnaPoruka = Encoding.UTF8.GetBytes(rezervacija);
                int brBajta = clientSocket.SendTo(binarnaPoruka, 0, binarnaPoruka.Length, SocketFlags.None, destinationEP); 
                Console.WriteLine($"Uspesno poslata rezervacija");
                Console.WriteLine("Ceka se odgovor.");

                brBajta = clientSocket.ReceiveFrom(prijemniBafer, ref posiljaocEP);
                string odgovor = Encoding.UTF8.GetString(prijemniBafer, 0, brBajta);
                Console.WriteLine($"Stigao je odgovor! Poruka:\n{odgovor}");
                Console.WriteLine("Unesite broj apartmana koji želite da rezervišete:");
                int izabraniBrojAp = int.Parse(Console.ReadLine());

                string potvrdaRezervacije = $"rez;{izabraniBrojAp};{brojGostiju}";
                byte[] binarnaPotvrda = Encoding.UTF8.GetBytes(potvrdaRezervacije);
                clientSocket.SendTo(binarnaPotvrda, destinationEP);

                brBajta = clientSocket.ReceiveFrom(prijemniBafer, ref posiljaocEP);
                string odgovor2 = Encoding.UTF8.GetString(prijemniBafer, 0, brBajta);
                Console.WriteLine($"Server: {odgovor2}");

                if (odgovor2 == "Rezervacija primljena")
                {
                    Console.WriteLine("Posaljite podatke o gostima.");
                    for (int i = 0; i < brojGostiju; i++)
                    {
                        Gost gost = new Gost();
                        Console.WriteLine($"Unesite podatke o {i + 1}. gostu:");
                        Console.Write("Ime: ");
                        gost.Ime = Console.ReadLine();
                        Console.Write("Prezime: ");
                        gost.Prezime = Console.ReadLine();
                        Console.Write("Pol (Z ili M): ");
                        gost.Pol = Console.ReadLine();
                        Console.Write("Datum rodjenja (yyyy-mm-dd): ");
                        gost.DatRodj = DateTime.Parse(Console.ReadLine());
                        Console.Write("Broj pasosa: ");
                        gost.BrPasosa = int.Parse(Console.ReadLine());

                        using (MemoryStream ms = new MemoryStream())
                        {
                            formatter.Serialize(ms, gost);
                            byte[] podaci = ms.ToArray();
                            clientSocket.SendTo(podaci, destinationEP);
                        }
                    }
                    // potvrda i trazenje unosa o br noc
                    brBajta = clientSocket.ReceiveFrom(prijemniBafer, ref posiljaocEP);
                    string zahtevZaNocenja = Encoding.UTF8.GetString(prijemniBafer, 0, brBajta);
                    Console.WriteLine($"Server: {zahtevZaNocenja}");

                    // unos broja noćenja
                    int brojNocenja = int.Parse(Console.ReadLine());

                    string nocenjaPoruka = $"nocenja;{izabraniBrojAp};{brojNocenja}";
                    byte[] binarnaNocenja = Encoding.UTF8.GetBytes(nocenjaPoruka);
                    clientSocket.SendTo(binarnaNocenja, destinationEP);

                    //potvrda o nocenju
                    brBajta = clientSocket.ReceiveFrom(prijemniBafer, ref posiljaocEP);
                    string odgovorNocenja = Encoding.UTF8.GetString(prijemniBafer, 0, brBajta);
                    Console.WriteLine($"Server: {odgovorNocenja}");

                    // alarm 
                    brBajta = clientSocket.ReceiveFrom(prijemniBafer, ref posiljaocEP);
                    string porukaAlarm = Encoding.UTF8.GetString(prijemniBafer, 0, brBajta);
                    Console.WriteLine(porukaAlarm);

                    Console.Write("Odgovor (da/ne): ");
                    string odgovorAlarma = Console.ReadLine();
                    string alarmPoruka = $"alarm;{odgovorAlarma}";
                    byte[] binAlarm = Encoding.UTF8.GetBytes(alarmPoruka);
                    clientSocket.SendTo(binAlarm, destinationEP);

                    // minibar 
                    brBajta = clientSocket.ReceiveFrom(prijemniBafer, ref posiljaocEP);
                    string porukaMinibar = Encoding.UTF8.GetString(prijemniBafer, 0, brBajta);
                    Console.WriteLine(porukaMinibar);

                    Console.Write("Odgovor (da/ne): ");
                    string odgovorMinibar = Console.ReadLine();
                    string minibarPoruka = $"minibar;{odgovorMinibar}";
                    byte[] binMinibar = Encoding.UTF8.GetBytes(minibarPoruka);
                    clientSocket.SendTo(binMinibar, destinationEP);

                    byte[] buffer = new byte[1024];
                    while (true)
                    {
                        int bytesRead = clientSocket.ReceiveFrom(buffer, ref posiljaocEP);
                        string poruka = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        if (poruka.StartsWith("Boravak završen"))
                        {
                            Console.WriteLine(poruka);
                            // prikazi račun iz poruke
                            Console.WriteLine("Unesite broj kreditne kartice:");
                            string kartica = Console.ReadLine();
                            string karticaPoruka = "kartica;" + kartica;
                            byte[] karticaBytes = Encoding.UTF8.GetBytes(karticaPoruka);
                            clientSocket.SendTo(karticaBytes, destinationEP);
                        }
                        else if (poruka.StartsWith("Plaćanje uspešno"))
                        {
                            Console.WriteLine(poruka);
                            break; // izlazak iz petlje, kraj sesije
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Rezervacija nije uspela. Kraj.");
                    clientSocket.Close();
                    Console.ReadKey();
                    return;
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Doslo je do greske tokom slanja poruke: \n{ex}");
            }
            Console.WriteLine("Gost zavrsava sa radom");
            clientSocket.Close(); 
            Console.ReadKey();
        }
    }
}
