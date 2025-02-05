//
//  WinCompose — a compose key for Windows — http://wincompose.info/
//
//  Copyright © 2013—2019 Sam Hocevar <sam@hocevar.net>
//              2014—2015 Benjamin Litzelmann
//
//  This program is free software. It comes without any warranty, to
//  the extent permitted by applicable law. You can redistribute it
//  and/or modify it under the terms of the Do What the Fuck You Want
//  to Public License, Version 2, as published by the WTFPL Task Force.
//  See http://www.wtfpl.net/ for more details.
//

using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace WinCompose
{
    /// <summary>
    /// Interaction logic for mainwindow.xaml
    /// </summary>
    public partial class SequenceWindow : INotifyPropertyChanged
    {
        public SequenceWindow()
        {
            InitializeComponent();
            DataContext = new RootViewModel();
            Activated += OnActivated;
            Loaded += OnLoaded;
            Settings.ThemeMode.ValueChanged += UpdateBackground;
        }

        private void OnActivated(object sender, EventArgs e)
            => SearchBox.Focus();

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SearchBox.PreviewKeyDown += OnSearchBoxPreviewKeyDown;
        }

        private void OnSearchBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Return)
                e.Handled = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private RootViewModel m_view_model => (RootViewModel)DataContext;

        private RichTextBox SearchBox
        {
            get
            {
                var grid = VisualTreeHelper.GetChild(SearchWidget, 0) as Grid;
                return VisualTreeHelper.GetChild(grid, 0) as RichTextBox;
            }
        }
        private void UpdateBackground()
        {
            WindowBackgroundManager.UpdateBackground(this , ApplicationThemeManager.GetAppTheme() , Wpf.Ui.Controls.WindowBackdropType.None);
        }

        protected virtual void OnPropertyChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnCloseCommandExecuted(object Sender, ExecutedRoutedEventArgs e)
        {
            // If the search box is focused and non-empty, clear it; otherwise,
            // actually close the window.
            if (SearchBox.IsFocused && !string.IsNullOrEmpty(m_view_model.SearchText))
                m_view_model.SearchText = "";
            else
                Hide();
        }

        private void OnCopyCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var seq_view = (ListBox.SelectedItem as SequenceViewModel)?.Result;
            if(seq_view != null)
            {
                // Try several times in case another process uses the clipboard
                // See https://github.com/samhocevar/wincompose/issues/319
                for (int n = 0; n < 10; ++n)
                {
                    try
                    {
                        Clipboard.SetText(seq_view);
                        return;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(10 * n);
                    }
                }
            }
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
            => m_view_model.SearchText = "";

        private void EditUserDefinedSequences_Click(object sender, RoutedEventArgs e)
            => Settings.EditCustomRulesFile();

        private void ReloadUserDefinedSequences_Click(object sender, RoutedEventArgs e)
        {
            Settings.LoadSequences();
            // Create a new view model to sync the sequence list UI with the
            // active sequences. Preserve the search text while doing so.
            var search_text = m_view_model.SearchText;
            DataContext = new RootViewModel();
            m_view_model.SearchText = search_text;
        }

        private void ToggleFavorite_Click(object sender, RoutedEventArgs e)
            => (ListBox.SelectedItem as SequenceViewModel)?.ToggleFavorite();
    }
}
