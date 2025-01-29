//
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
using System.Runtime.InteropServices;

namespace WinCompose
{
    /// <summary>
    /// Interaction logic for KeySelector.xaml
    /// </summary>
    public partial class KeySelector : BaseWindow
    {
        public KeySelector()
        {
            InitializeComponent();
            IsVisibleChanged += (o, e) => { if ((bool)e.NewValue) Composer.Captured += KeyCaptured; };
            Closing += (o, e) => Composer.Captured -= KeyCaptured;
        }

        public Key Key { get; private set; }

        public static string CancelButtonText
        {
            get => Marshal.PtrToStringAuto(NativeMethods.MB_GetString(DialogBoxCommandID.IDCANCEL));
        }

        private void KeyCaptured(Key k)
        {
            // Don't accept Escape as compose key.
            if (k.VirtualKey != VK.ESCAPE)
            {
                Key = k;
                // Close window from the right thread. Performance is not a
                // concern because we hijacked the whole screen.
                Dispatcher.Invoke(new Action(Close));
            }
        }

        private void CancelClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            Key = null;
            Close();
        }
    }
}
