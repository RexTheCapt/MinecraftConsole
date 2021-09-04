using System;
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
                return $"-Xmx{RamSize}G -jar {SelectedServerJar} nogui";
            }
        }
        private static string SelectedJava
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
        private static int RamSize
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
        private static bool AutoRestart
        {
            get
            {
                return _settings.GetBool(name: "AutoRestart", valueIfNull: true);
            }
            set
            {
                _settings.SetBool(name: "AutoRestart", value);
            }
        }
        private static string SelectedServerJar
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
        private static List<string> JavaPaths
        {
            get
            {
                JArray jArray = _settings.GetJarray("javaPaths");

                if (jArray == null)
                    return new List<string>();

                List<string> tmp = new();

                foreach (JToken token in jArray)
                    tmp.Add(token.ToString());

                return tmp;
            }
            set
            {
                _settings.SetStringList("javaPaths", value);
            }
        }
        private static bool _changeSettings;
        private static char seperator = Path.DirectorySeparatorChar;
        private static string profileName = "default";
        private static string profileRoot = "profiles";
        private static string profileDirectory => $"{profileRoot}{seperator}{profileName}";
        private static string profileProperties => $"{profileDirectory}{seperator}server.properties";

        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("--profile"))
                {
                    profileName = args[i + 1];
                    break;
                }
            }

        Restart:
            Console.ForegroundColor = ConsoleColor.Gray;
            if (_settings == null)
                _settings = new SettingsV2("MinecraftConsole", "MinecraftConsoleSettings", "RexTheCapt", SettingsV2.LocationEnum.Program);

            SetupProfile();
            SelectJavaPath();
            SelectRamSize();
            SelectServerJar();
            SetAutoRestart();
            StartServer();
            SaveProfile();
            _settings.Save();

            if (_changeSettings)
            {
                _changeSettings = false;
                goto Restart;
            }
        }

        private static void SetAutoRestart()
        {
            Console.CursorVisible = false;
            Console.WriteLine("Auto restart server?");

            (int Left, int Top) p = Console.GetCursorPosition();
            while (true)
            {
                Console.SetCursorPosition(p.Left, p.Top);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write("  < ");
                if (AutoRestart)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("TRUE ");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("FALSE");
                }
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(" >");

                ConsoleKeyInfo input = Console.ReadKey(true);
                if (input.Key == ConsoleKey.LeftArrow || input.Key == ConsoleKey.RightArrow)
                    AutoRestart = !AutoRestart;
                else if (input.Key == ConsoleKey.Enter)
                {
                    Console.CursorVisible = true;
                    Console.WriteLine();
                    return;
                }
            }
        }

        private static void SaveProfile()
        {
            if (File.Exists(profileProperties))
                File.Delete(profileProperties);
            File.Copy("server.properties", profileProperties);
        }

        private static void SetupProfile()
        {
            if (!Directory.Exists(profileRoot))
                Directory.CreateDirectory(profileRoot);

            if (!Directory.Exists(profileDirectory))
                Directory.CreateDirectory(profileDirectory);

            if (File.Exists($"{profileDirectory}{seperator}server.properties"))
            {
                if (File.Exists("server.properties"))
                    File.Delete("server.properties");

                File.Copy($"{profileDirectory}{seperator}server.properties", "server.properties");
            }
        }

        private static void StartServer()
        {
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Server starting with settings:\n" +
                    $"Java:    {SelectedJava}\n" +
                    $"Jar:     {SelectedServerJar}\n" +
                    $"RAM:     {RamSize}G\n" +
                    $"Restart: {(AutoRestart ? "TRUE" : "FALSE")}");
                Console.ForegroundColor = ConsoleColor.Gray;

                using (Process p = new())
                {
                    #region https://stackoverflow.com/questions/3633796/start-a-process-in-the-same-console
                    p.StartInfo = new ProcessStartInfo(SelectedJava, ExecutionString)
                    {
                        UseShellExecute = false
                    };

                    try
                    {
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

                        string logFile = $"{_settings.Directory}Error\\{DateTime.Now:yyyy-MM-dd HH-mm-ss}.log";
                        using (StreamWriter sw = new(logFile))
                        {
                            sw.WriteLine($"Type: {e.GetType()}\n" +
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
                DateTime endTime = DateTime.Now.AddSeconds(10);
                bool stop = false;
                while (endTime > DateTime.Now && AutoRestart)
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

                if (stop || !AutoRestart)
                    break;
            }
        }

        private static void SelectServerJar()
        {
            string[] jars = Directory.GetFiles(".\\", "*.jar");
            int index = 0;

            if (SelectedServerJar != null)
                for (int i = 0; i < jars.Length; i++)
                {
                    if (jars[i].Equals($".\\{SelectedServerJar}", StringComparison.OrdinalIgnoreCase))
                    {
                        index = i;
                        break;
                    }
                }

            Console.WriteLine("Please select java.");
            (int Left, int Top) = Console.GetCursorPosition();
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
                    SelectedServerJar = jars[index][2..];
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

                Console.SetCursorPosition(Left, Top);
            }
        }

        private static void SelectRamSize()
        {
            string inputString = $"{RamSize}";

            (int Left, int Top) pos = Console.GetCursorPosition();
            while (true)
            {
                Console.SetCursorPosition(pos.Left, pos.Top);
                Console.Write($"Ram size: {inputString}G");
                Console.CursorLeft--;
                ConsoleKeyInfo input = Console.ReadKey(true);

                if (input.Key == ConsoleKey.Backspace && inputString.Length > 0)
                {
                    inputString = inputString[0..^1];
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
                            RamSize = res;
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
        {
            List<Selection> javaSelections = GetJavaPaths();
            int index = 0;

            if (SelectedJava == null)
                index = 0;
            else
            {
                bool set = false;
                for (int i = 0; i < javaSelections.Count; i++)
                {
                    if (javaSelections[i].ToString().Equals(SelectedJava, StringComparison.OrdinalIgnoreCase))
                    {
                        index = i;
                        set = true;
                    }
                }

                if (!set)
                    index = 0;
            }

            string prompt = "Please select java. (Press A to add, D to delete)";
            Console.WriteLine(prompt);
            (int Left, int Top) = Console.GetCursorPosition();
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
                }

            RedoInput:
                ConsoleKeyInfo input = Console.ReadKey(true);
                if (input.Key == ConsoleKey.Enter)
                {
                    SelectedJava = javaSelections[index].ToString();
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
                        if (!JavaPaths.Contains(path))
                        {
                            var tmp = JavaPaths;
                            tmp.Add(path);
                            JavaPaths = tmp;
                        }
                    }

                    javaSelections = GetJavaPaths();
                }
                else if (input.Key == ConsoleKey.D)
                {
                    Selection selection = javaSelections[index];

                    for (int i = javaSelections.Count - 1; i >= 0; i--)
                        if (javaSelections[i].Path.Equals(selection.Path, StringComparison.OrdinalIgnoreCase))
                            javaSelections.RemoveAt(i);

                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"Removed \"{selection.Path}\"");
                    Console.WriteLine(prompt);
                    Left = Console.CursorLeft;
                    Top = Console.CursorTop;
                }
                else
                    goto RedoInput;

                Console.SetCursorPosition(Left, Top);
            }
        }

        private static List<Selection> GetJavaPaths()
        {
            List<Selection> selections = new();
            Selection home = GetJavaFromHome();
            if (home != null)
                selections.Add(home);
            //selections.Add(GetJavaFromRegistryKey());
            selections.AddRange(GetJavaFromDefaultInstallLocation());
            selections.AddRange(GetJavaFromSettings());
            return selections;
        }

        private static List<Selection> GetJavaFromDefaultInstallLocation()
        {
            List<Selection> installs = new();

            foreach (var dir in Directory.GetDirectories($@"C:\Program Files\Java"))
                if (File.Exists($@"{dir}\bin\java.exe"))
                    installs.Add(new Selection($@"{dir}\bin\java.exe", Selection.SelectionType.EnvironmentVariable));

            return installs;
        }

        private static List<Selection> GetJavaFromSettings()
        {
            List<Selection> selections = new();

            if (JavaPaths.Count == 0)
                return selections;

            for (int i = 0; i < JavaPaths.Count; i++)
                selections.Add(new Selection(JavaPaths[i].ToString(), Selection.SelectionType.Custom));

            return selections;
        }

        private static Selection GetJavaFromRegistryKey()
        {
            using (Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\JavaSoft\\Java Runtime Environment\\"))
            {
                string currentVersion = rk.GetValue("CurrentVersion").ToString();
                using (Microsoft.Win32.RegistryKey key = rk.OpenSubKey(currentVersion))
                {
                    string s = $"{key.GetValue("JavaHome").ToString()}\\bin\\java.exe";

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
