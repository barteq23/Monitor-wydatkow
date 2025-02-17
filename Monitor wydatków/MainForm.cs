// BudgetAnalyzerApp - WinForms + SQLite

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace BudgetAnalyzerApp
{
    public partial class MainForm : Form
    {
        private SQLiteConnection? connection;
        private Label labelBalance = null!;
        private DataGridView dataGridViewTransactions = null!;
        private Button buttonAddTransaction = null!;
        private Button buttonEditTransaction = null!;
        private Button buttonDeleteTransaction = null!;

        public MainForm()
        {
            InitializeComponent();
            InitializeDatabase();
            LoadTransactions();
            UpdateBalance();
        }

        private void InitializeComponent() //tworzenie interfejsu
        {
            this.labelBalance = new Label { Text = "Saldo: 0.00 zł", AutoSize = true, Font = new System.Drawing.Font("Arial", 14), Location = new System.Drawing.Point(20, 20) };
            this.dataGridViewTransactions = new DataGridView { Location = new System.Drawing.Point(20, 60), Size = new System.Drawing.Size(500, 300), ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
            this.buttonAddTransaction = new Button { Text = "Dodaj transakcję", Location = new System.Drawing.Point(20, 380), Size = new System.Drawing.Size(150, 30) };
            this.buttonEditTransaction = new Button { Text = "Edytuj transakcję", Location = new System.Drawing.Point(180, 380), Size = new System.Drawing.Size(150, 30) };
            this.buttonDeleteTransaction = new Button { Text = "Usuń transakcję", Location = new System.Drawing.Point(340, 380), Size = new System.Drawing.Size(150, 30) };

            this.buttonAddTransaction.Click += new EventHandler(this.buttonAddTransaction_Click);
            this.buttonEditTransaction.Click += new EventHandler(this.buttonEditTransaction_Click);
            this.buttonDeleteTransaction.Click += new EventHandler(this.buttonDeleteTransaction_Click);

            this.Controls.Add(this.labelBalance);
            this.Controls.Add(this.dataGridViewTransactions);
            this.Controls.Add(this.buttonAddTransaction);
            this.Controls.Add(this.buttonEditTransaction);
            this.Controls.Add(this.buttonDeleteTransaction);

            this.Text = "Analizator Budżetu Domowego";
            this.Size = new System.Drawing.Size(560, 450);
        }

        private void InitializeDatabase() //tworzenie bazy danych
        {
            connection = new SQLiteConnection("Data Source=budget.db;Version=3;");
            connection.Open();

            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Transactions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Date TEXT NOT NULL,
                    Category TEXT NOT NULL,
                    Amount REAL NOT NULL,
                    Type TEXT NOT NULL,
                    Description TEXT
                );";

            using (var cmd = new SQLiteCommand(createTableQuery, connection))
            {
                cmd.ExecuteNonQuery();
            }
        }


        private void LoadTransactions()  //ładowanie transakcji 
        {
            string selectQuery = "SELECT * FROM Transactions ORDER BY Date DESC;";
            using (var adapter = new SQLiteDataAdapter(selectQuery, connection))
            {
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                dataGridViewTransactions.DataSource = dt;
            }
        }

        private void UpdateBalance()  //suma saldo
        {
            string incomeQuery = "SELECT SUM(Amount) FROM Transactions WHERE Type='Income';";
            string expenseQuery = "SELECT SUM(Amount) FROM Transactions WHERE Type='Expense';";

            object? incomeResult = new SQLiteCommand(incomeQuery, connection).ExecuteScalar();
            object? expenseResult = new SQLiteCommand(expenseQuery, connection).ExecuteScalar();

            double income = incomeResult != DBNull.Value && incomeResult != null ? Convert.ToDouble(incomeResult) : 0;
            double expenses = expenseResult != DBNull.Value && expenseResult != null ? Convert.ToDouble(expenseResult) : 0;

            labelBalance.Text = $"Saldo: {income - expenses} zł";
        }

        private void buttonAddTransaction_Click(object? sender, EventArgs e) //przycisk dodaj
        {
            AddTransactionForm addForm = new AddTransactionForm(connection!);
            addForm.FormClosed += (s, args) => { LoadTransactions(); UpdateBalance(); };
            addForm.ShowDialog();
        }

        private void buttonEditTransaction_Click(object? sender, EventArgs e) //przycisk edytuj
        {
            if (dataGridViewTransactions.SelectedRows.Count > 0)
            {
                int id = Convert.ToInt32(dataGridViewTransactions.SelectedRows[0].Cells["Id"].Value);
                AddTransactionForm editForm = new AddTransactionForm(connection!, id);
                editForm.FormClosed += (s, args) => { LoadTransactions(); UpdateBalance(); };
                editForm.ShowDialog();
            }
        }

        private void buttonDeleteTransaction_Click(object? sender, EventArgs e) //przycisk usuń
        {
            if (dataGridViewTransactions.SelectedRows.Count > 0)
            {
                int id = Convert.ToInt32(dataGridViewTransactions.SelectedRows[0].Cells["Id"].Value);

                DialogResult dialogResult = MessageBox.Show("Czy na pewno chcesz usunąć tę transakcję?", "Potwierdzenie", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    string deleteQuery = "DELETE FROM Transactions WHERE Id=@Id;";
                    using (var cmd = new SQLiteCommand(deleteQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                    LoadTransactions();
                    UpdateBalance();
                }
            }
        }
    }

    public partial class AddTransactionForm : Form
    {
        private SQLiteConnection connection;
        private int? transactionId;
        private DateTimePicker dateTimePicker = null!;
        private ComboBox comboBoxCategory = null!;
        private TextBox textBoxAmount = null!, textBoxDescription = null!;
        private ComboBox comboBoxType = null!;
        private Button buttonSave = null!;

        private readonly string[] predefinedCategories = { "Darowizna", "Wypłata", "Kosmetyki", "Jedzenie", "Rozrywka", "Transport", "Inne", "Paliwo", "Wakacje", "Wyjazdy", "Prezenty", "Elektronika", "Zdrowie", "Ubrania" };

        public AddTransactionForm(SQLiteConnection conn, int? id = null) //łączenie z bazą, ładowanie danych
        {
            connection = conn;
            transactionId = id;
            InitializeComponent();

            if (transactionId.HasValue)
            {
                LoadTransactionData();
            }
        }

        private void InitializeComponent()  //tworzenie formularza 
        {
            this.dateTimePicker = new DateTimePicker { Location = new System.Drawing.Point(20, 20) };
            this.comboBoxCategory = new ComboBox { Location = new System.Drawing.Point(20, 60), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            this.comboBoxCategory.Items.AddRange(predefinedCategories);
            this.comboBoxCategory.SelectedIndex = 0;

            this.textBoxAmount = new TextBox { PlaceholderText = "Kwota", Location = new System.Drawing.Point(20, 100), Width = 200 };
            this.comboBoxType = new ComboBox { Location = new System.Drawing.Point(20, 140), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            this.comboBoxType.Items.AddRange(new string[] { "Income", "Expense" });
            this.comboBoxType.SelectedIndex = 0;

            this.textBoxDescription = new TextBox { PlaceholderText = "Opis (opcjonalny)", Location = new System.Drawing.Point(20, 180), Width = 200 };
            this.buttonSave = new Button { Text = "Zapisz", Location = new System.Drawing.Point(20, 220), Size = new System.Drawing.Size(200, 30) };
            this.buttonSave.Click += new EventHandler(this.buttonSave_Click);

            this.Controls.Add(dateTimePicker);
            this.Controls.Add(comboBoxCategory);
            this.Controls.Add(textBoxAmount);
            this.Controls.Add(comboBoxType);
            this.Controls.Add(textBoxDescription);
            this.Controls.Add(buttonSave);

            this.Text = transactionId.HasValue ? "Edytuj transakcję" : "Dodaj transakcję";
            this.Size = new System.Drawing.Size(260, 300);
        }

        private void LoadTransactionData() //ładowanie danych z bazy
        {
            string selectQuery = "SELECT * FROM Transactions WHERE Id=@Id;";
            using (var cmd = new SQLiteCommand(selectQuery, connection))
            {
                cmd.Parameters.AddWithValue("@Id", transactionId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        dateTimePicker.Value = DateTime.Parse(reader["Date"].ToString()!);
                        comboBoxCategory.SelectedItem = reader["Category"].ToString();
                        textBoxAmount.Text = reader["Amount"].ToString();
                        comboBoxType.SelectedItem = reader["Type"].ToString();
                        textBoxDescription.Text = reader["Description"].ToString();
                    }
                }
            }
        }

        private void buttonSave_Click(object? sender, EventArgs e) //zapisywanie transakcji (instnieje/nie istnieje)
        {
            if (transactionId.HasValue)
            {
                string updateQuery = @"
                    UPDATE Transactions 
                    SET Date=@Date, Category=@Category, Amount=@Amount, Type=@Type, Description=@Description 
                    WHERE Id=@Id;";

                using (var cmd = new SQLiteCommand(updateQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@Date", dateTimePicker.Value.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@Category", comboBoxCategory.SelectedItem?.ToString() ?? "Inne");
                    cmd.Parameters.AddWithValue("@Amount", double.TryParse(textBoxAmount.Text, out double amount) ? amount : 0);
                    cmd.Parameters.AddWithValue("@Type", comboBoxType.SelectedItem?.ToString() ?? "Expense");
                    cmd.Parameters.AddWithValue("@Description", textBoxDescription.Text);
                    cmd.Parameters.AddWithValue("@Id", transactionId);

                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                string insertQuery = @"
                    INSERT INTO Transactions (Date, Category, Amount, Type, Description) 
                    VALUES (@Date, @Category, @Amount, @Type, @Description);";

                using (var cmd = new SQLiteCommand(insertQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@Date", dateTimePicker.Value.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@Category", comboBoxCategory.SelectedItem?.ToString() ?? "Inne");
                    cmd.Parameters.AddWithValue("@Amount", double.TryParse(textBoxAmount.Text, out double amount) ? amount : 0);
                    cmd.Parameters.AddWithValue("@Type", comboBoxType.SelectedItem?.ToString() ?? "Expense");
                    cmd.Parameters.AddWithValue("@Description", textBoxDescription.Text);

                    cmd.ExecuteNonQuery();
                }
            }

            this.Close();
        }
    }
}
