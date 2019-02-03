﻿//
//  WinCompose — a compose key for Windows — http://wincompose.info/
//
//  Copyright © 2013—2018 Sam Hocevar <sam@hocevar.net>
//
//  This program is free software. It comes without any warranty, to
//  the extent permitted by applicable law. You can redistribute it
//  and/or modify it under the terms of the Do What the Fuck You Want
//  to Public License, Version 2, as published by the WTFPL Task Force.
//  See http://www.wtfpl.net/ for more details.
//

using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Input;
using WinForms = System.Windows.Forms;

namespace WinCompose
{
    /// <summary>
    /// All possible commands that the systray menu can execute
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
        Disable,
        Restart,
        Exit,
    }

    /// <summary>
    /// Interaction logic for SysTrayIcon.xaml
    /// </summary>
    public partial class SysTrayIcon : UserControl, INotifyPropertyChanged, IDisposable
    {
        public SysTrayIcon()
        {
            InitializeComponent();

            // Set the data context for the menu, not for our empty shell class
            ContextMenu.DataContext = this;
        }

        public override void EndInit()
        {
            base.EndInit();

            Application.RemoteControl.DisableEvent += OnDisableEvent;
            Application.RemoteControl.ExitEvent += OnExitEvent;
            Application.RemoteControl.SettingsEvent += OnSettingsEvent;
            Application.RemoteControl.SequencesEvent += OnSequencesEvent;
            Application.RemoteControl.BroadcastDisableEvent();

            WinForms.Application.EnableVisualStyles();
            WinForms.Application.SetCompatibleTextRenderingDefault(false);
            m_icon = new WinForms.NotifyIcon();
            m_icon.Click += NotifyiconClicked;
            m_icon.DoubleClick += NotifyiconDoubleclicked;

            // Opt-in only, as this feature is a bit controversial
            if (Settings.KeepIconVisible.Value)
                SysTray.AlwaysShow("wincompose[.]exe");

            Settings.DisableIcon.ValueChanged += SysTrayUpdateCallback;
            Composer.Changed += SysTrayUpdateCallback;
            Updater.Changed += SysTrayUpdateCallback;
            SysTrayUpdateCallback();

            Updater.Changed += UpdaterStateChanged;
            UpdaterStateChanged();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Composer.Changed -= SysTrayUpdateCallback;
            Updater.Changed -= SysTrayUpdateCallback;
            Updater.Changed -= UpdaterStateChanged;

            Application.RemoteControl.DisableEvent -= OnDisableEvent;
            Application.RemoteControl.ExitEvent -= OnExitEvent;
            Application.RemoteControl.SettingsEvent -= OnSettingsEvent;
            Application.RemoteControl.SequencesEvent -= OnSequencesEvent;

            if (m_icon != null)
            {
                m_icon.Visible = false;
                m_icon.Dispose();
                m_icon = null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand MenuItemCommand
        {
            get { return m_menu_item_command ?? (m_menu_item_command = new DelegateCommand(OnMenuItemClicked)); }
        }

        private DelegateCommand m_menu_item_command;

        private void OnMenuItemClicked(object parameter)
        {
            switch (parameter as MenuCommand?)
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
                    var url = Settings.IsInstalled() ? Updater.Get("Installer")
                                                     : Updater.Get("Portable");
                    System.Diagnostics.Process.Start(url);
                    break;

                case MenuCommand.VisitWebsite:
                    System.Diagnostics.Process.Start("http://wincompose.info/");
                    break;

                case MenuCommand.DonationPage:
                    System.Diagnostics.Process.Start("http://wincompose.info/donate/");
                    break;

                case MenuCommand.Disable:
                    if (Composer.IsDisabled)
                        Application.RemoteControl.BroadcastDisableEvent();
                    Composer.ToggleDisabled();
                    break;

                case MenuCommand.Restart:
                    // FIXME: there might be more cleanup to do here; but it’s probably
                    // not worth it, because restarting the app is a hack and whatever
                    // reason the user may have, it’s because of a bug or a limitation
                    // in WinCompose that we need to fix.
                    m_icon.Visible = false;
                    Application.Current.Shutdown();
                    WinForms.Application.Restart();
                    Environment.Exit(0);
                    break;

                case MenuCommand.Exit:
                    Application.Current.Shutdown();
                    break;
            }
        }

        private WinForms.NotifyIcon m_icon;
        private SequenceWindow m_sequencewindow;
        private SettingsWindow m_optionswindow;
        private DebugWindow m_debugwindow;
        private AboutBox m_about_box;

        private System.Drawing.Icon GetCurrentIcon()
        {
            return GetIcon((Composer.IsDisabled?     0x1 : 0x0) |
                           (Composer.IsComposing?    0x2 : 0x0) |
                           (Updater.HasNewerVersion? 0x4 : 0x0));
        }

        public static System.Drawing.Icon GetIcon(int index)
        {
            if (m_icon_cache == null)
                m_icon_cache = new System.Drawing.Icon[8];

            if (m_icon_cache[index] == null)
            {
                bool is_disabled = (index & 0x1) != 0;
                bool is_composing = (index & 0x2) != 0;
                bool has_update = (index & 0x4) != 0;

                // XXX: if you create new bitmap images here instead of using bitmaps from
                // resources, make sure the DPI settings match. Our PNGs are 72 DPI whereas
                // new Bitmap objects appear to use 96 by default (even if copy-constructed).
                // A reasonable workaround might be to use Clone().
                using (Bitmap bitmap = Properties.Resources.KeyEmpty)
                using (Graphics canvas = Graphics.FromImage(bitmap))
                {
                    // LED status: on or off
                    canvas.DrawImage(is_composing ? Properties.Resources.DecalActive
                                                  : Properties.Resources.DecalIdle, 0, 0);

                    // Large red cross indicating we’re disabled
                    if (is_disabled)
                        canvas.DrawImage(Properties.Resources.DecalDisabled, 0, 0);

                    // Tiny yellow exclamation mark to advertise updates
                    if (has_update)
                        canvas.DrawImage(Properties.Resources.DecalUpdate, 0, 0);

                    canvas.Save();
                    m_icon_cache[index] = System.Drawing.Icon.FromHandle(bitmap.GetHicon());
                }
            }

            return m_icon_cache[index];
        }

        private static System.Drawing.Icon[] m_icon_cache;

        private void SysTrayUpdateCallback()
        {
            m_icon.Visible = !Settings.DisableIcon.Value;
            m_icon.Icon = GetCurrentIcon();

            // XXX: we cannot directly set m_icon.Text because it has an
            // erroneous 64-character limitation (the underlying framework has
            // the correct 128-char limitation). So instead we use this hack,
            // taken from http://stackoverflow.com/a/580264/111461
            Type t = typeof(WinForms.NotifyIcon);
            BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Instance;
            t.GetField("text", hidden).SetValue(m_icon, GetCurrentToolTip());
            if ((bool)t.GetField("added", hidden).GetValue(m_icon))
                t.GetMethod("UpdateIcon", hidden).Invoke(m_icon, new object[] { true });

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDisabled)));
        }

        private static string GetCurrentToolTip()
        {
            string ret = i18n.Text.DisabledToolTip;

            if (!Composer.IsDisabled)
                ret = string.Format(i18n.Text.TrayToolTip,
                                    Settings.ComposeKeys.Value.FriendlyName,
                                    Settings.SequenceCount,
                                    Settings.Version);

            if (Updater.HasNewerVersion)
                ret += "\n" + i18n.Text.UpdatesToolTip;

            return ret;
        }

        private void NotifyiconClicked(object sender, EventArgs e)
        {
            bool is_right_button = (e as WinForms.MouseEventArgs).Button == WinForms.MouseButtons.Right;
            ContextMenu.IsOpen = is_right_button && !ContextMenu.IsOpen;
        }

        private void NotifyiconDoubleclicked(object sender, EventArgs e)
        {
            if ((e as WinForms.MouseEventArgs).Button == WinForms.MouseButtons.Left)
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
        }

        public bool IsDisabled => Composer.IsDisabled;
        public bool HasNewerVersion => Updater.HasNewerVersion;
        public string DownloadHeader => string.Format(i18n.Text.Download, Updater.Get("Latest") ?? "");

        private void UpdaterStateChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasNewerVersion)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DownloadHeader)));
        }

        private void OnDisableEvent()
        {
            if (!Composer.IsDisabled)
                Composer.ToggleDisabled();
            SysTrayUpdateCallback();
        }

        private void OnExitEvent()
        {
            Application.Current.Shutdown();
        }

        private void OnSettingsEvent()
        {
            OnMenuItemClicked(MenuCommand.ShowOptions);
        }

        private void OnSequencesEvent()
        {
            OnMenuItemClicked(MenuCommand.ShowSequences);
        }
    }
}
