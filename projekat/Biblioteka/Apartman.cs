using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteka
{
    public enum Klasa { I, II, III }
    public enum Stanje { PRAZAN, ZAUZET, POTREBNO_CISCENJE }
    public enum Alarm { NORMALNO, AKTIVIRANO }

    [Serializable]
    public class Apartman
    {
        public int BrojAp { get; set; }
        public Klasa Klasa { get; set; }
        public int MaxBrGostiju { get; set; }
        public int TrenutniBrGostiju { get; set; }
        public Stanje Stanje { get; set; }
        public Alarm Alarm { get; set; }
        public List<Gost> Gosti { get; set; } = new List<Gost>();
        public int BrojNocenja {  get; set; }
    }
}
