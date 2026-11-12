
using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Delacruz
{
    public partial class FormDashboard : Form
    {
        private string connectionString =
            @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" +
            Path.Combine(Application.StartupPath, "InventoryDB.accdb");

        private int selectedProductID = 0;

        public FormDashboard()
        {
            InitializeComponent();

            btnAdd.Click += btnAdd_Click;
            btnUpdate.Click += btnUpdate_Click;
            btnDelete.Click += btnDelete_Click;
            btnClear.Click += btnClear_Click;
            btnRefresh.Click += btnRefresh_Click;

            txtSearch.TextChanged += txtSearch_TextChanged;
            dgvProducts.CellClick += dgvProducts_CellClick;
        }

        private void FormDashboard_Load(object sender, EventArgs e)
        {
            LoadProducts();
        }

        private void LoadProducts()
        {
            try
            {
                using (OleDbConnection conn =
                    new OleDbConnection(connectionString))
                {
                    conn.Open();

                    string query = "SELECT * FROM Products";

                    if (txtSearch.Text.Trim() != "")
                    {
                        query += " WHERE ProductName LIKE ? OR Category LIKE ?";
                    }

                    OleDbCommand cmd = new OleDbCommand(query, conn);

                    if (txtSearch.Text.Trim() != "")
                    {
                        cmd.Parameters.AddWithValue("@p1",
                            "%" + txtSearch.Text.Trim() + "%");

                        cmd.Parameters.AddWithValue("@p2",
                            "%" + txtSearch.Text.Trim() + "%");
                    }

                    OleDbDataAdapter adapter =
                        new OleDbDataAdapter(cmd);

                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    dgvProducts.DataSource = dt;

                    dgvProducts.ReadOnly = true;
                    dgvProducts.SelectionMode =
                        DataGridViewSelectionMode.FullRowSelect;
                    dgvProducts.MultiSelect = false;
                    dgvProducts.AllowUserToAddRows = false;
                    dgvProducts.AutoSizeColumnsMode =
                        DataGridViewAutoSizeColumnsMode.Fill;

                    UpdateSummary(conn);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading products: " + ex.Message);
            }
        }

        private void UpdateSummary(OleDbConnection conn)
        {
            string query = "SELECT COUNT(*) FROM Products";

            OleDbCommand cmd = new OleDbCommand(query, conn);
            lblTotalProducts.Text = cmd.ExecuteScalar().ToString();

            query = "SELECT COUNT(*) FROM Products WHERE Quantity > ReorderLevel";

            cmd = new OleDbCommand(query, conn);
            lblInStock.Text = cmd.ExecuteScalar().ToString();

            query = "SELECT COUNT(*) FROM Products WHERE Quantity > 0 AND Quantity <= ReorderLevel";

            cmd = new OleDbCommand(query, conn);
            lblLowStock.Text = cmd.ExecuteScalar().ToString();

            query = "SELECT COUNT(*) FROM Products WHERE Quantity = 0";

            cmd = new OleDbCommand(query, conn);
            lblOutOfStock.Text = cmd.ExecuteScalar().ToString();
        }

        private bool ValidateInputs()
        {
            if (txtProductName.Text.Trim() == "" ||
                cmbCategory.Text.Trim() == "" ||
                txtUnitPrice.Text.Trim() == "" ||
                txtQuantity.Text.Trim() == "" ||
                txtReorderLevel.Text.Trim() == "")
            {
                MessageBox.Show("Please complete all required fields.");
                return false;
            }

            decimal price;
            int quantity, reorderLevel;

            if (!decimal.TryParse(txtUnitPrice.Text, out price) || price < 0)
            {
                MessageBox.Show("Please enter a valid unit price.");
                return false;
            }

            if (!int.TryParse(txtQuantity.Text, out quantity) || quantity < 0)
            {
                MessageBox.Show("Please enter a valid quantity.");
                return false;
            }

            if (!int.TryParse(txtReorderLevel.Text, out reorderLevel) || reorderLevel < 0)
            {
                MessageBox.Show("Please enter a valid reorder level.");
                return false;
            }

            return true;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs())
                return;

            try
            {
                using (OleDbConnection conn =
                    new OleDbConnection(connectionString))
                {
                    conn.Open();

                    string query = "INSERT INTO Products " +
                        "([ProductName], [Category], [Description], [UnitPrice], [Quantity], [ReorderLevel]) " +
                        "VALUES (?, ?, ?, ?, ?, ?)";

                    OleDbCommand cmd = new OleDbCommand(query, conn);

                    cmd.Parameters.AddWithValue("@p1", txtProductName.Text.Trim());
                    cmd.Parameters.AddWithValue("@p2", cmbCategory.Text);
                    cmd.Parameters.AddWithValue("@p3", txtDescription.Text);
                    cmd.Parameters.AddWithValue("@p4", decimal.Parse(txtUnitPrice.Text));
                    cmd.Parameters.AddWithValue("@p5", int.Parse(txtQuantity.Text));
                    cmd.Parameters.AddWithValue("@p6", int.Parse(txtReorderLevel.Text));

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Product added successfully!");

                ClearFields();
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding product: " + ex.Message);
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedProductID == 0)
            {
                MessageBox.Show("Please select a product first.");
                return;
            }

            if (!ValidateInputs())
                return;

            try
            {
                using (OleDbConnection conn =
                    new OleDbConnection(connectionString))
                {
                    conn.Open();

                    string query = "UPDATE Products SET " +
                        "[ProductName] = ?, [Category] = ?, [Description] = ?, " +
                        "[UnitPrice] = ?, [Quantity] = ?, [ReorderLevel] = ? " +
                        "WHERE [ProductID] = ?";

                    OleDbCommand cmd = new OleDbCommand(query, conn);

                    cmd.Parameters.AddWithValue("@p1", txtProductName.Text.Trim());
                    cmd.Parameters.AddWithValue("@p2", cmbCategory.Text);
                    cmd.Parameters.AddWithValue("@p3", txtDescription.Text);
                    cmd.Parameters.AddWithValue("@p4", decimal.Parse(txtUnitPrice.Text));
                    cmd.Parameters.AddWithValue("@p5", int.Parse(txtQuantity.Text));
                    cmd.Parameters.AddWithValue("@p6", int.Parse(txtReorderLevel.Text));
                    cmd.Parameters.AddWithValue("@p7", selectedProductID);

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Product updated successfully!");

                ClearFields();
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating product: " + ex.Message);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (selectedProductID == 0)
            {
                MessageBox.Show("Please select a product first.");
                return;
            }

            DialogResult result = MessageBox.Show(
                "Are you sure you want to delete this product?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    using (OleDbConnection conn =
                        new OleDbConnection(connectionString))
                    {
                        conn.Open();

                        string query =
                            "DELETE FROM Products WHERE ProductID = ?";

                        OleDbCommand cmd = new OleDbCommand(query, conn);
                        cmd.Parameters.AddWithValue("@p1", selectedProductID);

                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Product deleted successfully!");

                    ClearFields();
                    LoadProducts();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting product: " + ex.Message);
                }
            }
        }

        private void dgvProducts_CellClick(object sender,
            DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvProducts.Rows[e.RowIndex];

                selectedProductID =
                    Convert.ToInt32(row.Cells["ProductID"].Value);

                txtProductName.Text =
                    row.Cells["ProductName"].Value.ToString();

                cmbCategory.Text =
                    row.Cells["Category"].Value.ToString();

                txtDescription.Text =
                    row.Cells["Description"].Value.ToString();

                txtUnitPrice.Text =
                    row.Cells["UnitPrice"].Value.ToString();

                txtQuantity.Text =
                    row.Cells["Quantity"].Value.ToString();

                txtReorderLevel.Text =
                    row.Cells["ReorderLevel"].Value.ToString();
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadProducts();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Clear();
            ClearFields();
            LoadProducts();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearFields();
        }

        private void ClearFields()
        {
            selectedProductID = 0;

            txtProductName.Clear();
            cmbCategory.SelectedIndex = -1;
            txtDescription.Clear();
            txtUnitPrice.Clear();
            txtQuantity.Clear();
            txtReorderLevel.Clear();

            dgvProducts.ClearSelection();
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            Form1 login = new Form1();
            login.Show();
            this.Close();
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void lblLowStockTitle_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click_1(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtReorderLevel_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
