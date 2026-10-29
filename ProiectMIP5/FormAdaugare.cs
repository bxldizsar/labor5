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
    // cerinta 4 - form distinct pt adaugarea de produse
    public partial class FormAdaugare : Form
    {
        // false = new product, true = we only add quantity to an existing one
        private bool produsExistent;

        // new product
        public FormAdaugare()
        {
            InitializeComponent();
            produsExistent = false;

            Text = "Adaugare produs nou";
            txtId.Text = "(automat)";
            txtId.Enabled = false;
            dtpIntrare.Value = DateTime.Today;
            dtpExpirare.Value = DateTime.Today.AddDays(30);
        }

        // cerinta 4 - produs existent: id-ul se citeste din form-ul principal
        // quantity for an existing product, the id comes from the main form
        public FormAdaugare(int id)
        {
            InitializeComponent();
            produsExistent = true;

            Text = "Adaugare cantitate la produs existent";
            if (id > 0)
                txtId.Text = id.ToString();

            // the other fields dont matter here
            txtDenumire.Enabled = false;
            txtDescriere.Enabled = false;
            dtpIntrare.Enabled = false;
            dtpExpirare.Enabled = false;
            lblCantitate.Text = "Cantitate de adaugat:";
        }

        private void btnSalvare_Click(object sender, EventArgs e)
        {
            int id = 0;
            if (produsExistent && !int.TryParse(txtId.Text, out id))
            {
                MessageBox.Show("Id-ul trebuie sa fie un numar!");
                return;
            }

            string denumire = txtDenumire.Text.Trim();
            string descriere = txtDescriere.Text.Trim();
            DateTime intrare = dtpIntrare.Value.Date;
            DateTime expirare = dtpExpirare.Value.Date;
            int cantitate = (int)numCantitate.Value;

            if (!produsExistent)
            {
                if (denumire == "")
                {
                    MessageBox.Show("Introduceti denumirea produsului!");
                    return;
                }
                if (expirare < intrare)
                {
                    MessageBox.Show("Data expirarii nu poate fi inainte de data intrarii!");
                    return;
                }
            }

            btnSalvare.Enabled = false;

            // cerinta 6 - salvarea pe thread pool + Invoke la final
            // db work on the thread pool, the form doesnt freeze
            Task.Run(() =>
            {
                try
                {
                    using (MagazinDbContext db = new MagazinDbContext())
                    {
                        if (produsExistent)
                        {
                            Produs p = db.Produse.Find(id);
                            if (p == null)
                                throw new Exception("Nu exista produs cu Id-ul " + id + "!");

                            p.Cantitate += cantitate;
                        }
                        else
                        {
                            Produs p = new Produs();
                            p.Denumire = denumire;
                            p.Descriere = descriere;
                            p.DataIntrare = intrare;
                            p.DataExpirare = expirare;
                            p.Cantitate = cantitate;
                            db.Produse.Add(p);
                        }

                        db.SaveChanges();
                    }

                    // tell the gui thread that its done
                    this.Invoke(new Action(() =>
                    {
                        DialogResult = DialogResult.OK;
                    }));
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show(ex.Message, "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        btnSalvare.Enabled = true;
                    }));
                }
            });
        }
    }
}
