//
//  WinCompose — a compose key for Windows — http://wincompose.info/
//
//  Copyright © 2013—2019 Sam Hocevar <sam@hocevar.net>
//
//  This program is free software. It comes without any warranty, to
//  the extent permitted by applicable law. You can redistribute it
//  and/or modify it under the terms of the Do What the Fuck You Want
//  to Public License, Version 2, as published by the WTFPL Task Force.
//  See http://www.wtfpl.net/ for more details.
//

using System.Threading;

namespace WinCompose.misc
{
    struct AtomicFlag
    {
        public void Set()
            => Interlocked.CompareExchange(ref m_flag, 1, 0);

        public bool Get()
            => Interlocked.CompareExchange(ref m_flag, 0, 1) == 1;

        private int m_flag;
    }
}

