using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Neo4JTest.Model
{
    public class Guide
    {
        public int id { get; set; }
        public string naziv { get; set; }
        public string opis { get; set; }
        [Range(1, 10, ErrorMessage = "Minimalna vrednost je 1, maksimalna je 10")]
        public int kondTezina { get; set; }
        [Range(1,10,ErrorMessage = "Minimalna vrednost je 1, maksimalna je 10")]
        public int tehTezina { get; set; }
        public int duzinaTrase { get; set; }
        public int visinskaRazlika { get; set; }
        public string datumKreiranja { get; set; }
        public List<Image> images { get; set; }

    }
}
