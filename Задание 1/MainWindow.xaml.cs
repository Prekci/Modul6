using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.OleDb;
using System.Data.Common;

namespace Задание_1
{
    public partial class MainWindow : Window
    {
        private AccessDatabase db;
        public ObservableCollection<ToDoItem> ToDoItems { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            ToDoItems = new ObservableCollection<ToDoItem>(); // Инициализация ToDoItems
            ToDoGrid.ItemsSource = ToDoItems;
            db = new AccessDatabase("D:\\ПРАКТИКА\\Гончаренко Д модуль 6\\Data.accdb");
            LoadDataFromDatabase();
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            db.CloseConnection();
        }
        public void Button_Click(object sender, RoutedEventArgs e)
        {
            // Получите выбранный элемент из DataGrid
            ToDoItem selectedTask = ToDoGrid.SelectedItem as ToDoItem;
            if (selectedTask != null)
            {
                // Обновите статус задачи в соответствии с CheckBox
                selectedTask.Статус = selectedTask.Статус;
                // Сохраните изменения в базе данных Access
                selectedTask.UpdateStatusInDatabase();
                // Обновите источник данных
                ToDoGrid.Items.Refresh();
                MessageBox.Show("Статус успешно обновлен в базе данных.");
            }
        }
        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Получите выбранный элемент из DataGrid
            ToDoItem editedTask = e.Row.Item as ToDoItem;
            if (editedTask != null)
            {
                // Обновите текст задачи в соответствии с TextBox
                TextBox textBox = e.EditingElement as TextBox;
                if (textBox != null)
                {
                    editedTask.Задача = textBox.Text;
                    // Сохраните изменения в базе данных Access
                    editedTask.UpdateTaskInDatabase();
                    MessageBox.Show("Задача успешно обновлена в базе данных.");
                }
            }
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (ToDoGrid.SelectedItem != null)
            {
                ToDoItem selectedItem = (ToDoItem)ToDoGrid.SelectedItem;
                int selectedIndex = ToDoItems.IndexOf(selectedItem);
                if (selectedIndex >= 0)
                {
                    ToDoItems.RemoveAt(selectedIndex);
                    for (int i = selectedIndex; i < ToDoItems.Count; i++)
                    {
                        ToDoItems[i].ID = i + 1;
                    }
                    string deleteQuery = "DELETE FROM Таблица2 WHERE ID = ?";
                    using (OleDbCommand command = new OleDbCommand(deleteQuery, db.connection))
                    {
                        command.Parameters.AddWithValue("ID", selectedItem.ID);
                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Задача успешно удалена из базы данных.");
                        }
                        else
                        {
                            MessageBox.Show("Ошибка при удалении задачи из базы данных.");
                        }
                    }
                }
            }
        }
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            ToDoItem newTask = new ToDoItem(ToDoItems.Count + 1, db) { Статус = false, Задача = ""};
            ToDoItems.Add(newTask);
            string insertQuery = "INSERT INTO Таблица2 (Status, Task) VALUES (?, ?)";
            using (OleDbCommand command = new OleDbCommand(insertQuery, db.connection))
            {
                command.Parameters.AddWithValue("Status", newTask.Статус);
                command.Parameters.AddWithValue("Task", newTask.Задача);
                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    MessageBox.Show("Задача успешно добавлена в базу данных.");
                }
                else
                {
                    MessageBox.Show("Ошибка при добавлении задачи в базу данных.");
                }
            }
        }
        private void LoadDataFromDatabase()
        {
            ToDoItems.Clear();
            string selectQuery = "SELECT ID, Status, Task FROM Таблица2";
            int currentRow = 1;
            using (OleDbCommand command = new OleDbCommand(selectQuery, db.connection))
            {
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ToDoItem task = new ToDoItem((int)reader["ID"], db)
                        {
                            Статус = (bool)reader["Status"],
                            Задача = reader["Task"].ToString(),
                        };
                        ToDoItems.Add(task);
                        currentRow++;
                    }
                }
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDataFromDatabase();
            ToDoGrid.CellEditEnding += DataGrid_CellEditEnding;
        }
    }
    public class AccessDatabase
    {
        public OleDbConnection connection;
        private string connectionString;
        public AccessDatabase(string databasePath)
        {
            connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={databasePath}";
            connection = new OleDbConnection(connectionString);
            try
            {
                connection.Open();
            }
            catch (OleDbException ex)
            {
                MessageBox.Show($"Ошибка при подключении к базе данных: {ex.Message}");
            }
        }
        public OleDbDataReader ExecuteQuery(string query)
        {
            OleDbCommand command = new OleDbCommand(query, connection);
            return command.ExecuteReader();
        }
        public int ExecuteNonQuery(string query)
        {
            OleDbCommand command = new OleDbCommand(query, connection);
            return command.ExecuteNonQuery();
        }
        public void CloseConnection()
        {
            if (connection.State == ConnectionState.Open)
            {
                connection.Close();
            }
        }
    }
    public class ToDoItem : INotifyPropertyChanged
    {
        public int ID { get; set; }
        private bool _статус;
        public bool Статус
        {
            get { return _статус; }
            set
            {
                if (_статус != value)
                {
                    _статус = value;
                    OnPropertyChanged("Статус");
                    // Обновляем значение в базе данных
                    UpdateStatusInDatabase();
                }
            }
        }
        public string Задача { get; set; }
        public AccessDatabase Database { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public void UpdateStatusInDatabase()
        {
            string updateQuery = "UPDATE Таблица2 SET Status = ? WHERE ID = ?";
            using (OleDbCommand command = new OleDbCommand(updateQuery, Database.connection))
            {
                command.Parameters.AddWithValue("Status", Статус);
                command.Parameters.AddWithValue("ID", ID);
                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {

                }
                else
                {
                    MessageBox.Show("Ошибка при обновлении статуса в базе данных.");
                }
            }
        }
        public void UpdateTaskInDatabase()
        {
            if (ID > 0) // Проверьте, что у элемента есть допустимый ID
            {
                string updateQuery = "UPDATE Таблица2 SET Task = ? WHERE ID = ?";
                using (OleDbCommand command = new OleDbCommand(updateQuery, Database.connection))
                {
                    command.Parameters.AddWithValue("Task", Задача);
                    command.Parameters.AddWithValue("ID", ID);
                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Задача успешно обновлена в базе данных.");
                    }
                    else
                    {
                        MessageBox.Show("Ошибка при обновлении задачи в базе данных.");
                    }
                }
            }
        }
        public ToDoItem(int id, AccessDatabase database)
        {
            ID = id;
            Database = database;
        }
    }
}