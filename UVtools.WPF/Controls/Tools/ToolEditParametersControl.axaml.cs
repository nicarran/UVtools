﻿using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;
using UVtools.WPF.Extensions;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolEditParametersControl : ToolControl
    {
        public OperationEditParameters Operation => BaseOperation as OperationEditParameters;

        public RowControl[] RowControls;
        private Grid grid;

        public sealed class RowControl
        {
            public FileFormat.PrintParameterModifier Modifier { get; }

            public TextBlock Name { get; }
            public TextBlock OldValue { get; }
            public NumericUpDown NewValue { get; }
            public TextBlock Unit { get; }
            public Button ResetButton { get; }

            public RowControl(FileFormat.PrintParameterModifier modifier)
            {
                Modifier = modifier;

                modifier.NewValue = modifier.OldValue.Clamp(modifier.Minimum, modifier.Maximum);
                
                Name = new TextBlock
                {
                    Text = $"{modifier.Name}:",
                    VerticalAlignment = VerticalAlignment.Center,
                    Padding = new Thickness(15, 0),
                    Tag = this,
                };

                if(!string.IsNullOrWhiteSpace(modifier.Description)) ToolTip.SetTip(Name, modifier.Description);

                OldValue = new TextBlock
                {
                    Text = modifier.OldValue.ToString(CultureInfo.InvariantCulture),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    //Padding = new Thickness(15, 0),
                    Tag = this
                };

                NewValue = new NumericUpDown
                {
                    //DecimalPlaces = modifier.DecimalPlates,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Minimum = (double) modifier.Minimum,
                    Maximum = (double) modifier.Maximum,
                    Increment = modifier.Increment,
                    Value = (double)modifier.NewValue,
                    Tag = this,
                    //Width = 100,
                    ClipValueToMinMax = true
                };
                if (modifier.DecimalPlates > 0)
                {
                    NewValue.FormatString = $"F{modifier.DecimalPlates}";
                }

                Unit = new TextBlock
                {
                    Text = modifier.ValueUnit,
                    VerticalAlignment = VerticalAlignment.Center,
                    Padding = new Thickness(10, 0, 15, 0),
                    Tag = this
                };

                ResetButton = new Button
                {
                    IsVisible = false,
                    IsEnabled = false,
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = this,
                    Padding = new Thickness(5),
                    Content = new Image {Source = App.GetBitmapFromAsset("/Assets/Icons/undo-16x16.png")},
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                ResetButton.Click += ResetButtonOnClick;
                NewValue.ValueChanged += NewValueOnValueChanged;
            }

            private void NewValueOnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
            {
                Modifier.NewValue = (decimal) NewValue.Value;
                ResetButton.IsVisible = ResetButton.IsEnabled = Modifier.HasChanged;
            }

            private void ResetButtonOnClick(object? sender, RoutedEventArgs e)
            {
                NewValue.Value = (double) Modifier.OldValue;
                NewValue.Focus();
            }
        }

        public ToolEditParametersControl()
        {
            BaseOperation = new OperationEditParameters(SlicerFile);
            if (!ValidateSpawn()) return;
            InitializeComponent();
            
            grid = this.FindControl<Grid>("grid");
        }

        public void PopulateGrid()
        {
            const byte cols = 5;
            if (grid.Children.Count > cols)
            {
                grid.Children.RemoveRange(cols, grid.Children.Count - cols);
            }
            if (grid.RowDefinitions.Count > 1)
            {
                grid.RowDefinitions.RemoveRange(1, grid.RowDefinitions.Count-1);
            }

            int rowIndex = 1;
            RowControls = new RowControl[Operation.Modifiers.Length];
            //table.RowCount = Operation.Modifiers.Length+1;
            foreach (var modifier in Operation.Modifiers)
            {
                grid.RowDefinitions.Add(new RowDefinition());
                byte column = 0;

                var rowControl = new RowControl(modifier);
                grid.Children.Add(rowControl.Name);
                grid.Children.Add(rowControl.OldValue);
                grid.Children.Add(rowControl.NewValue);
                grid.Children.Add(rowControl.Unit);
                grid.Children.Add(rowControl.ResetButton);
                Grid.SetRow(rowControl.Name, rowIndex);
                Grid.SetColumn(rowControl.Name, column++);

                Grid.SetRow(rowControl.OldValue, rowIndex);
                Grid.SetColumn(rowControl.OldValue, column++);

                Grid.SetRow(rowControl.NewValue, rowIndex);
                Grid.SetColumn(rowControl.NewValue, column++);

                Grid.SetRow(rowControl.Unit, rowIndex);
                Grid.SetColumn(rowControl.Unit, column++);

                Grid.SetRow(rowControl.ResetButton, rowIndex);
                Grid.SetColumn(rowControl.ResetButton, column++);
                /*table.Controls.Add(rowControl.Name, column++, rowIndex);
                table.Controls.Add(rowControl.OldValue, column++, rowIndex);
                table.Controls.Add(rowControl.NewValue, column++, rowIndex);
                table.Controls.Add(rowControl.Unit, column++, rowIndex);
                table.Controls.Add(rowControl.ResetButton, column++, rowIndex);
                */
                RowControls[rowIndex - 1] = rowControl;

                rowIndex++;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void Callback(ToolWindow.Callbacks callback)
        {
            switch (callback)
            {
                case ToolWindow.Callbacks.Init:
                case ToolWindow.Callbacks.Loaded:
                    if (callback is ToolWindow.Callbacks.Init)
                    {
                        ParentWindow.SelectCurrentLayer();
                        ParentWindow.LayerRangeSync = true;
                    }

                    PopulateGrid();
                    Operation.PropertyChanged += OperationOnPropertyChanged;
                    break;
            }
        }

        private void OperationOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Operation.LayerIndexStart) && Operation.PerLayerOverride)
            {
                SlicerFile.RefreshPrintParametersPerLayerModifiersValues(Operation.LayerIndexStart);
                PopulateGrid();
                return;
            }
            if (e.PropertyName == nameof(Operation.PerLayerOverride))
            {
                if (Operation.PerLayerOverride)
                {
                    Operation.Modifiers = App.SlicerFile.PrintParameterPerLayerModifiers;
                    SlicerFile.RefreshPrintParametersPerLayerModifiersValues(Operation.LayerIndexStart);
                }
                else
                {
                    Operation.Modifiers = App.SlicerFile.PrintParameterModifiers;
                    SlicerFile.RefreshPrintParametersModifiersValues();
                }

                ParentWindow.LayerRangeVisible = Operation.PerLayerOverride;
                PopulateGrid();
                return;
            }
        }
    }
}
