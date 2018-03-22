﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Mapsui.Logging;
using Mapsui.Samples.Common.Desktop;
using Mapsui.Samples.Wpf.Utilities;
using Mapsui.Tests.Common;
using Mapsui.UI;

namespace Mapsui.Samples.Wpf
{
    public partial class Window1
    {
        public Window1()
        {
            InitializeComponent();
            MapControl.ErrorMessageChanged += MapErrorMessageChanged;
            MapControl.Hovered += MapControlOnMouseMove;
            MapControl.RotationLock = true;
            MapControl.UnSnapRotationDegrees = 30;
            MapControl.ReSnapRotationDegrees = 5;
  
            Logger.LogDelegate += LogMethod;

            FillComboBoxWithDemoSamples();

            SampleSet.SelectionChanged += SampleSetOnSelectionChanged;
            RenderMode.SelectionChanged += RenderModeOnSelectionChanged;
            var firstRadioButton = (RadioButton) SampleList.Children[0];
            firstRadioButton.IsChecked = true;
            firstRadioButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        private void MapControlOnHover(object sender, InfoEventArgs e)
        {
            FeatureInfo.Text = e.Feature == null ? "" : $"Hover Info:{Environment.NewLine}{e.Feature.ToDisplayText()}";
        }

        private void RenderModeOnSelectionChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
        {
            var selectedValue = ((ComboBoxItem) ((ComboBox) sender).SelectedItem).Content.ToString();

            if (selectedValue.ToLower().Contains("wpf"))
                MapControl.RenderMode = UI.Wpf.RenderMode.Wpf;
            else if (selectedValue.ToLower().Contains("skia"))
                MapControl.RenderMode = UI.Wpf.RenderMode.Skia;
            else
                throw new Exception("Unknown ComboBox item");
        }

        private void MapControlOnMouseMove(object sender, HoveredEventArgs e)
        {
            var screenPosition = e.ScreenPosition;
            var worldPosition = MapControl.Map.Viewport.ScreenToWorld(screenPosition.X, screenPosition.Y);
            MouseCoordinates.Text = $"{worldPosition.X:F0}, {worldPosition.Y:F0}";
        }

        private void FillComboBoxWithDemoSamples()
        {
            SampleList.Children.Clear();
            foreach (var sample in DemoSamples().ToList())
            {
                SampleList.Children.Add(CreateRadioButton(sample));
            }
        }

        private void FillComboBoxWithTestSamples()
        {
            SampleList.Children.Clear();
            foreach (var sample in TestSamples().ToList())
            {
                SampleList.Children.Add(CreateRadioButton(sample));
            }
        }

        private void SampleSetOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedValue = ((ComboBoxItem) ((ComboBox) sender).SelectedItem).Content.ToString();

            if (selectedValue == "Demo samples")
                FillComboBoxWithDemoSamples();
            else if (selectedValue == "Test samples")
                FillComboBoxWithTestSamples();
            else
                throw new Exception("Unknown ComboBox item");
        }

        private Dictionary<string, Func<Map>> TestSamples()
        {
            var result = new Dictionary<string, Func<Map>>();
            var i = 0;
            foreach (var sample in AllSamples.CreateList())
            {
                result[i.ToString()] = sample;
                i++;
            }
            return result;
        }

        private static Dictionary<string, Func<Map>> DemoSamples()
        {
            var allSamples = Common.AllSamples.CreateList();
            // Append samples from Mapsui.Desktop
            allSamples["Shapefile (Desktop)"] = ShapefileSample.CreateMap;
            allSamples["Shapefile Hover/Info (Desktop)"] = ShapefileHoverInfoSample.CreateMap;
            allSamples["Tiles on disk (Desktop)"] = MapTilerSample.CreateMap;
            allSamples["WMS (Desktop)"] = WmsSample.CreateMap;
            return allSamples;
        }

        private UIElement CreateRadioButton(KeyValuePair<string, Func<Map>> sample)
        {
            var radioButton = new RadioButton
            {
                FontSize = 16,
                Content = sample.Key,
                Margin = new Thickness(4)
            };

            radioButton.Click += (s, a) =>
            {
                MapControl.Map.Layers.Clear();
                MapControl.Map = sample.Value();
                MapControl.Map.Info += MapControlOnInfo;
                MapControl.Map.Hover += MapControlOnHover;
                LayerList.Initialize(MapControl.Map.Layers);
            };
            return radioButton;
        }

        readonly LimitedQueue<LogModel> _logMessage = new LimitedQueue<LogModel>(6); 

        private void LogMethod(LogLevel logLevel, string message, Exception exception)
        {
            _logMessage.Enqueue(new LogModel{Exception = exception, LogLevel = logLevel, Message = message});
            Dispatcher.Invoke(() => LogTextBox.Text = ToMultiLineString(_logMessage));
        }

        private string ToMultiLineString(LimitedQueue<LogModel> logMessages)
        {
            var result = new StringBuilder();

            var copy = logMessages.ToList();
            foreach (var logMessage in copy)
            {
                if (logMessage == null) continue;
                result.Append($"{logMessage.LogLevel} {logMessage.Message}{Environment.NewLine}");
            }

            return result.ToString();
        }

        private void MapErrorMessageChanged(object sender, EventArgs e)
        {
            LogTextBox.Text = MapControl.ErrorMessage; // todo: keep history
        }

        private void RotationSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var percent = RotationSlider.Value/(RotationSlider.Maximum - RotationSlider.Minimum);
            MapControl.Map.Viewport.Rotation = percent*360;
            MapControl.Refresh();
        }

        private void MapControlOnInfo(object sender, InfoEventArgs infoEventArgs)
        {
            if (infoEventArgs.Feature != null) 
                FeatureInfo.Text = $"Click Info:{Environment.NewLine}{infoEventArgs.Feature.ToDisplayText()}{Environment.NewLine}CTRL:{infoEventArgs.ModifierCtrl}, SHIFT:{infoEventArgs.ModifierShift}";
        }
    }
}