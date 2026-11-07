using System;
using System.IO;
using System.Windows.Forms;

namespace CapriDesktop
{
    internal sealed class Tally
    {
        internal const int Milestone = 100;
        private static readonly int[] Buttons = { 0x01, 0x02, 0x04 };
        private readonly string file;
        private readonly System.Windows.Forms.Timer watch = new System.Windows.Forms.Timer { Interval = 20 };
        private readonly bool[] held = new bool[3];
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
            foreach (int vk in Buttons) Native.GetAsyncKeyState(vk);
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

        private void Check()
        {
            for (int i = 0; i < Buttons.Length; i++)
            {
                bool down = (Native.GetAsyncKeyState(Buttons[i]) & 0x8001) != 0;
                bool pressed = down && !held[i];
                held[i] = down;
                if (!pressed) continue;
                Clicks++;
                unsaved++;
                if (unsaved >= 25) Save();
                if (Clicked != null) Clicked(Clicks);
            }
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
