using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace DS3SavesBackup
{
    class Program
    {
        private static string[] localized;

        private static string nowToString()
        {
            DateTime now = DateTime.Now;
            return (now.Year.ToString() +
                (now.Month < 10 ? "0" : "") + now.Month.ToString() +
                (now.Day < 10 ? "0" : "") + now.Day.ToString() +
                (now.Hour < 10 ? "0" : "") + now.Hour.ToString() +
                (now.Minute < 10 ? "0" : "") + now.Minute.ToString() +
                (now.Second < 10 ? "0" : "") + now.Second.ToString());
        }

        private static void messWrite(string message, bool line)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(DateTime.Now.ToString().PadRight(25));
            Console.ResetColor();
            if (line) Console.WriteLine(message);
            else Console.Write(message);
        }

        private static void saveConfig(string appKey, string appValue)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings[appKey].Value = appValue;
            configuration.Save(ConfigurationSaveMode.Modified);
        }

        private static void mainLoop(string saves_path, string backup_path, int maxbkup, List<string> backupList)
        {
            Process[] ds3Process = Process.GetProcessesByName("DarkSoulsIII");
            if (ds3Process.Length > 0)
            {
                if (Directory.GetFiles(saves_path).Length == 0)
                {
                    messWrite(localized[10], true);
                }
                else
                {

                    messWrite(localized[6] + "...", false);
                    if (backupList.Count == maxbkup)
                    {
                        try
                        {
                            File.Delete(backupList.First());
                        }
                        catch (Exception e)
                        {
                            messWrite(e.ToString(), true);
                            Environment.Exit(0);
                        }
                        backupList.RemoveAt(0);
                    }
                    string backup = Path.Combine(backup_path, "ds3_" + nowToString() + ".zip");
                    backupList.Add(backup);
                    try
                    {
                        ZipFile.CreateFromDirectory(saves_path, backup, CompressionLevel.Fastest, false);
                    }
                    catch (Exception e)
                    {
                        messWrite(e.ToString(), true);
                        Environment.Exit(0);
                    }
                    Console.WriteLine(" " + localized[7] + ": " + Path.GetFileName(backup));
                }
            }
            else
            {
                messWrite(localized[8], true);
            }
        }

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("Dark Souls 3 Saves Backup Tool - 2018");
            Console.ResetColor();

            if (!File.Exists(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile))
            {
                messWrite("\nConfig file not found", true);
                Environment.Exit(0);
            }

            if (ConfigurationManager.AppSettings["language"] == "rus")
            {
                localized = new string[] { "Нажмите 'Q' для выхода",
                    "Путь к сохранениям",
                    "Путь к бэкапу",
                    "Копировать каждые",
                    "мин",
                    "Кол-во бэкапов",
                    "Создаем бэкап",
                    "готово",
                    "'Dark Souls 3' не запущен",
                    "Папки с сохранениями не существует. Запустите 'Dark Souls 3' в первый раз",
                    "Нечего копировать"};
            }
            else
            {
                localized = new string[] { "Press 'Q' for quit",
                    "Saves path",
                    "Backup path",
                    "Autobackup every",
                    "min",
                    "Max backups",
                    "Creating backup",
                    "done",
                    "'Dark Souls 3' is not running",
                    "Save folder didn't exist. Run 'Dark Souls 3' for the first time",
                    "Nothing to backup"};
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(localized[0] + "\n");
            Console.ResetColor();

            string savesPath, backupPath;
            int interval, maxBackup;

            try
            {
                interval = Convert.ToInt32(ConfigurationManager.AppSettings["auto_backup_interval_min"]);
                if (interval < 1)
                {
                    interval = 10;
                    saveConfig("auto_backup_interval_min", interval.ToString());
                }
            }
            catch
            {
                interval = 10;
                saveConfig("auto_backup_interval_min", interval.ToString());
            }

            try
            {
                maxBackup = Convert.ToInt32(ConfigurationManager.AppSettings["max_backups"]);
                if ((maxBackup < 2) || (maxBackup > 100))
                {
                    maxBackup = 10;
                    saveConfig("max_backups", maxBackup.ToString());
                }
            }
            catch
            {
                maxBackup = 10;
                saveConfig("max_backups", maxBackup.ToString());
            }

            try
            {
                savesPath = Path.GetFullPath(ConfigurationManager.AppSettings["saves_path"]);
            }
            catch
            {
                savesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DarkSoulsIII");
                saveConfig("saves_path", savesPath);
            }

            if (!Directory.Exists(savesPath))
            {
                messWrite(localized[9], true);
                Environment.Exit(0);
            }

            try
            {
                backupPath = Path.GetFullPath(ConfigurationManager.AppSettings["backup_path"]);
            }
            catch
            {
                backupPath = Path.GetFullPath("backups");
                saveConfig("backup_path", backupPath);
            }

            try
            {
                if (!Directory.Exists(backupPath)) Directory.CreateDirectory(backupPath);
            }
            catch (Exception e)
            {
                messWrite(e.ToString(), true);
                Environment.Exit(0);
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(localized[1] + ": " + savesPath, true);
            Console.WriteLine(localized[2] + ": " + backupPath, true);
            Console.WriteLine(localized[3] + ": " + interval.ToString() + " " + localized[4], true);
            Console.WriteLine(localized[5] + ": " + maxBackup.ToString() + "\n", true);
            Console.ResetColor();

            List<string> backupList = new List<string>();
            foreach (string filename in Directory.GetFiles(backupPath, "ds3*.zip", SearchOption.TopDirectoryOnly))
            {
                backupList.Add(filename);
            }

            Timer mTimer = new Timer((e) => { mainLoop(savesPath, backupPath, maxBackup, backupList); }, null, 0, interval * 1000 * 60);
            while (Console.ReadKey(true).Key != ConsoleKey.Q) { }
        }
    }
}
