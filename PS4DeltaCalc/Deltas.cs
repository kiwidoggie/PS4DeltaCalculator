using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PS4DeltaCalc
{
    public class Deltas
    {
        public class Entry
        {
            public string Name { get; set; }
            public ulong Start { get; set; }
            public ulong End { get; set; }
            public uint Size { get; set; }
            public byte[] Data { get; set; }
            public string HexString { get; set; }

            // Found
            public ulong DeltaStart { get; set; }
            public string Pattern { get; set; }
            public string Mask { get; set; }
            public Entry(string p_Line)
            {
                var s_Entries = p_Line.Split('|');
                if (s_Entries.Length != 5)
                    throw new InvalidDataException("The entry line does not match excpected format.");

                Name = s_Entries[0];
                Start = ulong.Parse(s_Entries[1]);
                End = ulong.Parse(s_Entries[2]);
                Size = uint.Parse(s_Entries[3]);
                HexString = s_Entries[4];
                Data = StringToByteArray(s_Entries[4]);
            }

            public override string ToString()
            {
                return Name;
            }
            // http://stackoverflow.com/questions/321370/how-can-i-convert-a-hex-string-to-a-byte-array
            public static byte[] StringToByteArray(string hex)
            {
                return Enumerable.Range(0, hex.Length)
                                 .Where(x => x % 2 == 0)
                                 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                                 .ToArray();
            }
        }

        private List<Entry> m_OriginalEntries;
        private List<Entry> m_ChangedEntries;

        public void ReadOriginalLog(string p_Path)
        {
            m_OriginalEntries = ReadLogFile(p_Path);
        }

        public void ReadChangedLog(string p_Path)
        {
            m_ChangedEntries = ReadLogFile(p_Path);
        }

        private List<Entry> ReadLogFile(string p_Path)
        {
            var s_List = new List<Entry>();

            if (!File.Exists(p_Path))
                return s_List;

            var s_Lines = File.ReadAllLines(p_Path);

            foreach (var l_Line in s_Lines)
            {
                if (l_Line.StartsWith("#"))
                    continue;

                s_List.Add(new Entry(l_Line));
            }

            return s_List;
        }

        public void Compare(string p_Log = "dump.log")
        {
            var s_Logs = new List<string>();
            s_Logs.Add("# PS4DeltaCalc by kiwidog (http://kiwidog.me)");

            s_Logs.Add("# Name | Pattern | Mask | Changed Address");
            // Iterate through all entries, and try to find a near match
            var s_Total = (double)(m_OriginalEntries.Count == 0 ? 1.0 : m_OriginalEntries.Count);
            var s_Current = 0.0;
            var s_Percentage = s_Current / s_Total;

            for (var i = 0; i < m_OriginalEntries.Count; ++i)
            {
                s_Current = i + 1;
                s_Percentage = s_Current / s_Total;

                Console.WriteLine($"{s_Percentage * 100}% completed.");

                var l_Entry = m_OriginalEntries[i];
                if (l_Entry.Name.StartsWith("sub_"))
                    continue;

                var l_Changed = FindChangedEntry(l_Entry);
                if (l_Changed == null)
                    continue;

                var l_Data = string.Empty;
                var l_Mask = string.Empty;

                CreatePattern(l_Entry, l_Changed, out l_Data, out l_Mask);

                s_Logs.Add($"{l_Entry.Name}|{l_Data}|{l_Mask}|{l_Changed.Start}");
            }

            // Write the log to file
            File.WriteAllLines(p_Log, s_Logs.ToArray());
        }

        private Entry FindChangedEntry(Entry p_OriginalEntry)
        {
            var s_Entry = m_ChangedEntries.FirstOrDefault(p_Entry => p_OriginalEntry.Name.Equals(p_Entry.Name));

            // We could not find it by name, search via hex bytes
            if (s_Entry == null)
            {
                s_Entry = FindBestMatch(p_OriginalEntry);
                if (s_Entry == null)
                {
                    Console.WriteLine($"Could not find changed function {p_OriginalEntry.Name}.");
                    return null;
                }

                Console.WriteLine($"Found {p_OriginalEntry.Name} by similarity.");
                return s_Entry;
            }

            Console.WriteLine($"Found {p_OriginalEntry.Name} by name.");
            return s_Entry;
        }
        private Entry FindBestMatch(Entry p_OriginalEntry)
        {
            var s_FirstFewBytes = p_OriginalEntry.HexString.Substring(0, 10);

            var s_FilteredEntries = m_ChangedEntries.Where(p_Entry => p_Entry.HexString.StartsWith(s_FirstFewBytes)).ToList();

            var s_Value = 9999;
            Entry s_FoundEntry = null;

            for (var i = 0; i < s_FilteredEntries.Count; ++i)
            {
                var l_Entry = s_FilteredEntries[i];
                var l_Changes = Compute(p_OriginalEntry.HexString, l_Entry.HexString);

                if (l_Changes < s_Value)
                {
                    s_Value = l_Changes;
                    s_FoundEntry = l_Entry;
                }

                if (l_Changes == 0)
                    break;
            }

            if (s_Value == 9999 || s_FoundEntry == null)
            {
                Console.WriteLine($"Function {p_OriginalEntry.Name} was not found.");
                return null;
            }

            return s_FoundEntry;
        }
        private void CreatePattern(Entry p_OriginalEntry, Entry p_ChangedEntry, out string p_Bytes, out string p_Mask)
        {
            p_Bytes = string.Empty;
            p_Mask = string.Empty;

            if (p_OriginalEntry == null || p_ChangedEntry == null)
            {
                Console.WriteLine("Original or Changed entry is null.");
                return;
            }

            var s_SearchLength = Math.Min(p_OriginalEntry.Data.Length, p_ChangedEntry.Data.Length);

            // Iterate through the data, checking each byte to see if it matches
            for (var i = 0; i < s_SearchLength; ++i)
            {
                // If we match then add the actual byte data representation and an "x" for match
                if (p_OriginalEntry.Data[i] == p_ChangedEntry.Data[i])
                {
                    p_Bytes += "\\x" + p_OriginalEntry.Data[i].ToString("X2");
                    p_Mask += "x";
                }
                else // Otherwise we put in a wildcard.
                {
                    p_Bytes += "\\x00";
                    p_Mask += "?";
                }
            }

        }

        private static int Compute(string s, string t)
        {
            if (s == t) return 0;
            if (s.Length == 0) return t.Length;
            if (t.Length == 0) return s.Length;
            var tLength = t.Length;
            var columns = tLength + 1;
            var v0 = new int[columns];
            var v1 = new int[columns];
            for (var i = 0; i < columns; i++)
                v0[i] = i;
            for (var i = 0; i < s.Length; i++)
            {
                v1[0] = i + 1;
                for (var j = 0; j < tLength; j++)
                {
                    var cost = (s[i] == t[j]) ? 0 : 1;
                    v1[j + 1] = Math.Min(Math.Min(v1[+j] + 1, v0[j + 1] + 1), v0[j] + cost);
                    v0[j] = v1[j];
                }
                v0[tLength] = v1[tLength];
            }
            return v1[tLength];
        }

    }
}
