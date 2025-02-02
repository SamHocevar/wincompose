//
//  WinCompose — a compose key for Windows — http://wincompose.info/
//
//  Copyright © 2013—2021 Sam Hocevar <sam@hocevar.net>
//
//  This program is free software. It comes without any warranty, to
//  the extent permitted by applicable law. You can redistribute it
//  and/or modify it under the terms of the Do What the Fuck You Want
//  to Public License, Version 2, as published by the WTFPL Task Force.
//  See http://www.wtfpl.net/ for more details.
//

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Drawing;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;

namespace WinCompose
{
    /// <summary>
    /// All possible commands that the notification area menu can execute
    /// </summary>
    public enum MenuCommand
    {
        ShowSequences,
        ShowOptions,
        About,
        DebugWindow,
        VisitWebsite,
        DonationPage,
        Download,
        Exit,
    }

    /// <summary>
    /// Interaction logic for NotificationIcon.xaml
    /// </summary>
    public partial class NotificationIcon : TaskbarIcon, INotifyPropertyChanged, IDisposable
    {
        public NotificationIcon()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            Application.RemoteControl.ExitEvent += OnExitEvent;
            Application.RemoteControl.OpenEvent += OnOpenEvent;

            TrayMouseDoubleClick += NotifyiconDoubleclicked;

            Settings.ComposeKeys.ValueChanged += MarkIconDirty;
            Settings.UseXComposeRules.ValueChanged += MarkIconDirty;
            Settings.UseEmojiRules.ValueChanged += MarkIconDirty;
            Settings.UseXorgRules.ValueChanged += MarkIconDirty;
            Composer.Changed += MarkIconDirty;
            Updater.Changed += MarkIconDirty;
            MarkIconDirty();

            Updater.Changed += UpdaterStateChanged;
            UpdaterStateChanged();

            CompositionTarget.Rendering += UpdateNotificationIcon;
        }
        public new void Dispose()
        {
            GC.SuppressFinalize(this);
            CompositionTarget.Rendering -= UpdateNotificationIcon;

            Settings.ComposeKeys.ValueChanged -= MarkIconDirty;
            Settings.UseXComposeRules.ValueChanged -= MarkIconDirty;
            Settings.UseEmojiRules.ValueChanged -= MarkIconDirty;
            Settings.UseXorgRules.ValueChanged -= MarkIconDirty;
            Composer.Changed -= MarkIconDirty;
            Updater.Changed -= MarkIconDirty;
            Updater.Changed -= UpdaterStateChanged;
			
            Application.RemoteControl.ExitEvent -= OnExitEvent;
            Application.RemoteControl.OpenEvent -= OnOpenEvent;

            Visibility = Visibility.Collapsed;

            base.Dispose(true);
        }
        //protected virtual void Dispose(bool disposing)
        //{
        //    CompositionTarget.Rendering -= UpdateNotificationIcon;

        //    Composer.Changed -= MarkIconDirty;
        //    Updater.Changed -= MarkIconDirty;
        //    Updater.Changed -= UpdaterStateChanged;

        //    Application.RemoteControl.ExitEvent -= OnExitEvent;
        //    Application.RemoteControl.OpenEvent -= OnOpenEvent;

        //    Visibility = Visibility.Collapsed;

        //}

        public ICommand MenuItemCommand
        {
            get { return m_menu_item_command ?? (m_menu_item_command = new DelegateCommand(OnCommand)); }
        }

        private DelegateCommand m_menu_item_command;

        private void OnCommand(object o)
        {
            switch (o as MenuCommand?)
            {
                case MenuCommand.ShowSequences:
                    m_sequencewindow = m_sequencewindow ?? new SequenceWindow();
                    m_sequencewindow.Show();
                    m_sequencewindow.Activate();
                    break;

                case MenuCommand.ShowOptions:
                    m_optionswindow = m_optionswindow ?? new SettingsWindow();
                    m_optionswindow.Show();
                    m_optionswindow.Activate();
                    break;

                case MenuCommand.DebugWindow:
                    m_debugwindow = m_debugwindow ?? new DebugWindow();
                    m_debugwindow.Show();
                    m_debugwindow.Activate();
                    break;

                case MenuCommand.About:
                    m_about_box = m_about_box ?? new AboutBox();
                    m_about_box.Show();
                    m_about_box.Activate();
                    break;

                case MenuCommand.Download:
                    var url = Utils.IsInstalled ? Updater.Get("Installer")
                                                : Updater.Get("Portable");
                    Process.Start(url);
                    break;

                case MenuCommand.VisitWebsite:
                    Process.Start("http://wincompose.info/");
                    break;

                case MenuCommand.DonationPage:
                    Process.Start("http://wincompose.info/donate/");
                    break;

                case MenuCommand.Exit:
                    Application.Current.Shutdown();
                    break;
            }
        }

        private SequenceWindow m_sequencewindow;
        private SettingsWindow m_optionswindow;
        private DebugWindow m_debugwindow;
        private AboutBox m_about_box;

        public event PropertyChangedEventHandler PropertyChanged;

        //private static System.Drawing.Icon[] m_icon_cache;

        private misc.AtomicFlag m_dirty;

        private void MarkIconDirty() => m_dirty.Set();

        private void UpdateNotificationIcon(object o, EventArgs e)
        {
            if (m_dirty.Get())
            {
                Currenttooltip = GetCurrentToolTip();
            }
        }
        private string CurrentToolTip;

        public string Currenttooltip
        {
            get => CurrentToolTip;
            set
            {
                if (value == CurrentToolTip) return;
                CurrentToolTip = value;
                Debug.WriteLine(CurrentToolTip);
                OnPropertyChanged("currenttooltip");
            }
        }

        private static string GetCurrentToolTip()
        {
            var ret = string.Format(i18n.Text.TrayToolTip,
                                    Settings.ComposeKeys.Value.FriendlyName,
                                    Settings.SequenceCount,
                                    Settings.Version);

            if (Updater.HasNewerVersion)
                ret += "\n" + i18n.Text.UpdatesToolTip;

            return ret;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this , new PropertyChangedEventArgs(propertyName));
        }


        public void NotifyiconDoubleclicked(object sender, EventArgs e)
        {
            m_sequencewindow = m_sequencewindow ?? new SequenceWindow();

            if (m_sequencewindow.IsVisible)
            {
                m_sequencewindow.Hide();
            }
            else
            {
                m_sequencewindow.Show();
                m_sequencewindow.Activate();
            }
        }

        public bool HasNewerVersion => Updater.HasNewerVersion;
        public string DownloadHeader => string.Format(i18n.Text.Download, Updater.Get("Latest") ?? "");

        private void UpdaterStateChanged()
        {
            PropertyChanged?.Invoke(this , new PropertyChangedEventArgs(nameof(HasNewerVersion)));
            PropertyChanged?.Invoke(this , new PropertyChangedEventArgs(nameof(DownloadHeader)));
        }

        private void OnExitEvent() => Application.Current.Shutdown();

        private void OnOpenEvent(MenuCommand cmd) => OnCommand(cmd);
    }
}
