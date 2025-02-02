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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace WinCompose
{
    public class Category
    {
        public string Name { get; protected set; }
        public string Icon { get; protected set; }

        public int RangeStart { get; protected set; } = -1;
        public int RangeEnd { get; protected set; } = -1;
    }

    public class TopCategory : Category
    {
        public TopCategory(string name)
        {
            Name = name;
        }
    }

    public class EmojiCategory : Category
    {
        public static readonly IEnumerable<EmojiCategory> AllCategories = GetAllCategories();

        public static EmojiCategory FromEmojiString(string str)
        {
            return LUT.TryGetValue(str, out var ret) ? ret : null;
        }

        private static IDictionary<string, EmojiCategory> LUT;

        private static IEnumerable<EmojiCategory> GetAllCategories()
        {
            LUT = new Dictionary<string, EmojiCategory>();

            var match_group = new Regex(@"^# group: (.*)");
            var match_sequence = new Regex(@"^([0-9A-F ]+[0-9A-F]).*");
            var list = new List<EmojiCategory>();

            using (var sr = new GZipResourceStream("emoji-test.txt.gz"))
            {
                EmojiCategory last_category = null;
                string buffer = sr.ReadToEnd();
                foreach (var line in buffer.Split('\r', '\n'))
                {
                    var m = match_group.Match(line);
                    if (m.Success)
                    {
                        last_category = new EmojiCategory() { Name = m.Groups[1].ToString() };
                        list.Add(last_category);
                    }

                    m = match_sequence.Match(line);
                    if (m.Success)
                    {
                        string value = "";
                        foreach (var codepoint in m.Groups[1].ToString().Split(' '))
                            value += char.ConvertFromUtf32(Convert.ToInt32(codepoint, 16));
                        LUT[value] = last_category;

                        // Set icon for this category
                        if (string.IsNullOrEmpty(last_category.Icon))
                            last_category.Icon = value;
                    }
                }
            }

            return list;
        }
    }

    public class CodepointCategory : Category
    {
        public static readonly IEnumerable<CodepointCategory> AllCategories = GetAllCategories();

        private static IEnumerable<CodepointCategory> GetAllCategories()
        {
            var list = new List<CodepointCategory>();

            using (var reader = new GZipResourceStream("Blocks.txt.gz"))
            {
                Regex r = new Regex(@"^([a-fA-F0-9]*)\.\.([a-fA-F0-9]*); ([A-Za-z0-9 \-]*)$");
                for (string l = reader.ReadLine(); l != null; l = reader.ReadLine())
                {
                    Match m = r.Match(l);
                    if (m.Success)
                    {
                        var start = Convert.ToInt32(m.Groups[1].Value, 16);
                        var end = Convert.ToInt32(m.Groups[2].Value, 16);
                        var name = (string)m.Groups[3].Value;
                        list.Add(new CodepointCategory() { Name = name, RangeStart = start, RangeEnd = end });
                    }
                }
            }

            list.Sort((x, y) => string.Compare(x.Name, y.Name, Thread.CurrentThread.CurrentCulture, CompareOptions.StringSort));
            return list;
        }
    }
}
