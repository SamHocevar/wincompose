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
using System.Diagnostics;
using System.Windows;

using WinCompose.i18n;

namespace WinCompose
{
    public class SettingsWindowViewModel : ViewModelBase
    {
        private DelegateCommand m_close_command;
        private DelegateCommand m_edit_command;
        private KeySelector m_key_selector;
        private string m_theme_mode;
        private string m_selected_language;
        private string m_close_button_text;
        private Visibility m_warn_message_visibility;

        public SettingsWindowViewModel()
        {
            m_close_command = new DelegateCommand(OnCloseCommandExecuted);
            m_edit_command = new DelegateCommand(OnEditCommandExecuted);
            m_selected_language = Settings.Language.Value;
            m_theme_mode = Settings.ThemeMode.Value;
            m_close_button_text = Text.Close;
            m_warn_message_visibility = Visibility.Collapsed;
        }

        public DelegateCommand CloseButtonCommand
        {
            get => m_close_command;
            private set => SetValue(ref m_close_command, value, nameof(CloseButtonCommand));
        }

        public DelegateCommand EditButtonCommand
        {
            get => m_edit_command;
            private set => SetValue(ref m_edit_command, value, nameof(EditButtonCommand));
        }

        public string SelectedLanguage
        {
            get => m_selected_language;
            set => SetValue(ref m_selected_language, value, nameof(SelectedLanguage));
        }
        public string SelectedTheme
        {
            get => m_theme_mode;
            set => SetValue(ref m_theme_mode , value , nameof(SelectedTheme));
        }

        public Key SelectedLedKey
        {
            // FIXME: this settings value should be a Key, not a KeySequence
            get => Settings.LedKey.Value[0];
            set => Settings.LedKey.Value = new KeySequence() { value };
        }

        public Key ComposeKey0 { get => GetComposeKey(0); set => SetComposeKey(0, value); }
        public Key ComposeKey1 { get => GetComposeKey(1); set => SetComposeKey(1, value); }

        private Key GetComposeKey(int index)
        {
            return Settings.ComposeKeys.Value.Count > index ? Settings.ComposeKeys.Value[index] : null;
        }

        private void SetComposeKey(int index, Key key)
        {
            if (index < 0)
                return;

            // Rebuild a complete list to force saving configuration file
            var newlist = new KeySequence(Settings.ComposeKeys.Value);
            while (newlist.Count <= index)
                newlist.Add(null);
            newlist[index] = key;
            Settings.ComposeKeys.Value = newlist;
        }

        public double DelayTicks
        {
            get => Settings.ResetTimeout.Value == -1 ? 0 : Math.Log(Settings.ResetTimeout.Value / 200.0, 1.6);
            set
            {
                Settings.ResetTimeout.Value = value == 0 ? -1 : (int)Math.Round(200 * Math.Pow(1.6, value));
                OnPropertyChanged(nameof(DelayText));
            }
        }

        public string DelayText
        {
            get
            {
                if (Settings.ResetTimeout.Value < 0)
                    return i18n.Text.NoTimeout;

                // Perform some aesthetic rounding on the displayed value
                double display_value = Settings.ResetTimeout.Value / 1000.0;
                double round = Math.Pow(10, Math.Floor(Math.Log(display_value / 2, 10)));
                display_value = Math.Round(display_value / round) * round;
                return string.Format(i18n.Text.DelaySeconds, display_value);
            }
        }

        public string CloseButtonText
        {
            get => m_close_button_text;
            private set => SetValue(ref m_close_button_text, value, nameof(CloseButtonText));
        }

        public Visibility WarnMessageVisibility
        {
            get => m_warn_message_visibility;
            set => SetValue(ref m_warn_message_visibility, value, nameof(WarnMessageVisibility));
        }

        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == nameof(SelectedLanguage))
            {
                Settings.Language.Value = SelectedLanguage;
                WarnMessageVisibility   = Visibility.Visible;
                CloseButtonText         = Text.Restart;
                CloseButtonCommand      = new DelegateCommand(OnRestartCommandExecuted);
            }
            if (propertyName == nameof(SelectedTheme))
            {
                Settings.ThemeMode.Value = SelectedTheme;
            }
        }

        private void OnCloseCommandExecuted(object parameter)
        {
            ((Window)parameter).Hide();
        }

        private void OnEditCommandExecuted(object parameter)
        {
            if (int.TryParse(parameter as string ?? "", out var key_index))
            {
                m_key_selector = m_key_selector ?? new KeySelector();
                m_key_selector.ShowDialog();
                SetComposeKey(key_index, m_key_selector.Key ?? new Key(VK.DISABLED));
                OnPropertyChanged("ComposeKey" + (parameter as string));
            }
        }

        private static void OnRestartCommandExecuted(object parameter)
        {
            // FIXME: this could be refactored into NotificationIcon.xaml.cs
            Application.Current.Exit += (s, e) => Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }
    }
}
