using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProiectMIP5
{
    // cerinta 1 - modelul de date entity framework pt produsele din magazin
    public class Produs
    {
        // Id -> ef makes it the primary key + identity automatically
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string Denumire { get; set; }

        [MaxLength(500)]
        public string Descriere { get; set; }

        public DateTime DataIntrare { get; set; }

        public DateTime DataExpirare { get; set; }

        public int Cantitate { get; set; }
    }
}
