//
//  WinCompose — a compose key for Windows — http://wincompose.info/
//
//  Copyright © 2013—2015 Sam Hocevar <sam@hocevar.net>
//              2014—2015 Benjamin Litzelmann
//
//  This program is free software. It comes without any warranty, to
//  the extent permitted by applicable law. You can redistribute it
//  and/or modify it under the terms of the Do What the Fuck You Want
//  to Public License, Version 2, as published by the WTFPL Task Force.
//  See http://www.wtfpl.net/ for more details.
//

using System.Windows;
using System.ComponentModel;
using System.Windows.Input;
using Wpf.Ui.Appearance;

namespace WinCompose
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        public SettingsWindow()
        {
            DataContext = new SettingsWindowViewModel();
            InitializeComponent();
            Settings.ThemeMode.ValueChanged += UpdateBackground;
        }

        private void OnCloseCommandExecuted(object Sender, ExecutedRoutedEventArgs e)
        {
            Hide();
        }

        private void UpdateBackground()
        {
            WindowBackgroundManager.UpdateBackground(this, ApplicationThemeManager.GetAppTheme(), Wpf.Ui.Controls.WindowBackdropType.None);
        }
    }
}
