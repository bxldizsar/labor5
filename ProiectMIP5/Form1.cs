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
    // cerinta 2 - form-ul principal: datagridview cu toate produsele, buton refresh, meniu
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // first time it takes a few sec because ef creates/opens the db
            IncarcaProduse("");
        }

        // cerinta 6 - accesul la baza de date pe thread pool (Task.Run), rezultatul ajunge la gui prin Invoke
        // loads the products on the thread pool, then gives the list back to the gui with Invoke
        private void IncarcaProduse(string filtru)
        {
            lblStatus.Text = "Se incarca datele...";
            btnRefresh.Enabled = false;
            btnCauta.Enabled = false;

            Task.Run(() =>
            {
                try
                {
                    List<Produs> lista;
                    using (MagazinDbContext db = new MagazinDbContext())
                    {
                        var query = from p in db.Produse
                                    select p;

                        // search: only the products that have the text in the name or description
                        if (filtru != "")
                            query = query.Where(p => p.Denumire.Contains(filtru) || p.Descriere.Contains(filtru));

                        lista = query.OrderBy(p => p.Id).ToList();
                    }

                    this.Invoke(new Action(() => AfiseazaProduse(lista)));
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() =>
                    {
                        lblStatus.Text = "Eroare";
                        btnRefresh.Enabled = true;
                        btnCauta.Enabled = true;
                        MessageBox.Show("Eroare la citirea datelor:\n" + ex.Message, "Eroare",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            });
        }

        // this one already runs on the gui thread
        private void AfiseazaProduse(List<Produs> lista)
        {
            dataGridView1.DataSource = lista;

            dataGridView1.Columns["DataIntrare"].HeaderText = "Data intrarii";
            dataGridView1.Columns["DataExpirare"].HeaderText = "Data expirarii";
            dataGridView1.Columns["DataIntrare"].DefaultCellStyle.Format = "dd.MM.yyyy";
            dataGridView1.Columns["DataExpirare"].DefaultCellStyle.Format = "dd.MM.yyyy";
            dataGridView1.Columns["Descriere"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // expired products in red
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                Produs p = (Produs)row.DataBoundItem;
                if (p.DataExpirare.Date < DateTime.Today)
                    row.DefaultCellStyle.BackColor = Color.LightCoral;
            }

            lblStatus.Text = lista.Count + " produse  |  dublu click pe un produs pentru vanzare";
            btnRefresh.Enabled = true;
            btnCauta.Enabled = true;
        }

        private Produs ProdusSelectat()
        {
            if (dataGridView1.CurrentRow == null)
                return null;
            return (Produs)dataGridView1.CurrentRow.DataBoundItem;
        }

        // cerinta 2 - butonul refresh
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            txtCautare.Clear();
            IncarcaProduse("");
        }

        // cerinta 3 - cautarea se face din form-ul principal, ca filtrare in datagridview
        // ---------------- cautare ----------------

        private void btnCauta_Click(object sender, EventArgs e)
        {
            IncarcaProduse(txtCautare.Text.Trim());
        }

        private void txtCautare_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                IncarcaProduse(txtCautare.Text.Trim());
            }
        }

        private void cautareProduseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // search box is on the main form, we just jump there
            txtCautare.Focus();
            txtCautare.SelectAll();
        }

        // cerinta 4 - adaugare: produs nou / cantitate la produs existent (2 sub-meniuri)
        // ---------------- adaugare ----------------

        private void produsNouToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormAdaugare f = new FormAdaugare();
            if (f.ShowDialog() == DialogResult.OK)
                IncarcaProduse("");
        }

        private void produsExistentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // the id comes from the main form (the selected row), but it can be changed in the form
            Produs p = ProdusSelectat();
            int id = 0;
            if (p != null)
                id = p.Id;

            FormAdaugare f = new FormAdaugare(id);
            if (f.ShowDialog() == DialogResult.OK)
                IncarcaProduse("");
        }

        // cerinta 5 - vanzare: form-ul se deschide la dublu click pe datagridview
        // ---------------- vanzare ----------------

        private void DeschideVanzare(Produs p)
        {
            FormVanzare f = new FormVanzare(p);
            if (f.ShowDialog() == DialogResult.OK)
                IncarcaProduse("");
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // -1 = header row
            if (e.RowIndex < 0) return;

            Produs p = (Produs)dataGridView1.Rows[e.RowIndex].DataBoundItem;
            DeschideVanzare(p);
        }

        private void vanzareProduseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Produs p = ProdusSelectat();
            if (p == null)
            {
                MessageBox.Show("Selectati un produs din tabel (sau dublu click pe el)!");
                return;
            }
            DeschideVanzare(p);
        }
    }
}
