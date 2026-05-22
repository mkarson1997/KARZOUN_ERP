using FornixxCRM.Helpers;
using System;
using System.Windows;
using System.Windows.Input;

namespace FornixxCRM.Views.Settings;

public partial class PasswordPromptWindow : Window
{
    private readonly string _hashedPassword;

    public PasswordPromptWindow(string hashedPassword)
    {
        InitializeComponent();
        _hashedPassword = hashedPassword;
        
        // Match main app FlowDirection
        this.FlowDirection = LocalizationManager.FlowDirection;
        TxtPassword.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (PasswordHasher.VerifyPassword(TxtPassword.Password, _hashedPassword))
        {
            DialogResult = true;
            Close();
        }
        else
        {
            TxtError.Visibility = Visibility.Visible;
            TxtPassword.SelectAll();
            TxtPassword.Focus();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Ok_Click(sender, e);
        }
    }
}
