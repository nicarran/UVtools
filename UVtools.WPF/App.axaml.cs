﻿/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Emgu.CV;
using UVtools.Core;
using UVtools.Core.FileFormats;
using UVtools.Core.Managers;
using UVtools.Core.SystemOS;
using UVtools.WPF.Extensions;
using UVtools.WPF.Structures;

namespace UVtools.WPF
{
    public class App : Application
    {
        //public static ThemeSelector ThemeSelector { get; set; }
        public static MainWindow MainWindow;
        public static FileFormat SlicerFile = null;

        public static AppVersionChecker VersionChecker { get; } = new();

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                UserSettings.Load();
                UserSettings.SetVersion();

                OperationProfiles.Load();

                MaterialManager.FilePath = Path.Combine(UserSettings.SettingsFolder, "materials.xml");
                MaterialManager.Load();

                /*ThemeSelector = ThemeSelector.Create(Path.Combine(ApplicationPath, "Assets", "Themes"));
                ThemeSelector.LoadSelectedTheme(Path.Combine(UserSettings.SettingsFolder, "selected.theme"));
                if (ThemeSelector.SelectedTheme.Name == "UVtoolsDark" || ThemeSelector.SelectedTheme.Name == "Light")
                {
                    foreach (var theme in ThemeSelector.Themes)
                    {
                        if (theme.Name != "UVtoolsLight") continue;
                        theme.ApplyTheme();
                        break;
                    }
                }*/

                if (!CvInvoke.Init())
                {
                    Console.WriteLine("UVtools can not init OpenCV library\n" +
                                      "Please build or install this dependencies in order to run UVtools\n" +
                                      "Check manual or page at 'Requirements' section for help");
                }

                MainWindow = new MainWindow();
                try
                {
                    if(!CvInvoke.Init())
                        await MainWindow.MessageBoxError("UVtools can not init OpenCV library\n" +
                                                     "Please build or install this dependencies in order to run UVtools\n" +
                                                     "Check manual or page at 'Requirements' section for help", 
                        "UVtools can not run");
                }
                catch (Exception e)
                {
                    await MainWindow.MessageBoxError("UVtools can not run due lack of dependencies from cvextern/OpenCV\n" +
                                                     "Please build or install this dependencies in order to run UVtools\n" +
                                                     "Check manual or page at 'Requirements' section for help\n\n" +
                                                     "Additional information:\n" +
                                                     $"{e}", "UVtools can not run");
                    return;
                }

                desktop.MainWindow = MainWindow;
                //desktop.Exit += (sender, e) => ThemeSelector.SaveSelectedTheme(Path.Combine(UserSettings.SettingsFolder, "selected.theme"));
            }

            base.OnFrameworkInitializationCompleted();
        }

        #region Utilities

        public static readonly string AppExecutable = Path.Combine(ApplicationPath, About.Software);
        public static readonly string AppExecutableQuoted = $"\"{AppExecutable}\"";
        public static void NewInstance(string filePath)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    SystemAware.StartProcess($"{AppExecutable}.exe", $"\"{filePath}\"");
                }
                else if(File.Exists(AppExecutable)) // Direct execute
                {
                    SystemAware.StartProcess(AppExecutable, $"\"{filePath}\"");
                }
                else
                {
                    SystemAware.StartProcess("dotnet", $"UVtools.dll \"{filePath}\"");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public static Stream GetAsset(string url)
        {
            Uri uri;

            // Allow for assembly overrides
            if (url.StartsWith("avares://"))
            {
                uri = new Uri(url);
            }
            else
            {
                var assemblyName = Assembly.GetEntryAssembly().GetName().Name;
                uri = new Uri($"avares://{assemblyName}{url}");
            }
            
            var res = AvaloniaLocator.Current.GetService<IAssetLoader>()?.Open(uri);
            return res;
        }

        public static Bitmap GetBitmapFromAsset(string url) => new(GetAsset(url));

        public static string ApplicationPath => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static string GetPrusaSlicerDirectory(bool isSuperSlicer = false)
        {
            var slicerFolder = isSuperSlicer ? "SuperSlicer" : "PrusaSlicer";
            if (OperatingSystem.IsWindows())
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    slicerFolder);
            }

            if (OperatingSystem.IsLinux())
            {
                var folder1 = Path.Combine(
                                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                        ".config",
                                        slicerFolder);
                if (Directory.Exists(folder1)) return folder1;
                return Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            $".{slicerFolder}");
            }

            if (OperatingSystem.IsMacOS())
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library",
                    "Application Support",
                    slicerFolder);
            }

            return null;
        }

        #endregion

        #region Assembly Attribute Accessors

        public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public static string VersionStr => Version.ToString(3);

        public static string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
            }
        }

        public static string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public static string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }

                string description = ((AssemblyDescriptionAttribute)attributes[0]).Description + $"{Environment.NewLine}{Environment.NewLine}Available File Formats:";

                return FileFormat.AvailableFormats.SelectMany(fileFormat => fileFormat.FileExtensions).Aggregate(description, (current, fileExtension) => current + $"{Environment.NewLine}- {fileExtension.Description} (.{fileExtension.Extension})");
            }
        }

        public static string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public static string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public static string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }

        #endregion
    }
}
