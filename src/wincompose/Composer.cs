//
//  WinCompose — a compose key for Windows — http://wincompose.info/
//
//  Copyright © 2013—2021 Sam Hocevar <sam@hocevar.net>
//              2014—2015 Benjamin Litzelmann
//
//  This program is free software. It comes without any warranty, to
//  the extent permitted by applicable law. You can redistribute it
//  and/or modify it under the terms of the Do What the Fuck You Want
//  to Public License, Version 2, as published by the WTFPL Task Force.
//  See http://www.wtfpl.net/ for more details.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace WinCompose
{

internal enum EventType
{
    KeyUp,
    KeyDown,
    KeyUpDown
}

/// <summary>
/// A convenience class that can be fed either scancodes (<see cref="SC"/>)
/// or virtual keys (<see cref="VK"/>), then uses the Win32 API function
/// <see cref="SendInput"/> to send all these events in one single call.
/// </summary>
class InputSequence : List<INPUT>
{
    public void AddUnicodeInput(char ch)
        => AddInput(EventType.KeyUpDown,
                    new KEYBDINPUT() { wScan = (ScanCodeShort)ch, dwFlags = KEYEVENTF.UNICODE });

    public void AddKeyEvent(EventType type, VK vk)
        => AddInput(type, new KEYBDINPUT() { wVk = (VirtualKeyShort)vk });

    public void Send()
    {
        NativeMethods.SendInput((uint)Count, ToArray(),
                                Marshal.SizeOf(typeof(INPUT)));
    }

    private void AddInput(EventType type, KEYBDINPUT ki)
    {
        INPUT tmp = new INPUT();
        tmp.type = EINPUT.KEYBOARD;
        tmp.U.ki = ki;

        if (type != EventType.KeyUp)
            Add(tmp);

        if (type != EventType.KeyDown)
        {
            tmp.U.ki.dwFlags |= KEYEVENTF.KEYUP;
            Add(tmp);
        }
    }
}

/// <summary>
/// The main composer class. It gets input from the keyboard hook, and
/// acts depending on the global configuration and current keyboard state.
/// </summary>
static class Composer
{
    /// <summary>
    /// Initialize the composer.
    /// </summary>
    public static void Init()
    {
        KeyboardLeds.StartMonitoring();
        KeyboardLayout.CheckForChanges();

        m_timeout = new DispatcherTimer();
        m_timeout.Tick += (o, e) => ResetSequence();
    }

    /// <summary>
    /// Terminate the composer.
    /// </summary>
    public static void Fini()
    {
        m_timeout.Stop();
        KeyboardLeds.StopMonitoring();
    }

    /// <summary>
    /// Get input from the keyboard hook; return true if the key was handled
    /// and needs to be removed from the input chain.
    /// </summary>
    public static bool OnKey(WM ev, VK vk, SC sc, LLKHF flags)
    {
        // Remember when the user touched a key for the last time
        m_last_key_time = DateTime.Now;

        // We need to check the keyboard layout before we save the dead
        // key, otherwise we may be saving garbage.
        KeyboardLayout.CheckForChanges();

        KeyboardLayout.SaveDeadKey();
        bool ret = OnKeyInternal(ev, vk, sc, flags);
        KeyboardLayout.RestoreDeadKey();

        return ret;
    }

    private static bool OnKeyInternal(WM ev, VK vk, SC sc, LLKHF flags)
    {
        bool is_keydown = (ev == WM.KEYDOWN || ev == WM.SYSKEYDOWN);
        bool is_keyup = !is_keydown;
        bool add_to_sequence = is_keydown;
        bool is_capslock_hack = false;

        bool has_shift = (NativeMethods.GetKeyState(VK.SHIFT) & 0x80) != 0;
        bool has_altgr = (NativeMethods.GetKeyState(VK.LCONTROL) &
                          NativeMethods.GetKeyState(VK.RMENU) & 0x80) != 0;
        bool has_lrshift = (NativeMethods.GetKeyState(VK.LSHIFT) &
                            NativeMethods.GetKeyState(VK.RSHIFT) & 0x80) != 0;
        bool has_capslock = NativeMethods.GetKeyState(VK.CAPITAL) != 0;

        // Guess what the system would print if we weren’t interfering. If
        // a printable representation exists, use that. Otherwise, default
        // to its virtual key code.
        Key key = KeyboardLayout.VkToKey(vk, sc, flags, has_shift, has_altgr, has_capslock);

        // Special handling of Left Control on keyboards with AltGr
        if (KeyboardLayout.HasAltGr && key.VirtualKey == VK.LCONTROL)
        {
            bool is_altgr = false;
            // If this is a key down event with LLKHF_ALTDOWN but no Alt key
            // is down, it is actually AltGr.
            is_altgr |= is_keydown && (flags & LLKHF.ALTDOWN) != 0
                         && ((NativeMethods.GetKeyState(VK.LMENU) |
                              NativeMethods.GetKeyState(VK.RMENU)) & 0x80) == 0;
            // If this is a key up event but Left Control is not down, it is
            // actually AltGr.
            is_altgr |= is_keyup && m_control_down_was_altgr;

            m_control_down_was_altgr = is_altgr && is_keydown;

            if (is_altgr)
            {
                // Eat the key if one of our compose keys is AltGr
                if (Settings.ComposeKeys.Value.Contains(new Key(VK.RMENU)))
                    goto exit_discard_key;

                // Otherwise the keypress is not for us, ignore it
                goto exit_forward_key;
            }
        }

        // If Caps Lock is on, and the Caps Lock hack is enabled, we check
        // whether this key without Caps Lock gives a non-ASCII alphabetical
        // character. If so, we replace “result” with the lowercase or
        // uppercase variant of that character.
        if (has_capslock && Settings.CapsLockCapitalizes.Value)
        {
            Key alt_key = KeyboardLayout.VkToKey(vk, sc, flags, has_shift, has_altgr, false);

            if (alt_key.IsPrintable && alt_key.PrintableResult[0] > 0xbf)
            {
                string str_upper = alt_key.PrintableResult.ToUpper();
                string str_lower = alt_key.PrintableResult.ToLower();

                // Hack for German keyboards: it seems that ToUpper() does
                // not properly change ß into ẞ.
                if (str_lower == "ß")
                    str_upper = "ẞ";

                if (str_upper != str_lower)
                {
                    key = new Key(has_shift ? str_lower : str_upper);
                    is_capslock_hack = true;
                }
            }
        }

        // If we are being used to capture a key, send the resulting key.
        if (Captured != null)
        {
            if (is_keyup)
                Captured.Invoke(key);
            goto exit_discard_key;
        }

        // Update statistics
        if (is_keydown)
        {
            // Update single key statistics
            Stats.AddKey(key);

            // Update key pair statistics if applicable
            if (DateTime.Now < m_last_key_time.AddMilliseconds(2000)
                 && m_last_key != null)
            {
                Stats.AddPair(m_last_key, key);
            }

            // Remember when we pressed a key for the last time
            m_last_key_time = DateTime.Now;
            m_last_key = key;
        }

        // If the special Synergy window has focus, we’re actually sending
        // keystrokes to another computer; disable WinCompose. Same if it is
        // a Cygwin X window.
        if (KeyboardLayout.Window.IsOtherDesktop)
            goto exit_forward_key;

        // Sanity check in case the configuration changed between two
        // key events.
        if (CurrentComposeKey.VirtualKey != VK.NONE
             && !Settings.ComposeKeys.Value.Contains(CurrentComposeKey))
        {
            CurrentState = State.Idle;
            CurrentComposeKey = new Key(VK.NONE);
        }

        // Magic sequence handling. If the exact sequence is typed, we
        // enter compose state.
        if (m_magic_pos < m_magic_sequence.Count
             && m_magic_sequence[m_magic_pos].Key == key.VirtualKey
             && m_magic_sequence[m_magic_pos].Value == is_keydown)
        {
            ++m_magic_pos;
            Logger.Debug($"Magic key {m_magic_pos}/{m_magic_sequence.Count}");

            if (CurrentState == State.Idle && m_magic_pos == m_magic_sequence.Count)
            {
                CurrentState = State.Sequence;
                CurrentComposeKey = new Key(VK.NONE);
                Logger.Debug($"Magic sequence entered (state: {m_state})");
                m_magic_pos = 0;
                goto exit_forward_key;
            }
        }
        else
            m_magic_pos = 0;

        // If we receive a keyup for the compose key while in emulation
        // mode, we’re done. Send a KeyUp event and exit emulation mode.
        if (is_keyup && CurrentState == State.KeyCombination
             && key == CurrentComposeKey)
        {
            bool compose_key_was_altgr = m_compose_key_is_altgr;
            Key old_compose_key = CurrentComposeKey;
            CurrentState = State.Idle;
            CurrentComposeKey = new Key(VK.NONE);
            m_compose_key_is_altgr = false;
            m_compose_counter = 0;

            Logger.Debug("KeyCombination ended (state: {0})", m_state);

            // If relevant, send an additional KeyUp for the opposite
            // key; experience indicates that it helps unstick some
            // applications such as mintty.exe.
            switch (old_compose_key.VirtualKey)
            {
                case VK.LMENU: SendKeyUp(VK.RMENU); break;
                case VK.RMENU: SendKeyUp(VK.LMENU);
                               // If keyup is RMENU and we have AltGr on this
                               // keyboard layout, send LCONTROL up too.
                               if (compose_key_was_altgr)
                                   SendKeyUp(VK.LCONTROL);
                               break;
                case VK.LSHIFT: SendKeyUp(VK.RSHIFT); break;
                case VK.RSHIFT: SendKeyUp(VK.LSHIFT); break;
                case VK.LCONTROL: SendKeyUp(VK.RCONTROL); break;
                case VK.RCONTROL: SendKeyUp(VK.LCONTROL); break;
            }

            goto exit_forward_key;
        }

        // If this is the compose key and we’re idle, enter Sequence mode
        if (m_compose_counter == 0 && CurrentState == State.Idle
             && is_keydown && Settings.ComposeKeys.Value.Contains(key))
        {
            CurrentState = State.Sequence;
            CurrentComposeKey = key;
            m_compose_key_is_altgr = key.VirtualKey == VK.RMENU &&
                                     KeyboardLayout.HasAltGr;
            ++m_compose_counter;

            Logger.Debug("Now composing (state: {0}) (altgr: {1})",
                         m_state, m_compose_key_is_altgr);

            // Lauch the sequence reset expiration timer
            if (Settings.ResetTimeout.Value > 0)
            {
                m_timeout.Interval = TimeSpan.FromMilliseconds(Settings.ResetTimeout.Value);
                m_timeout.Start();
            }

            return true;
        }

        // If this is a compose key KeyDown event and it’s already down, or it’s
        // a KeyUp and it’s already up, eat this event without forwarding it.
        if (key == CurrentComposeKey
             && is_keydown == ((m_compose_counter & 1) != 0))
        {
            return true;
        }

        // Escape cancels the current sequence
        if (is_keydown && CurrentState == State.Sequence && key.VirtualKey == VK.ESCAPE)
        {
            // FIXME: if a sequence was in progress, maybe print it!
            ResetSequence();
            Logger.Debug("No longer composing (state: {0})", m_state);
            return true;
        }

        // Feature: emulate capslock key with both shift keys, and optionally
        // disable capslock using only one shift key.
        if (key.VirtualKey == VK.LSHIFT || key.VirtualKey == VK.RSHIFT)
        {
            if (is_keyup && has_lrshift && Settings.EmulateCapsLock.Value)
            {
                SendKeyPress(VK.CAPITAL);
                goto exit_forward_key;
            }

            if (is_keydown && has_capslock && Settings.ShiftDisablesCapsLock.Value)
            {
                SendKeyPress(VK.CAPITAL);
                goto exit_forward_key;
            }
        }

        // Feature: capslock is only active while being held
        if (key.VirtualKey == VK.CAPITAL && is_keyup && Settings.MustHoldCapsLock.Value
             && NativeMethods.GetKeyState(VK.CAPITAL) != 0)
        {
            SendKeyUp(VK.CAPITAL);
            SendKeyDown(VK.CAPITAL);
            goto exit_forward_key;
        }

        // If we are not currently composing a sequence, do nothing unless
        // one of our hacks forces us to send the key as a string (for
        // instance the Caps Lock capitalisation feature).
        if (CurrentState != State.Sequence)
        {
            if (is_capslock_hack && is_keydown)
            {
                SendString(key.PrintableResult);
                return true;
            }

            // If this was a dead key, it will be completely ignored. But
            // it’s okay since we stored it.
            goto exit_forward_key;
        }

        //
        // From this point we know we are composing
        //

        if (m_timeout.IsEnabled)
        {
            // Extend the expiration timer due date
            m_timeout.Stop();
            m_timeout.Start();
        }

        // If this is the compose key again, replace its value with our custom
        // virtual key.
        // FIXME: we don’t properly support compose keys that also normally
        // print stuff, such as `.
        if (key == CurrentComposeKey
             || (Settings.AlwaysCompose.Value && Settings.ComposeKeys.Value.Contains(key)))
        {
            ++m_compose_counter;
            key = new Key(VK.COMPOSE);

            // If the compose key is AltGr, we only add it to the sequence when
            // it’s a KeyUp event, otherwise we may be adding Multi_key to the
            // sequence while the user actually wants to enter an AltGr char.
            if (m_compose_key_is_altgr && m_compose_counter > 2)
                add_to_sequence = is_keyup;
        }

        // If the compose key is down and the user pressed a new key, maybe
        // instead of composing they want to do a key combination, such as
        // Alt+Tab or Windows+Up. So we abort composing and send the KeyDown
        // event for the Compose key that we previously discarded. The same
        // goes for characters that need AltGr when AltGr is the compose key.
        //
        // Never do this if the event is KeyUp.
        // Never do this if we already started a sequence
        // Never do this if the key is a modifier key such as shift or alt.
        if (m_compose_counter == 1 && is_keydown
             && m_sequence.Count == 0 && !key.IsModifier)
        {
            bool keep_original = Settings.KeepOriginalKey.Value;
            bool key_unusable = !key.IsUsable;
            bool altgr_combination = m_compose_key_is_altgr &&
                            KeyboardLayout.KeyToAltGrVariant(key) != null;

            if (keep_original || key_unusable || altgr_combination)
            {
                bool compose_key_was_altgr = m_compose_key_is_altgr;
                ResetSequence();
                if (compose_key_was_altgr)
                {
                    // It’s necessary to use KEYEVENTF_EXTENDEDKEY otherwise the system
                    // does not understand that we’re sending AltGr.
                    SendKeyDown(VK.LCONTROL);
                    SendKeyDown(VK.RMENU, KEYEVENTF.EXTENDEDKEY);
                }
                else
                {
                    SendKeyDown(CurrentComposeKey.VirtualKey);
                }
                CurrentState = State.KeyCombination;
                Logger.Debug("KeyCombination started (state: {0})", m_state);
                goto exit_forward_key;
            }
        }

        // If the compose key is AltGr and it’s down, check whether the current
        // key needs translating.
        if (m_compose_key_is_altgr && (m_compose_counter & 1) != 0)
        {
            Key altgr_variant = KeyboardLayout.KeyToAltGrVariant(key);
            if (altgr_variant != null)
            {
                key = altgr_variant;
                // Do as if we already released Compose, otherwise the next KeyUp
                // event will cause VK.COMPOSE to be added to the sequence…
                ++m_compose_counter;
            }
        }

        // If the key can't be used in a sequence, just ignore it.
        if (!key.IsUsable)
            goto exit_forward_key;

        // If we reached this point, everything else ignored this key, so it
        // is a key we should add to the current sequence.
        if (add_to_sequence)
        {
            Logger.Debug("Adding to sequence: “{0}”", key.FriendlyName);
            return AddToSequence(key);
        }

exit_discard_key:
        return true;

exit_forward_key:
        Logger.Debug("Forwarding {0} “{1}” to system (state: {2})",
                     is_keydown ? "⭝" : "⭜", key.FriendlyName, m_state);
        return false;
    }

    /// <summary>
    /// Add a key to the sequence currently being built. If necessary, output
    /// the finished sequence or trigger actions for invalid sequences.
    /// </summary>
    private static bool AddToSequence(Key key)
    {
        KeySequence old_sequence = new KeySequence(m_sequence);
        m_sequence.Add(key);

        // We try the following, in this order:
        //  1. if m_sequence + key is a valid sequence, we don't go further,
        //     we append key to m_sequence and output the result.
        //  2. if m_sequence + key is a valid prefix, it means the user
        //     could type other characters to build a longer sequence,
        //     so just append key to m_sequence.
        //  3. if m_sequence + key is a valid generic prefix, continue as well.
        //  4. if m_sequence + key is a valid sequence, send it.
        //  5. (optionally) try again 1. and 2. ignoring case.
        //  6. none of the characters make sense, output all of them as if
        //     the user didn't press Compose.
        foreach (bool ignore_case in Settings.CaseInsensitive.Value ?
                              new bool[]{ false, true } : new bool[]{ false })
        {
            if (Settings.IsValidSequence(m_sequence, ignore_case))
            {
                string tosend = Settings.GetSequenceResult(m_sequence,
                                                           ignore_case);
                SendString(tosend);
                Logger.Debug("Valid sequence! Sent “{0}”", tosend);
                Stats.AddSequence(m_sequence);
                ResetSequence();
                return true;
            }

            if (Settings.IsValidPrefix(m_sequence, ignore_case))
            {
                // Still a valid prefix, continue building sequence
                return true;
            }

            if (!ignore_case)
            {
                if (Settings.IsValidGenericPrefix(m_sequence))
                    return true;

                if (Settings.GetGenericSequenceResult(m_sequence, out var tosend))
                {
                    SendString(tosend);
                    Logger.Debug("Valid generic sequence! Sent “{0}”", tosend);
                    Stats.AddSequence(m_sequence);
                    ResetSequence();
                    return true;
                }
            }

            // Wait for a second character if SwapOnInvalid is true
            if (m_sequence.Count == 1 && Settings.SwapOnInvalid.Value)
                return true;

            // Try to swap characters if the corresponding option is set
            if (m_sequence.Count == 2 && Settings.SwapOnInvalid.Value)
            {
                var other_sequence = new KeySequence() { m_sequence[1], m_sequence[0] };
                if (Settings.IsValidSequence(other_sequence, ignore_case))
                {
                    string tosend = Settings.GetSequenceResult(other_sequence,
                                                               ignore_case);
                    SendString(tosend);
                    Logger.Debug("Found swapped sequence! Sent “{0}”", tosend);
                    Stats.AddSequence(other_sequence);
                    ResetSequence();
                    return true;
                }
            }
        }

        // FIXME: At this point we know the key can’t be part of a valid
        // sequence, but this should not be a hard error if the key is a
        // modifier key.
        OnInvalidSequence();
        return true;
    }

    private static void SendString(string str)
    {
        List<VK> modifiers = new List<VK>();

        // HACK: GTK+ applications will crash when receiving surrogate pairs through VK.PACKET,
        // so we use the Ctrl-Shift-u special sequence.
        bool use_gtk_hack = KeyboardLayout.Window.IsGtk
                             && str.Any(x => char.IsSurrogate(x));

        // HACK: in MS Office, some symbol insertions change the text font
        // without returning to the original font. To avoid this, we output
        // a space character, then go left, insert our actual symbol, then
        // go right and backspace.
        // These are the actual window class names for Outlook and Word…
        // TODO: PowerPoint ("PP(7|97|9|10)FrameClass")
        bool use_office_hack = KeyboardLayout.Window.IsOffice
                                && Settings.InsertZwsp.Value;

        InputSequence seq = new InputSequence();

        // Clear keyboard modifiers if we need one of our custom hacks
        if (use_gtk_hack || use_office_hack)
        {
            VK[] all_modifiers =
            {
                VK.LSHIFT, VK.RSHIFT,
                VK.LCONTROL, VK.RCONTROL,
                VK.LMENU, VK.RMENU,
                // Needs to be released, too, otherwise Caps Lock + é on
                // a French keyboard will print garbage if Caps Lock is
                // not released soon enough. See note below.
                VK.CAPITAL,
            };

            modifiers = all_modifiers.Where(x => (NativeMethods.GetKeyState(x) & 0x80) == 0x80)
                                     .ToList();
            modifiers.ForEach(vk => seq.AddKeyEvent(EventType.KeyUp, vk));
        }

        if (use_office_hack)
        {
            seq.AddUnicodeInput('\u200b');
            seq.AddKeyEvent(EventType.KeyUpDown, VK.LEFT);
        }

        for (int i = 0; i < str.Length; ++i)
        {
            char ch = str[i];

            if (ch == '\n' || ch == '\r')
            {
                // On some applications (e.g. Chrome or PowerPoint), \n cannot be injected
                // through its scancode, so we send the virtual key instead.
                seq.AddKeyEvent(EventType.KeyUpDown, VK.RETURN);
            }
            else if (use_gtk_hack && char.IsSurrogate(ch))
            {
                // Sanity check
                if (i + 1 >= str.Length || !char.IsHighSurrogate(ch) || !char.IsLowSurrogate(str, i + 1))
                    continue;

                var codepoint = char.ConvertToUtf32(ch, str[++i]);

                // GTK+ hack:
                //  - We need to disable Caps Lock
                //  - Wikipedia says Ctrl+Shift+u, release, then type the four hex digits,
                //    and press Enter (http://en.wikipedia.org/wiki/Unicode_input).
                //  - The Gimp accepts either Enter or Space but stops immediately in both
                //    cases.
                //  - Inkscape stops after Enter, but allows to chain sequences using Space.
                bool has_capslock = NativeMethods.GetKeyState(VK.CAPITAL) != 0;
                if (has_capslock)
                    seq.AddKeyEvent(EventType.KeyUpDown, VK.CAPITAL);

                seq.AddKeyEvent(EventType.KeyDown, VK.LCONTROL);
                seq.AddKeyEvent(EventType.KeyDown, VK.LSHIFT);
                seq.AddKeyEvent(EventType.KeyUpDown, VK.U);
                seq.AddKeyEvent(EventType.KeyUp, VK.LSHIFT);
                seq.AddKeyEvent(EventType.KeyUp, VK.LCONTROL);
                foreach (var key in $"{codepoint:X04}")
                    seq.AddKeyEvent(EventType.KeyUpDown, (VK)key);
                seq.AddKeyEvent(EventType.KeyUpDown, VK.RETURN);

                if (has_capslock)
                    seq.AddKeyEvent(EventType.KeyUpDown, VK.CAPITAL);
            }
            else
            {
                seq.AddUnicodeInput(ch);
            }
        }

        if (use_office_hack)
        {
            seq.AddKeyEvent(EventType.KeyUpDown, VK.RIGHT);
        }

        // Restore keyboard modifier state if we needed one of our custom hacks
        modifiers.ForEach(vk => seq.AddKeyEvent(EventType.KeyDown, vk));

        // Send the whole keyboard sequence
        seq.Send();
    }

    /// <summary>
    /// Broadcast state changes.
    /// </summary>
    public static event Action Changed;

    /// <summary>
    /// Allows other parts of the program to capture a key.
    /// TODO: make this work with key combinations, too!
    /// </summary>
    public static event Action<Key> Captured;

    private static void OnInvalidSequence()
    {
        // FIXME: what if the key is e.g. left arrow?
        var printable = string.Join("", m_sequence.Where(k => k.IsPrintable)
                                                  .Select(k => k.PrintableResult));

        // Unknown characters for sequence, print them if necessary
        if (!Settings.DiscardOnInvalid.Value && !string.IsNullOrEmpty(printable))
        {
            SendString(printable);
            Logger.Debug($"Invalid sequence! Sent “{printable}”");
        }

        // Emit a beep unless all keys in the sequence were the Compose key.
        if (Settings.BeepOnInvalid.Value && m_sequence.Any(k => k.VirtualKey != VK.COMPOSE))
            SystemSounds.Beep.Play();

        ResetSequence();
    }

    private static void ResetSequence()
    {
        CurrentState = State.Idle;
        m_compose_counter = 0;
        m_sequence.Clear();
    }

    private static void SendKeyDown(VK vk, KEYEVENTF flags = 0)
        => NativeMethods.keybd_event(vk, 0, flags, 0);

    private static void SendKeyUp(VK vk, KEYEVENTF flags = 0)
        => NativeMethods.keybd_event(vk, 0, KEYEVENTF.KEYUP | flags, 0);

    private static void SendKeyPress(VK vk, KEYEVENTF flags = 0)
    {
        SendKeyDown(vk, flags);
        SendKeyUp(vk, flags);
    }

    /// <summary>
    /// The sequence being currently typed
    /// </summary>
    private static KeySequence m_sequence = new KeySequence();

    /// <summary>
    /// The list of keys we have seen pressed and not yet released
    /// </summary>
    private static HashSet<Key> m_pressed = new HashSet<Key>();

    private static Key m_last_key;
    private static DateTime m_last_key_time = DateTime.Now;

    private static DispatcherTimer m_timeout;

    private static readonly TimeSpan NEVER = TimeSpan.FromMilliseconds(-1);

    /// <summary>
    /// How many times we pressed and released compose.
    /// </summary>
    private static int m_compose_counter = 0;

    /// <summary>
    /// Whether the last control keypress was AltGr
    /// </summary>
    private static bool m_control_down_was_altgr = false;

    /// <summary>
    /// Whether the current compose key is AltGr
    /// </summary>
    private static bool m_compose_key_is_altgr = false;

    public enum State
    {
        Idle,
        Sequence,
        /// <summary>
        /// KeyCombination mode: the compose key is held, but not for composing,
        /// more likely for a key combination such as Alt-Tab or Ctrl-Esc.
        /// </summary>
        KeyCombination,
        // TODO: we probably want "Disabled" as another possible state
    };

    public static State CurrentState
    {
        get => m_state;

        private set
        {
            bool has_changed = (m_state != value);
            if (has_changed && m_state == State.Sequence)
                m_timeout.Stop();
            m_state = value;
            if (has_changed)
                Changed?.Invoke();
        }
    }

    private static State m_state;

    /// <summary>
    /// The compose key being used; only valid in state “KeyCombination” for now.
    /// </summary>
    public static Key CurrentComposeKey { get; private set; } = new Key(VK.NONE);

    /// <summary>
    /// Indicates whether a compose sequence is in progress
    /// </summary>
    public static bool IsComposing => CurrentState == State.Sequence;

    private static int m_magic_pos;

    /// <summary>
    /// The magic sequence is a sequence of key down and key up events that are
    /// nearly impossible for a human to trigger. This may be used by keyboard
    /// drivers to trigger compose sequences.
    /// </summary>
    private static readonly List<KeyValuePair<VK, bool>> m_magic_sequence = new List<KeyValuePair<VK, bool>>()
    {
        new KeyValuePair<VK, bool>(VK.LSHIFT, true),
        new KeyValuePair<VK, bool>(VK.LMENU, true),
        new KeyValuePair<VK, bool>(VK.LCONTROL, true),
        new KeyValuePair<VK, bool>(VK.LMENU, false),
        new KeyValuePair<VK, bool>(VK.LMENU, true),
        new KeyValuePair<VK, bool>(VK.LSHIFT, false),
        new KeyValuePair<VK, bool>(VK.LCONTROL, false),
        new KeyValuePair<VK, bool>(VK.LMENU, false),
    };

    private static NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();
}

}
