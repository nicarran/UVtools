﻿/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using UVtools.Core.Extensions;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationCalibrateElephantFoot : Operation
    {
        #region Members
        private Size _resolution = Size.Empty;
        private decimal _layerHeight = 0.05M;
        private bool _syncLayers;
        private ushort _bottomLayers = 10;
        private ushort _normalLayers = 70;
        private decimal _bottomExposure = 60;
        private decimal _normalExposure = 12;
        private byte _margin = 30;
        private bool _isErodeEnabled = true;
        private byte _erodeStartIteration = 2;
        private byte _erodeEndIteration = 6;
        private byte _erodeIterationSteps = 1;
        private bool _isDimmingEnabled = true;
        private byte _dimmingWallThickness = 20;
        private byte _dimmingStartBrightness = 140;
        private byte _dimmingEndBrightness = 200;
        private byte _dimmingBrightnessSteps = 20;
        private bool _outputOriginalPart = true;
        private bool _enableAntiAliasing = true;

        #endregion

        #region Overrides

        public override bool CanROI => false;

        public override bool CanCancel => false;

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.None;

        public override string Title => "Elephant foot";
        public override string Description =>
            "Generates test models with various strategies and increments to verify the best method/values to remove the elephant foot.\n" +
            "Elephant foot is removed when bottom layers are flush and aligned with normal layers.\n" +
            "You must repeat this test when change any of the following: printer, LEDs, resin and exposure times.\n" +
            "Note: The current opened file will be overwritten with this test, use a dummy or a not needed file.";

        public override string ConfirmationText =>
            $"generate the elephant foot test?";

        public override string ProgressTitle =>
            $"Generating the elephant foot test";

        public override string ProgressAction => "Generated";

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();

            if (_isErodeEnabled && _erodeStartIteration > _erodeEndIteration)
            {
                sb.AppendLine("Erode start iterations can't be higher than end iterations.");
            }

            if (_isDimmingEnabled && _dimmingStartBrightness > _dimmingEndBrightness)
            {
                sb.AppendLine("Wall dimming start brightness can't be higher than end brightness.");
            }

            if (ObjectCount <= 0)
            {
                sb.AppendLine("No objects to output, please adjust the settings.");
            }

            return new StringTag(sb.ToString());
        }

        public override string ToString()
        {
            var result = $"[Layer Height: {_layerHeight}] " +
                         $"[Layers: {_bottomLayers}/{_normalLayers}] " +
                         $"[Exposure: {_bottomExposure}/{_normalExposure}] " +
                         $"[Margin: {_margin}] [ORI: {_outputOriginalPart}]" +
                         $"[E: {_erodeStartIteration}-{_erodeEndIteration} S{_erodeIterationSteps}] " +
                         $"[D: W{_dimmingWallThickness} B{_dimmingStartBrightness}-{_dimmingEndBrightness} S{_dimmingBrightnessSteps}] " +
                         $"[AA: {_enableAntiAliasing}]";
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }

        #endregion

        #region Properties

        [XmlIgnore]
        public Size Resolution
        {
            get => _resolution;
            set => RaiseAndSetIfChanged(ref _resolution, value);
        }


        public decimal LayerHeight
        {
            get => _layerHeight;
            set
            {
                if(!RaiseAndSetIfChanged(ref _layerHeight, Math.Round(value, 2))) return;
                RaisePropertyChanged(nameof(BottomHeight));
                RaisePropertyChanged(nameof(NormalHeight));
                RaisePropertyChanged(nameof(TotalHeight));
            }
        }
        
        public ushort Microns => (ushort) (LayerHeight * 1000);

        public bool SyncLayers
        {
            get => _syncLayers;
            set
            {
                if(!RaiseAndSetIfChanged(ref _syncLayers, value)) return;
                if (_syncLayers)
                {
                    NormalLayers = _bottomLayers;
                }
            }
        }

        public ushort BottomLayers
        {
            get => _bottomLayers;
            set
            {
                if(!RaiseAndSetIfChanged(ref _bottomLayers, value)) return;
                if (_syncLayers)
                {
                    NormalLayers = _bottomLayers;
                }
                RaisePropertyChanged(nameof(BottomHeight));
                RaisePropertyChanged(nameof(TotalHeight));
                RaisePropertyChanged(nameof(LayerCount));
            }
        }

        public ushort NormalLayers
        {
            get => _normalLayers;
            set
            {
                if (!RaiseAndSetIfChanged(ref _normalLayers, value)) return;
                if (_syncLayers)
                {
                    BottomLayers = _normalLayers;
                }
                RaisePropertyChanged(nameof(NormalHeight));
                RaisePropertyChanged(nameof(TotalHeight));
                RaisePropertyChanged(nameof(LayerCount));
            }
        }

        public uint LayerCount => (uint)(_bottomLayers + _normalLayers);

        public decimal BottomHeight => Math.Round(LayerHeight * BottomLayers, 2);
        public decimal NormalHeight => Math.Round(LayerHeight * NormalLayers, 2);

        public decimal TotalHeight => BottomHeight + NormalHeight;

        public decimal BottomExposure
        {
            get => _bottomExposure;
            set => RaiseAndSetIfChanged(ref _bottomExposure, Math.Round(value, 2));
        }

        public decimal NormalExposure
        {
            get => _normalExposure;
            set => RaiseAndSetIfChanged(ref _normalExposure, Math.Round(value, 2));
        }

        public byte Margin
        {
            get => _margin;
            set => RaiseAndSetIfChanged(ref _margin, value);
        }

        public bool OutputOriginalPart
        {
            get => _outputOriginalPart;
            set
            {
                if(!RaiseAndSetIfChanged(ref _outputOriginalPart, value)) return;
                RaisePropertyChanged(nameof(ObjectCount));
            }
        }

        public bool EnableAntiAliasing
        {
            get => _enableAntiAliasing;
            set => RaiseAndSetIfChanged(ref _enableAntiAliasing, value);
        }

        public bool IsErodeEnabled
        {
            get => _isErodeEnabled;
            set
            {
                if(!RaiseAndSetIfChanged(ref _isErodeEnabled, value)) return;
                RaisePropertyChanged(nameof(ErodeObjects));
                RaisePropertyChanged(nameof(ObjectCount));
            }
        }

        public byte ErodeStartIteration
        {
            get => _erodeStartIteration;
            set
            {
                if(!RaiseAndSetIfChanged(ref _erodeStartIteration, value)) return;
                RaisePropertyChanged(nameof(ErodeObjects));
                RaisePropertyChanged(nameof(ObjectCount));
            }
        }

        public byte ErodeEndIteration
        {
            get => _erodeEndIteration;
            set
            {
                if(!RaiseAndSetIfChanged(ref _erodeEndIteration, value)) return;
                RaisePropertyChanged(nameof(ErodeObjects));
                RaisePropertyChanged(nameof(ObjectCount));
            }
        }

        public byte ErodeIterationSteps
        {
            get => _erodeIterationSteps;
            set
            {
                if(!RaiseAndSetIfChanged(ref _erodeIterationSteps, value)) return;
                RaisePropertyChanged(nameof(ErodeObjects));
                RaisePropertyChanged(nameof(ObjectCount));
            }
        }

        [XmlIgnore]
        public Kernel ErodeKernel { get; set; } = new();

        public bool IsDimmingEnabled
        {
            get => _isDimmingEnabled;
            set
            {
                if(!RaiseAndSetIfChanged(ref _isDimmingEnabled, value)) return;
                RaisePropertyChanged(nameof(DimmingObjects));
                RaisePropertyChanged(nameof(ObjectCount));
            }
        }

        public byte DimmingWallThickness
        {
            get => _dimmingWallThickness;
            set => RaiseAndSetIfChanged(ref _dimmingWallThickness, value);
        }

        public byte DimmingStartBrightness
        {
            get => _dimmingStartBrightness;
            set
            {
                if(!RaiseAndSetIfChanged(ref _dimmingStartBrightness, value)) return;
                RaisePropertyChanged(nameof(DimmingStartBrightnessPercent));
                RaisePropertyChanged(nameof(DimmingObjects));
                RaisePropertyChanged(nameof(ObjectCount));
            }
        }

        public float DimmingStartBrightnessPercent => (float) Math.Round(_dimmingStartBrightness * 100 / 255M, 2);

        public byte DimmingEndBrightness
        {
            get => _dimmingEndBrightness;
            set
            {
                if(!RaiseAndSetIfChanged(ref _dimmingEndBrightness, value)) return;
                RaisePropertyChanged(nameof(DimmingEndBrightnessPercent));
                RaisePropertyChanged(nameof(DimmingObjects));
                RaisePropertyChanged(nameof(ObjectCount));
            }
        }

        public float DimmingEndBrightnessPercent => (float)Math.Round(_dimmingEndBrightness * 100 / 255M, 2);

        public byte DimmingBrightnessSteps
        {
            get => _dimmingBrightnessSteps;
            set
            {
                if(!RaiseAndSetIfChanged(ref _dimmingBrightnessSteps, value)) return;
                RaisePropertyChanged(nameof(DimmingObjects));
                RaisePropertyChanged(nameof(ObjectCount));
            }
        }

        public int ErodeObjects => _isErodeEnabled ? 
            (int)Math.Floor((_erodeEndIteration - _erodeStartIteration) / (decimal)_erodeIterationSteps) + 1 
            : 0;

        public int DimmingObjects => _isDimmingEnabled ?
            (int) Math.Floor((_dimmingEndBrightness - _dimmingStartBrightness) / (decimal) _dimmingBrightnessSteps) + 1
            : 0;

        public int ObjectCount => (_outputOriginalPart ? 1 : 0) + ErodeObjects + DimmingObjects;

        #endregion

        #region Equality

        private bool Equals(OperationCalibrateElephantFoot other)
        {
            return _layerHeight == other._layerHeight && _syncLayers == other._syncLayers && _bottomLayers == other._bottomLayers && _normalLayers == other._normalLayers && _bottomExposure == other._bottomExposure && _normalExposure == other._normalExposure && _margin == other._margin && _isErodeEnabled == other._isErodeEnabled && _erodeStartIteration == other._erodeStartIteration && _erodeEndIteration == other._erodeEndIteration && _erodeIterationSteps == other._erodeIterationSteps && _isDimmingEnabled == other._isDimmingEnabled && _dimmingWallThickness == other._dimmingWallThickness && _dimmingStartBrightness == other._dimmingStartBrightness && _dimmingEndBrightness == other._dimmingEndBrightness && _dimmingBrightnessSteps == other._dimmingBrightnessSteps && _outputOriginalPart == other._outputOriginalPart && _enableAntiAliasing == other._enableAntiAliasing;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is OperationCalibrateElephantFoot other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(_layerHeight);
            hashCode.Add(_syncLayers);
            hashCode.Add(_bottomLayers);
            hashCode.Add(_normalLayers);
            hashCode.Add(_bottomExposure);
            hashCode.Add(_normalExposure);
            hashCode.Add(_margin);
            hashCode.Add(_isErodeEnabled);
            hashCode.Add(_erodeStartIteration);
            hashCode.Add(_erodeEndIteration);
            hashCode.Add(_erodeIterationSteps);
            hashCode.Add(_isDimmingEnabled);
            hashCode.Add(_dimmingWallThickness);
            hashCode.Add(_dimmingStartBrightness);
            hashCode.Add(_dimmingEndBrightness);
            hashCode.Add(_dimmingBrightnessSteps);
            hashCode.Add(_outputOriginalPart);
            hashCode.Add(_enableAntiAliasing);
            return hashCode.ToHashCode();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the bottom and normal layers, 0 = bottom | 1 = normal
        /// </summary>
        /// <returns></returns>
        public Mat[] GetLayers()
        {
            Mat[] layers = new Mat[2];
            var anchor = new Point(-1, -1);
            var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);

            layers[0] = EmguExtensions.InitMat(Resolution);
            LineType lineType = _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected;
            const int length = 250;
            const int triangleLength = 50;
            const byte startX = 2;
            const byte startY = 2;
            int x = startX;
            int y = startY;

            int maxX = x;
            int maxY = y;

            var pointList = new List<Point> { new(x, y) };

            void addPoint()
            {
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
                pointList.Add(new Point(x, y));
            }

            x += length;
            addPoint();

            x -= triangleLength;
            y += triangleLength;
            addPoint();

            y += 50;
            addPoint();

            x += triangleLength;
            y += triangleLength;
            addPoint();
            x -= triangleLength;
            y += triangleLength;
            addPoint();

            x += triangleLength;
            y += triangleLength;
            addPoint();
            x -= triangleLength;
            y += triangleLength;
            addPoint();

            x += triangleLength;
            addPoint();
            y += triangleLength;
            addPoint();
            x -= triangleLength;
            y += triangleLength;
            addPoint();

            x = startX;
            addPoint();


            const byte ellipseHeight = 50;
            
            maxY += ellipseHeight;
            using Mat shape = EmguExtensions.InitMat(new Size(maxX + startX, maxY + startY));
            CvInvoke.FillPoly(shape, new VectorOfPoint(pointList.ToArray()), EmguExtensions.WhiteByte, lineType);
            CvInvoke.Circle(shape, new Point(0, 0), length / 4, EmguExtensions.BlackByte,
                -1, lineType);
            CvInvoke.Ellipse(shape, new Point(maxX / 2, maxY - ellipseHeight), new Size(maxX / 3, ellipseHeight), 0, 0, 360,
                EmguExtensions.WhiteByte, -1, lineType);
            CvInvoke.Circle(shape, new Point(length / 2, maxY - 100), length / 5, EmguExtensions.BlackByte, -1, lineType);

            int currentX = 0;
            int currentY = 0;

            maxX = 0;

            const FontFace font = FontFace.HersheyDuplex;
            const byte fontStartX = 30;
            const byte fontStartY = length / 4 + 42;
            const double fontScale = 1.3;
            const byte fontThickness = 3;
            const byte fontMargin = 42;

            void addText(Mat mat, ushort number, params string[] text)
            {
                CvInvoke.PutText(mat, number.ToString(), new Point(100, 55), font, 1.5, EmguExtensions.BlackByte, 4, lineType);
                CvInvoke.PutText(mat, "UVtools EP", new Point(fontStartX, fontStartY), font, 0.8, EmguExtensions.BlackByte, 2, lineType);
                CvInvoke.PutText(mat, $"{Microns}um", new Point(fontStartX, fontStartY + fontMargin), font, fontScale, EmguExtensions.BlackByte, fontThickness, lineType);
                CvInvoke.PutText(mat, $"{BottomExposure}|{NormalExposure}s", new Point(fontStartX, fontStartY + fontMargin * 2), font, fontScale, EmguExtensions.BlackByte, fontThickness, lineType);
                if (text is null) return;
                for (var i = 0; i < text.Length; i++)
                {
                    CvInvoke.PutText(mat, text[i], new Point(fontStartX, fontStartY + fontMargin * (i + 3)), font,
                        fontScale, EmguExtensions.BlackByte, fontThickness, lineType);
                }

            }

            ushort count = 0;

            if (OutputOriginalPart)
            {
                using var roi = new Mat(layers[0], new Rectangle(new Point(currentX, currentY), shape.Size));
                shape.CopyTo(roi);
                addText(roi, ++count, "ORI");
            }
            else
            {
                currentX -= shape.Width + Margin;
            }


            layers[1] = layers[0].Clone();

            if (IsErodeEnabled)
            {
                for (int iteration = ErodeStartIteration;
                    iteration <= ErodeEndIteration;
                    iteration += ErodeIterationSteps)
                {
                    currentX += shape.Width + Margin;
                    maxX = Math.Max(maxX, currentX);

                    if (currentX + shape.Width >= layers[0].Width)
                    {
                        currentX = startX;
                        currentY += shape.Height + Margin;
                    }

                    if (currentY + shape.Height >= layers[0].Height)
                    {
                        break; // Insufficient size
                    }

                    count++;
                    using (var roi = new Mat(layers[1], new Rectangle(new Point(currentX, currentY), shape.Size)))
                    {
                        shape.CopyTo(roi);
                        addText(roi, count, $"E: {iteration}i");
                    }

                    using (var roi = new Mat(layers[0],
                        new Rectangle(new Point(currentX, currentY), shape.Size)))
                    using (var erode = new Mat())
                    {
                        CvInvoke.Erode(shape, erode, ErodeKernel.Matrix, ErodeKernel.Anchor, iteration, BorderType.Reflect101,
                            default);
                        erode.CopyTo(roi);
                        addText(roi, count, $"E: {iteration}i");
                    }
                }
            }

            if (IsDimmingEnabled)
            {
                for (int brightness = DimmingStartBrightness;
                    brightness <= DimmingEndBrightness;
                    brightness += DimmingBrightnessSteps)
                {
                    currentX += shape.Width + Margin;

                    if (currentX + shape.Width >= layers[0].Width)
                    {
                        currentX = 0;
                        currentY += shape.Height + Margin;
                    }

                    if (currentY + shape.Height >= layers[0].Height)
                    {
                        break; // Insufficient size
                    }

                    count++;
                    using (var roi = new Mat(layers[1], new Rectangle(new Point(currentX, currentY), shape.Size)))
                    {
                        shape.CopyTo(roi);
                        addText(roi, count, $"W: {DimmingWallThickness}", $"B: {brightness}");
                    }

                    using (var roi = new Mat(layers[0],
                        new Rectangle(new Point(currentX, currentY), shape.Size)))
                    using (var erode = new Mat())
                    using (var diff = new Mat())
                    using (var target = new Mat())
                    using (var mask = shape.CloneBlank())
                    {
                        mask.SetTo(new MCvScalar(brightness));
                        CvInvoke.Erode(shape, erode, kernel, anchor, DimmingWallThickness, BorderType.Reflect101,
                            default);
                        CvInvoke.Subtract(shape, erode, diff);
                        CvInvoke.BitwiseAnd(diff, mask, target);
                        CvInvoke.Add(erode, target, target);
                        target.CopyTo(roi);
                        addText(roi, count, $"W: {DimmingWallThickness}", $"B: {brightness}");
                    }
                }
            }

            // Preview
            //layers[2] = new Mat(layers[0], new Rectangle(0, 0, Math.Min(layers[0].Width, maxX), Math.Min(layers[0].Height, currentY)));

            return layers;
        }

        public Mat GetThumbnail()
        {
            Mat thumbnail = EmguExtensions.InitMat(new Size(400, 200), 3);
            var fontFace = FontFace.HersheyDuplex;
            var fontScale = 1;
            var fontThickness = 2;
            const byte xSpacing = 45;
            const byte ySpacing = 45;
            CvInvoke.PutText(thumbnail, "UVtools", new Point(140, 35), fontFace, fontScale, new MCvScalar(255, 27, 245), fontThickness + 1);
            CvInvoke.Line(thumbnail, new Point(xSpacing, 0), new Point(xSpacing, ySpacing + 5), new MCvScalar(255, 27, 245), 3);
            CvInvoke.Line(thumbnail, new Point(xSpacing, ySpacing + 5), new Point(thumbnail.Width - xSpacing, ySpacing + 5), new MCvScalar(255, 27, 245), 3);
            CvInvoke.Line(thumbnail, new Point(thumbnail.Width - xSpacing, 0), new Point(thumbnail.Width - xSpacing, ySpacing + 5), new MCvScalar(255, 27, 245), 3);
            CvInvoke.PutText(thumbnail, "Elephant Foot Cal.", new Point(xSpacing, ySpacing * 2), fontFace, fontScale, new MCvScalar(0, 255, 255), fontThickness);
            CvInvoke.PutText(thumbnail, $"{Microns}um @ {BottomExposure}s/{NormalExposure}s", new Point(xSpacing, ySpacing * 3), fontFace, fontScale, EmguExtensions.White3Byte, fontThickness);
            CvInvoke.PutText(thumbnail, $"{ObjectCount} Objects", new Point(xSpacing, ySpacing * 4), fontFace, fontScale, EmguExtensions.White3Byte, fontThickness);

            /*thumbnail.SetTo(EmguExtensions.Black3Byte);
                
                CvInvoke.Circle(thumbnail, new Point(400/2, 200/2), 200/2, EmguExtensions.White3Byte, -1);
                for (int angle = 0; angle < 360; angle+=20)
                {
                    CvInvoke.Line(thumbnail, new Point(400 / 2, 200 / 2), new Point((int)(400 / 2 + 100 * Math.Cos(angle * Math.PI / 180)), (int)(200 / 2 + 100 * Math.Sin(angle * Math.PI / 180))), new MCvScalar(255, 27, 245), 3);
                }
                
                thumbnail.Save("D:\\Thumbnail.png");*/
            return thumbnail;
        }

        #endregion
    }
}