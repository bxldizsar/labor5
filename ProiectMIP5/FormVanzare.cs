using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProiectMIP5
{
    // cerinta 5 - form distinct pt vanzarea de produse
    public partial class FormVanzare : Form
    {
        private int idProdus;

        // cerinta 5 - constructorul primeste detaliile inregistrarii
        // gets the record from the main form (the row that was double clicked)
        public FormVanzare(Produs p)
        {
            InitializeComponent();

            idProdus = p.Id;
            lblIdVal.Text = p.Id.ToString();
            lblDenumireVal.Text = p.Denumire;
            lblDescriereVal.Text = p.Descriere;
            lblExpirareVal.Text = p.DataExpirare.ToString("dd.MM.yyyy");
            lblDisponibilVal.Text = p.Cantitate.ToString();

            // cant sell more than what we have
            numCantitate.Minimum = 1;
            numCantitate.Maximum = p.Cantitate;
            numCantitate.Value = 1;
        }

        private void btnVinde_Click(object sender, EventArgs e)
        {
            int cantitate = (int)numCantitate.Value;
            btnVinde.Enabled = false;

            // cerinta 6 - vanzarea pe thread pool + Invoke la final
            Task.Run(() =>
            {
                try
                {
                    bool sters = false;

                    using (MagazinDbContext db = new MagazinDbContext())
                    {
                        // read it again, maybe it changed since the grid was loaded
                        Produs p = db.Produse.Find(idProdus);
                        if (p == null)
                            throw new Exception("Produsul nu mai exista in baza de date!");
                        if (p.Cantitate < cantitate)
                            throw new Exception("Stoc insuficient! Disponibil: " + p.Cantitate);

                        p.Cantitate -= cantitate;

                        // cerinta 5 - daca cantitatea ajunge la 0, produsul se sterge
                        // nothing left -> delete it from the db
                        if (p.Cantitate == 0)
                        {
                            db.Produse.Remove(p);
                            sters = true;
                        }

                        db.SaveChanges();
                    }

                    this.Invoke(new Action(() =>
                    {
                        if (sters)
                            MessageBox.Show("Vanzare reusita. Produsul s-a terminat si a fost sters.");
                        else
                            MessageBox.Show("Vanzare reusita.");
                        DialogResult = DialogResult.OK;
                    }));
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show(ex.Message, "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        btnVinde.Enabled = true;
                    }));
                }
            });
        }
    }
}
