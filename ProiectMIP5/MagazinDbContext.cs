using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProiectMIP5
{
    // cerinta 1 - contextul ef (baza de date in localdb, creata automat - code first)
    public class MagazinDbContext : DbContext
    {
        // connection string is in App.config (same name as the class)
        public DbSet<Produs> Produse { get; set; }
    }
}
