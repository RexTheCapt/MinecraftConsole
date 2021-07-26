using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Newtonsoft.Json.Linq;

using RtcLib;

namespace MinecraftConsole
{
    class Program
    {
        private static SettingsV2 _settings = null;
        private static string ExecutionString
        {
            get
            {
                return $"-Xmx{_ramSize}G -jar {_selectedServerJar} nogui";
            }
        }
        private static string _selectedJava
        {
            get
            {
                return _settings.GetString("SelectedJava");
            }
            set
            {
                _settings.SetString("SelectedJava", value);
            }
        }
        private static int _ramSize
        {
            get
            {
                return _settings.GetInt32(name: "ramSize", valueIfNull: 2);
            }
            set
            {
                _settings.SetInt32("ramSize", value);
            }
        }
        private static string _selectedServerJar
        {
            get
            {
                return _settings.GetString("SelectedJar");
            }
            set
            {
                _settings.SetString("SelectedJar", value);
            }
        }
        private static List<string> _javaPaths
        {
            get
            {
                JArray jArray = _settings.GetJarray("javaPaths");

                if (jArray == null)
                    return new List<string>();

                List<string> tmp = new List<string>();

                foreach (JToken token in jArray)
                    tmp.Add(token.ToString());

                return tmp;
            }
            set
            {
                _settings.SetStringList("javaPaths", value);
            }
        }
        private static bool _changeSettings = false;

        static void Main(string[] args)
        {
            Restart:
            Console.ForegroundColor = ConsoleColor.Gray;
            if (_settings == null)
                _settings = new SettingsV2("MinecraftConsole", "MinecraftConsoleSettings", "RexTheCapt", SettingsV2.LocationEnum.Program);
            //List<Selection> javaSelections = new List<Selection>();

            //Selection selection = GetJavaHome();
            //if (selection != null)
            //    javaSelections.Add(selection);

            //selection = GetJavaRegistryKey();
            //if (selection != null)
            //    javaSelections.Add(selection);

            //javaSelections.AddRange(GetJavaFromSettings());

            //SelectJavaPath(javaSelections);
            SelectJavaPath();
            SelectRamSize();
            SelectServerJar();
            StartServer();
            _settings.Save();
            if (_changeSettings)
                goto Restart;
        }

        private static void StartServer()
        {
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Server starting with settings:\n" +
                    $"Java: {_selectedJava}\n" +
                    $"Jar:  {_selectedServerJar}\n" +
                    $"RAM:  {_ramSize}G");
                Console.ForegroundColor = ConsoleColor.Gray;

                using (Process p = new Process())
                {
                    #region https://stackoverflow.com/questions/3633796/start-a-process-in-the-same-console
                    p.StartInfo = new ProcessStartInfo(_selectedJava, ExecutionString)
                    {
                        UseShellExecute = false
                    };

                    try { 
                    p.Start();
                    p.WaitForExit();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("---SERVER STOPPED---");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                    catch (Exception e)
                    {
                        if (!Directory.Exists($"{_settings.Directory}Error"))
                            Directory.CreateDirectory($"{_settings.Directory}Error");

                        string logFile = $"{_settings.Directory}Error\\{DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")}.log";
                        using (StreamWriter sw = new StreamWriter(logFile))
                        {
                            sw.WriteLine($"Type: {e.GetType().ToString()}\n" +
                                $"Message: {e.Message}\n" +
                                $"Stacktrace: {e.StackTrace}");
                        }

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"---FAILED---\n" +
                            $"Exception info:\n" +
                            $"  Name:           {e.GetType().Name}\n" +
                            $"  Message:        {e.Message}\n" +
                            $"  Saved location: {logFile}");
                        _changeSettings = true;
                        return;
                    }

                    #endregion
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                (int Left, int Top) pos = Console.GetCursorPosition();
                DateTime endTime = DateTime.Now.AddSeconds(10);
                bool stop = false;
                while (endTime > DateTime.Now)
                {
                    Console.Write($"\rServer restarting in {endTime - DateTime.Now}, press any button to shutdown or C to change settings.");

                    if (Console.KeyAvailable)
                    {
                        stop = true;
                        ConsoleKeyInfo key = Console.ReadKey(true);

                        if (key.Key == ConsoleKey.C)
                            _changeSettings = true;
                    }
                    
                    if (stop)
                        break;
                }

                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.Gray;

                if (stop)
                    break;
            }
        }

        private static void Server_Exited(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void Server_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            // You have to do this through the Dispatcher because this method is called by a different Thread
            //Dispatcher.Invoke(new Action(() =>
            //{
            //    ConsoleTextBlock.Text += e.Data + "\r\n";
            //    ConsoleScroll.ScrollToEnd();

            //}));

            throw new NotImplementedException();
        }

        private static void SelectServerJar()
        {
            string[] jars = Directory.GetFiles(".\\", "*.jar");
            int index = 0;

            if (_selectedServerJar != null)
                for (int i = 0; i < jars.Length; i++)
                {
                    if (jars[i].Equals($".\\{_selectedServerJar}", StringComparison.OrdinalIgnoreCase))
                    {
                        index = i;
                        break;
                    }
                }

            Console.WriteLine("Please select java.");
            (int Left, int Top) pos = Console.GetCursorPosition();
            while (true)
            {
                for (int i = 0; i < jars.Length; i++)
                {
                    string sel = jars[i].ToString();

                    if (i == index)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("  > ");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write("    ");
                    }

                    Console.Write(sel + "    \n");
                }

            RedoInput:
                ConsoleKeyInfo input = Console.ReadKey(true);
                if (input.Key == ConsoleKey.Enter)
                {
                    _selectedServerJar = jars[index].Substring(2);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }
                else if (input.Key == ConsoleKey.DownArrow)
                {
                    index++;

                    if (index >= jars.Length)
                        index = 0;
                }
                else if (input.Key == ConsoleKey.UpArrow)
                {
                    index--;

                    if (index < 0)
                        index = jars.Length - 1;
                }
                else
                    goto RedoInput;

                Console.SetCursorPosition(pos.Left, pos.Top);
            }
        }

        private static void SelectRamSize()
        {
            string inputString = $"{_ramSize}";

            (int Left, int Top) pos = Console.GetCursorPosition();
            while (true)
            {
                Console.SetCursorPosition(pos.Left, pos.Top);
                Console.Write($"Ram size: {inputString}G");
                Console.CursorLeft--;
                ConsoleKeyInfo input = Console.ReadKey(true);

                if (input.Key == ConsoleKey.Backspace && inputString.Length > 0)
                {
                    inputString = inputString.Substring(0, inputString.Length - 1);
                    Console.Write("  ");
                }
                else if (input.Key == ConsoleKey.Enter)
                {
                    if (string.IsNullOrWhiteSpace(inputString))
                    {
                        Console.SetCursorPosition(pos.Left, pos.Top);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Input is blank!                ");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        pos = Console.GetCursorPosition();
                    }
                    else
                    {
                        if (int.TryParse(inputString, out int res))
                        {
                            _ramSize = res;
                            Console.WriteLine("");
                            return;
                        }
                        else
                        {
                            Console.SetCursorPosition(pos.Left, pos.Top);
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Input \"{inputString}\" is not an number!");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            pos = Console.GetCursorPosition();
                        }
                    }
                }
                else if (input.Key != ConsoleKey.Backspace)
                {
                    inputString += input.KeyChar;
                }
            }
        }

        private static void SelectJavaPath()
        //private static void SelectJavaPath(List<Selection> javaSelections)
        {
            List<Selection> javaSelections = GetJavaPaths();
            int index = 0;

            if (_selectedJava == null)
                index = 0;
            else
            {
                bool set = false;
                for (int i = 0; i < javaSelections.Count; i++)
                {
                    if (javaSelections[i].ToString().Equals(_selectedJava, StringComparison.OrdinalIgnoreCase))
                    {
                        index = i;
                        set = true;
                    }
                }

                if (!set)
                    index = 0;
            }

            Console.WriteLine("Please select java. (Press A to add, D to delete)");
            (int Left, int Top) pos = Console.GetCursorPosition();
            while (true)
            {
                for (int i = 0; i < javaSelections.Count; i++)
                {
                    string sel = javaSelections[i].ToString();

                    if (i == index)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("  > ");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write("    ");
                    }

                    Console.Write(sel + "    \n");
                    #region unfinished code
                    //if (sel.Length < 32)
                    //{
                    //    int length = sel.Length;
                    //    Console.Write(sel);
                    //    while (length < 32)
                    //    {
                    //        Console.Write(" ");
                    //        length++;
                    //    }
                    //}
                    //else
                    #endregion
                }

            RedoInput:
                ConsoleKeyInfo input = Console.ReadKey(true);
                if (input.Key == ConsoleKey.Enter)
                {
                    _selectedJava = javaSelections[index].ToString();
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }
                else if (input.Key == ConsoleKey.DownArrow)
                {
                    index++;

                    if (index >= javaSelections.Count)
                        index = 0;
                }
                else if (input.Key == ConsoleKey.UpArrow)
                {
                    index--;

                    if (index < 0)
                        index = javaSelections.Count - 1;
                }
                else if (input.Key == ConsoleKey.A)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write("Path to add: ");
                    string path = Console.ReadLine().Replace("\"", "");

                    Console.CursorLeft = 0;
                    Console.CursorTop--;
                    while (Console.CursorLeft != Console.WindowWidth - 1)
                        Console.Write(" ");

                    if (File.Exists(path))
                    {
                        if (!_javaPaths.Contains(path))
                        {
                            var tmp = _javaPaths;
                            tmp.Add(path);
                            _javaPaths = tmp;
                        }
                    }

                    javaSelections = GetJavaPaths();
                }
                else
                    goto RedoInput;

                Console.SetCursorPosition(pos.Left, pos.Top);
            }
        }

        private static List<Selection> GetJavaPaths()
        {
            List<Selection> selections = new List<Selection>();
            Selection home = GetJavaFromHome();
            if (home != null)
                selections.Add(home);
            selections.Add(GetJavaFromRegistryKey());
            selections.AddRange(GetJavaFromSettings());
            return selections;
        }

        private static List<Selection> GetJavaFromSettings()
        {
            List<Selection> selections = new List<Selection>();
            
            if (_javaPaths.Count == 0)
                return selections;

            for (int i = 0; i < _javaPaths.Count; i++)
                selections.Add(new Selection(_javaPaths[i].ToString(), Selection.SelectionType.Custom));

            return selections;
        }

        private static Selection GetJavaFromRegistryKey()
        {
            using (Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\JavaSoft\\Java Runtime Environment\\"))
            {
                string currentVersion = rk.GetValue("CurrentVersion").ToString();
                using (Microsoft.Win32.RegistryKey key = rk.OpenSubKey(currentVersion))
                {
                    string s = key.GetValue("JavaHome").ToString();

                    if (s == null)
                        return null;
                    else
                        return new Selection(s, Selection.SelectionType.EnvironmentVariable);
                }
            }
        }

        private static Selection GetJavaFromHome()
        {
            string home = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (string.IsNullOrEmpty(home))
                return null;
            else
                return new Selection(home, Selection.SelectionType.EnvironmentVariable);
        }
    }
}
