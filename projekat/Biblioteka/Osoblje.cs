using System;

namespace Biblioteka
{

    [Serializable]
    public class Osoblje
    {
        public int Id { get; set; }
        public string Ime { get; set; }
        public string Prezime { get; set; }
        public string Pol { get; set; }
        public string Funkcija { get; set; }
    }
}
