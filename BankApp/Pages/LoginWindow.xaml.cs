using Kurs_Bank.Data;
using Kurs_Bank.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Windows;
using System.Windows.Input;

namespace Kurs_Bank.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string login = LoginBox.Text.Trim();
                string password = PasswordBox.Password;

                if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
                {
                    ShowError("Заполните все поля");
                    return;
                }

                using (var db = DbContextFactory.Create())
                {
                    var user = db.Users
                        .Include(u => u.Client)
                        .FirstOrDefault(u => u.Login == login && u.Password == password);

                    if (user == null)
                    {
                        ShowError("Неверный логин или пароль");
                        return;
                    }

                    SessionManager.CurrentClient = user.Client;
                    var main = new MainWindow();
                    main.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка");
            }
        }

        private void GoToRegister_Click(object sender, MouseButtonEventArgs e)
        {
            var reg = new RegisterWindow();
            reg.Show();
            this.Close();
        }

        private void ShowError(string msg)
        {
            ErrorText.Text = msg;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}