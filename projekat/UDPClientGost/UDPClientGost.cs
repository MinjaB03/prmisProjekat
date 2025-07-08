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
           // List<Gost> gosti = new List<Gost>();
            BinaryFormatter formatter = new BinaryFormatter();

            
                byte[] prijemniBafer = new byte[1024];
                Console.WriteLine("---------HOTEL---------\n Odaberite 1 - za pokusaj rezervacije ili 2 - za izlaz:");
                if (Console.ReadLine() == "2")
                {
                    Console.WriteLine("Gost zavrsava sa radom");
                    clientSocket.Close(); // Zatvaramo soket na kraju rada
                    Console.ReadKey();
                }
                Console.WriteLine("Odabrali ste POKUSAJ REZERVACIJE");
                Console.WriteLine("Broj gostiju: ");
                int brG = int.Parse(Console.ReadLine());
                Console.WriteLine("Broj apartmana: ");
                int brAp = int.Parse(Console.ReadLine());
                Console.WriteLine("Broj nocenja: ");
                int brN = int.Parse(Console.ReadLine());
                string rezervacija = "rez;" + brAp + ";" + brG + ";" + brN + ";";

            try
            {
                    byte[] binarnaPoruka = Encoding.UTF8.GetBytes(rezervacija);
                    int brBajta = clientSocket.SendTo(binarnaPoruka, 0, binarnaPoruka.Length, SocketFlags.None, destinationEP); // Poruka koju saljemo u binarnom zapisu, pocetak poruke, duzina, flegovi, odrediste
                    Console.WriteLine($"Uspesno poslata rezervacija");
                    Console.WriteLine("Ceka se odgovor.");

                    brBajta = clientSocket.ReceiveFrom(prijemniBafer, ref posiljaocEP);
                    string odgovor = Encoding.UTF8.GetString(prijemniBafer, 0, brBajta);
                    Console.WriteLine($"Stigao je odgovor! Poruka:\n{odgovor}");

                    if(odgovor == "Rezervacija primljena")
                    {
                        Console.WriteLine("Posaljite podatke o gostima.");
                        for (int i = 0; i < brG; i++)
                        {
                            Gost gost = new Gost();
                            Console.WriteLine($"Unesite podatke o {i+1}. gostu: \n");
                            Console.WriteLine("Ime: ");
                            gost.Ime = Console.ReadLine();
                            Console.WriteLine("Prezime: ");
                            gost.Prezime = Console.ReadLine();
                            Console.WriteLine("Pol (Z ili M): ");
                            gost.Pol = Console.ReadLine();
                            Console.WriteLine("Datum rodjenja (format: yyyy-mm-dd): ");
                            gost.DatRodj = DateTime.Parse(Console.ReadLine());
                            Console.WriteLine("Broj pasosa: ");
                            gost.BrPasosa = int.Parse(Console.ReadLine());

                            using (MemoryStream ms = new MemoryStream())
                            {
                                formatter.Serialize(ms, gost);
                                byte[] data = ms.ToArray();
                                clientSocket.SendTo(data, 0, data.Length, SocketFlags.None, destinationEP);
                            }
                        }
                       

                        brBajta = clientSocket.ReceiveFrom(prijemniBafer, ref posiljaocEP);
                        string odgovor2 = Encoding.UTF8.GetString(prijemniBafer, 0, brBajta);
                    Console.WriteLine("Da li zelite da aktivirate alarm?(da ili ne)");
                    string odg = Console.ReadLine(); // "da" ili "ne"
                    string alarmPoruka = "alarm;" + odg;
                    byte[] odgB = Encoding.UTF8.GetBytes(alarmPoruka);
                    clientSocket.SendTo(odgB, 0, odgB.Length, SocketFlags.None, destinationEP);

                }
                else
                    {
                    Console.WriteLine("Gost zavrsava sa radom");
                    clientSocket.Close(); // Zatvaramo soket na kraju rada
                    Console.ReadKey();
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Doslo je do greske tokom slanja poruke: \n{ex}");
                }
            
            Console.WriteLine("Gost zavrsava sa radom");
            clientSocket.Close(); // Zatvaramo soket na kraju rada
            Console.ReadKey();
        }
    }
}
