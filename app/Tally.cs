using System;
using System.IO;
using System.Windows.Forms;

namespace CapriDesktop
{
    internal sealed class Tally
    {
        internal const int Milestone = 100;
        private static readonly int[] Buttons = { 0x01, 0x02, 0x04, 0x05, 0x06 };
        private readonly string file;
        private readonly System.Windows.Forms.Timer watch = new System.Windows.Forms.Timer { Interval = 20 };
        private readonly bool[] held = new bool[256];
        private int unsaved;
        internal Action<long> Clicked;

        internal long Clicks { get; private set; }

        internal Tally(string folder)
        {
            file = Path.Combine(folder, "clicks.txt");
            long saved;
            try { if (File.Exists(file) && long.TryParse(File.ReadAllText(file).Trim(), out saved)) Clicks = saved; }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            watch.Tick += delegate { Check(); };
        }

        internal void Start()
        {
            for (int vk = 1; vk < 255; vk++) Native.GetAsyncKeyState(vk);
            watch.Start();
        }

        internal void Stop()
        {
            watch.Stop();
            Save();
        }

        internal void Reset()
        {
            Clicks = 0;
            Save();
        }

        private static bool Counted(int vk)
        {
            if (vk >= 0x01 && vk <= 0x06) return Array.IndexOf(Buttons, vk) >= 0;
            return vk >= 0x08 && vk != 0x10 && vk != 0x11 && vk != 0x12 && vk < 0xFF;
        }

        private void Check()
        {
            bool counted = false;
            for (int vk = 1; vk < 255; vk++)
            {
                if (!Counted(vk)) continue;
                bool mouse = vk <= 0x06;
                bool down = (Native.GetAsyncKeyState(vk) & (mouse ? 0x8001 : 0x8000)) != 0;
                bool pressed = down && !held[vk];
                held[vk] = down;
                if (!pressed) continue;
                Clicks++;
                unsaved++;
                counted = true;
            }
            if (!counted) return;
            if (unsaved >= 25) Save();
            if (Clicked != null) Clicked(Clicks);
        }

        private void Save()
        {
            unsaved = 0;
            try { File.WriteAllText(file, Clicks.ToString()); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
