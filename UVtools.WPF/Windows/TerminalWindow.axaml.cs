/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.IO;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using UVtools.Core;
using UVtools.WPF.Controls;

namespace UVtools.WPF.Windows
{
    public partial class TerminalWindow : WindowEx
    {
        private static readonly string DefaultTerminalText = $"> Welcome to {About.Software} interactive terminal.\n" +
                                                             "> Type in some commands in C# language to inject code.\n" +
                                                             "> Example 1: SlicerFile.FirstLayer\n" +
                                                             "> Example 2: SlicerFile.ExposureTime = 3\n\n";

        private string _terminalText = DefaultTerminalText;
        private string _commandText = string.Empty;

        private bool _multiLine = true;
        private bool _autoScroll = true;
        private bool _verbose;
        private bool _clearCommandAfterSend = true;
        
        private readonly TextBox _terminalTextBox;
        public ScriptState _scriptState;

        #region Properties

        public string TerminalText
        {
            get => _terminalText;
            set
            {
                if(!RaiseAndSetIfChanged(ref _terminalText, value)) return;
                if(_autoScroll) _terminalTextBox.CaretIndex = _terminalText.Length - 1;
            }
        }

        public string CommandText
        {
            get => _commandText;
            set => RaiseAndSetIfChanged(ref _commandText, value ?? string.Empty);
        }

        public bool MultiLine
        {
            get => _multiLine;
            set => RaiseAndSetIfChanged(ref _multiLine, value);
        }

        public bool AutoScroll
        {
            get => _autoScroll;
            set => RaiseAndSetIfChanged(ref _autoScroll, value);
        }

        public bool Verbose
        {
            get => _verbose;
            set => RaiseAndSetIfChanged(ref _verbose, value);
        }

        public bool ClearCommandAfterSend
        {
            get => _clearCommandAfterSend;
            set => RaiseAndSetIfChanged(ref _clearCommandAfterSend, value);
        }

        #endregion

        public TerminalWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            _terminalTextBox = this.FindControl<TextBox>("TerminalTextBox");
            AddHandler(DragDrop.DropEvent, (sender, e) =>
            {
                var text = e.Data.GetText();
                if (text is not null)
                {
                    CommandText = text;
                    return;
                }

                var fileNames = e.Data.GetFileNames();
                if (fileNames is not null)
                {
                    var sb = new StringBuilder();
                    foreach (var fileName in fileNames)
                    {
                        try
                        {
                            if (!File.Exists(fileName)) continue;
                            var fi = new FileInfo(fileName);
                            if(fi.Length > 5000000) continue; // 5Mb only!
                            sb.AppendLine(File.ReadAllText(fi.FullName));
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception);
                        }
                        
                    }
                    
                    CommandText = sb.ToString();
                }
            });

            DataContext = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void Clear()
        {
            TerminalText = DefaultTerminalText;
        }

        public async void SendCommand()
        {
            if (string.IsNullOrWhiteSpace(_commandText))
            {
                TerminalText += '\n';
                return;
            }

            var output = new StringBuilder(_terminalText);
            if (_verbose) output.AppendLine(_commandText);

            try
            {
                if (_scriptState is null)
                {
                    _scriptState = CSharpScript.RunAsync(_commandText,
                        ScriptOptions.Default
                            .AddReferences(typeof(About).Assembly)
                            .AddImports(
                                "System",
                                "System.Collections.Generic",
                                "System.Math",
                                "System.IO",
                                "System.Linq",
                                "System.Threading",
                                "System.Threading.Tasks",
                                "UVtools.Core",
                                "UVtools.Core.Extensions",
                                "UVtools.Core.FileFormats",
                                "UVtools.Core.Layers",
                                "UVtools.Core.Objects",
                                "UVtools.Core.Operations")
                            .WithAllowUnsafe(true), this).Result;
                }
                else
                {
                    _scriptState = await _scriptState.ContinueWithAsync(_commandText);
                }

                if (_scriptState.ReturnValue is not null)
                {
                    output.AppendLine(_scriptState.ReturnValue.ToString());
                }
                else if (_scriptState.Exception is not null)
                {
                    output.AppendLine(_scriptState.Exception.ToString());
                }
                else if(!_verbose)
                {
                    output.AppendLine(_commandText);
                }
            }
            catch (Exception e)
            {
                output.AppendLine(e.Message);
            }
            
            TerminalText = output.ToString();

            if (_clearCommandAfterSend) CommandText = string.Empty;
        }
    }
}
