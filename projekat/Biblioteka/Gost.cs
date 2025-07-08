using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteka
{
    [Serializable]
    public class Gost
    {
        public string Ime { get; set; }
        public string Prezime { get; set; }
        public string Pol { get; set; }
        public DateTime DatRodj { get; set; }
        public int BrPasosa { get; set; }
    }
}
