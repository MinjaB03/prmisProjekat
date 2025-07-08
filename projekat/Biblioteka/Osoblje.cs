using System;

namespace Biblioteka
{
    public enum Funkcija { CISCENJE_APARTMANA, SANACIJA_ALARMA, UPRAVLJANJE_MINIBAROM}

    [Serializable]
    public class Osoblje
    {
        public int Id { get; set; }
        public string Ime { get; set; }
        public string Prezime { get; set; }
        public string Pol { get; set; }
        public Funkcija Funkcija { get; set; }
    }
}
