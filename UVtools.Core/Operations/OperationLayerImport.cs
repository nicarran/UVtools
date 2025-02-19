﻿/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationLayerImport : Operation
    {
        #region Enums
        public enum ImportTypes : byte
        {
            [Description("Insert: Inserts the new imported layers")]
            Insert,
            [Description("Replace: Replaces layers with the imported layers")]
            Replace,
            [Description("Stack: Stacks and combine imported layers in the current layers")]
            Stack,
            [Description("MergeSum: Merges current layers with the imported content by summing value of layer pixels")]
            MergeSum,
            [Description("MergeMax: Merges current layers with the imported content by using the maximum value of layer pixels")]
            MergeMax,
            [Description("Subtract: Subtracts current layers with the imported content")]
            Subtract,
            [Description("AbsDiff: Absolute difference between current layers and the imported content")]
            AbsDiff,
            [Description("BitwiseAnd: Perform a 'bitwise AND' operation over layers and the imported pixels")]
            BitwiseAnd,
            [Description("BitwiseOr: Perform a 'bitwise OR' operation over layers and the imported pixels")]
            BitwiseOr,
            [Description("BitwiseXOr: Perform a 'bitwise XOR' operation over layers and the imported pixels")]
            BitwiseXOr,
        }
        #endregion

        #region Members
        private ImportTypes _importType = ImportTypes.Stack;
        private uint _startLayerIndex;
        private bool _extendBeyondLayerCount = true;
        private bool _discardUnmodifiedLayers;
        private ushort _stackMargin = 50;
        private RangeObservableCollection<ValueDescription> _files = new();
        #endregion

        #region Overrides

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.None;
        public override bool CanROI => false;
        public override string Title => "Import layers";

        public override string Description =>
            "Import layers from local files into the model at a selected layer height.\n" +
            "NOTE: Imported images must be greyscale and have the same resolution as the model.";

        public override string ConfirmationText => $"{_importType} import {Count} file{(Count>=1?"s":"")}?";

        public override string ProgressTitle =>
            $"{_importType} importing {Count} file{(Count>=1 ? "s" : "")}";

        public override string ProgressAction => "Imported layers";

        public override bool CanCancel => true;

        public override uint LayerIndexEnd => _startLayerIndex + Count - 1;

        public override bool CanHaveProfiles => false;

        public override string ValidateInternally()
        {
            /*var result = new ConcurrentBag<StringTag>();
            Parallel.ForEach(Files,  CoreSettings.ParallelOptions, file =>
            {
                using (Mat mat = CvInvoke.Imread(file.TagString, ImreadModes.AnyColor))
                {
                    if (mat.Size != FileResolution)
                    {
                        result.Add(file);
                    }
                }
            });

            if (result.IsEmpty) return null;
            var message = new StringBuilder();
            message.AppendLine($"The following {result.Count} files mismatched the target resolution of {FileResolution.Width}x{FileResolution.Height}:");
            message.AppendLine();
            uint count = 0;
            foreach (var file in result)
            {
                count++;
                if (count == 20)
                {
                    message.AppendLine("... Too many to show ...");
                    break;
                }
                message.AppendLine(file.Content);
            }
            

            return new StringTag(message.ToString(), result);*/

            StringBuilder sb = new();

            if (_files.Count == 0)
            {
                sb.AppendLine("No files to import.");
            }

            return sb.ToString();
        }
        #endregion

        #region Properties

        public ImportTypes ImportType
        {
            get => _importType;
            set
            {
                if(!RaiseAndSetIfChanged(ref _importType, value)) return;
                RaisePropertyChanged(nameof(IsImportStackType));
                RaisePropertyChanged(nameof(IsExtendBeyondLayerCountVisible));
            }
        }

        
        public static Array ImportTypesItems => Enum.GetValues(typeof(ImportTypes));

        public bool IsImportStackType => _importType == ImportTypes.Stack;

        public uint StartLayerIndex
        {
            get => _startLayerIndex;
            set => RaiseAndSetIfChanged(ref _startLayerIndex, value);
        }

        public bool ExtendBeyondLayerCount
        {
            get => _extendBeyondLayerCount;
            set => RaiseAndSetIfChanged(ref _extendBeyondLayerCount, value);
        }

        public bool IsExtendBeyondLayerCountVisible => _importType is ImportTypes.Replace or ImportTypes.Stack or ImportTypes.MergeSum or ImportTypes.MergeMax;

        public bool DiscardUnmodifiedLayers
        {
            get => _discardUnmodifiedLayers;
            set => RaiseAndSetIfChanged(ref _discardUnmodifiedLayers, value);
        }

        public ushort StackMargin
        {
            get => _stackMargin;
            set => RaiseAndSetIfChanged(ref _stackMargin, value);
        }

        [XmlIgnore]
        public RangeObservableCollection<ValueDescription> Files
        {
            get => _files;
            set => RaiseAndSetIfChanged(ref _files, value);
        }

        public uint Count => (uint)_files.Count;
        #endregion

        #region Constructor

        public OperationLayerImport() { }

        public OperationLayerImport(FileFormat slicerFile) : base(slicerFile) { }


        #endregion

        #region Methods

        public void AddFile(string file)
        {
            _files.Add(new ValueDescription(file, Path.GetFileNameWithoutExtension(file)));
        }

        public void Sort()
        {
            _files.Sort((file1, file2) => string.Compare(Path.GetFileNameWithoutExtension(file1.ValueAsString), Path.GetFileNameWithoutExtension(file2.ValueAsString), StringComparison.Ordinal));
        }
        

        /*public uint CalculateTotalLayers(uint totalLayers)
        {
            if (DiscardRemainingLayers)
            {
                return (uint) (1 + InsertAfterLayerIndex + Files.Count - (ReplaceStartLayer ? 1 : 0));
            }
            if (ReplaceSubsequentLayers)
            {
                uint result = (uint) (1 + InsertAfterLayerIndex + Files.Count - (ReplaceStartLayer ? 1 : 0));
                return result <= totalLayers ? totalLayers : result;
            }

            return (uint)(totalLayers + Files.Count - (ReplaceStartLayer ? 1 : 0));
        }*/

        public override string ToString()
        {
            var result = $"[Files: {Count}]" + LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            progress.ItemCount = 0;
            var result = SlicerFile.SuppressRebuildPropertiesWork(() => { 
                List<FileFormat> fileFormats = new();
                List<KeyValuePair<uint, string>> keyImage = new();
                int lastProcessedLayerIndex = -1;

                // Order raw images
                for (int i = 0; i < Count; i++)
                {
                    if (!_files[i].ValueAsString.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
                        !_files[i].ValueAsString.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) &&
                        !_files[i].ValueAsString.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) &&
                        !_files[i].ValueAsString.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) &&
                        !_files[i].ValueAsString.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)) continue;
                    keyImage.Add(new KeyValuePair<uint, string>((uint)keyImage.Count, _files[i].ValueAsString));
                }

                // Create virtual file format with images
                if (keyImage.Count > 0)
                {
                    progress.Reset("Packing images", (uint)keyImage.Count);
                    SL1File format = new();
                    format.LayerManager.Init((uint)keyImage.Count);

                    Parallel.ForEach(keyImage, CoreSettings.ParallelOptions, pair =>
                    {
                        if (progress.Token.IsCancellationRequested) return;
                        using var mat = CvInvoke.Imread(pair.Value, ImreadModes.Grayscale);
                        if (pair.Key == 0) format.Resolution = mat.Size;
                        format[pair.Key] = new Layer(pair.Key, mat, format);

                        progress.LockAndIncrement();
                    });

                    progress.Token.ThrowIfCancellationRequested();
                    fileFormats.Add(format);
                }

                // Order remaining possible file formats
                for (int i = 0; i < Count; i++)
                {
                    if (_files[i].ValueAsString.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        _files[i].ValueAsString.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
                        _files[i].ValueAsString.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                        _files[i].ValueAsString.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        _files[i].ValueAsString.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)) continue;

                    var fileFormat = FileFormat.FindByExtensionOrFilePath(_files[i].ValueAsString, true);
                    if (fileFormat is null) continue;
                    fileFormat.FileFullPath = _files[i].ValueAsString;
                    fileFormats.Add(fileFormat);
                }

                progress.Token.ThrowIfCancellationRequested();

                if (fileFormats.Count == 0) return false;

                if (_importType == ImportTypes.Stack)
                {
                    new OperationMove(SlicerFile, Enumerations.Anchor.TopLeft).Execute(progress);
                }

                foreach (var fileFormat in fileFormats)
                {
                    if (!string.IsNullOrEmpty(fileFormat.FileFullPath))
                    {
                        fileFormat.Decode(fileFormat.FileFullPath, progress);
                    }

                    var boundingRectangle = SlicerFile.LayerManager.GetBoundingRectangle(progress);
                    var fileFormatBoundingRectangle = fileFormat.LayerManager.GetBoundingRectangle(progress);
                    var roiRectangle = Rectangle.Empty;

                    // Check if is possible to process this file
                    switch (_importType)
                    {
                        case ImportTypes.Insert:
                            if (SlicerFile.Resolution != fileFormat.Resolution &&
                                (SlicerFile.Resolution.Width < fileFormatBoundingRectangle.Width ||
                                 SlicerFile.Resolution.Height < fileFormatBoundingRectangle.Height)) continue;
                            SlicerFile.LayerManager.ReallocateInsert(_startLayerIndex, fileFormat.LayerCount);
                            break;
                        case ImportTypes.Replace:
                        case ImportTypes.Stack:
                            if (SlicerFile.Resolution != fileFormat.Resolution &&
                                (SlicerFile.Resolution.Width < fileFormatBoundingRectangle.Width ||
                                 SlicerFile.Resolution.Height < fileFormatBoundingRectangle.Height)) continue;


                            //if(fileFormatBoundingRectangle.Width >= SlicerFile.ResolutionX || fileFormatBoundingRectangle.Height >= SlicerFile.ResolutionY) 
                            //    continue;

                            if (_importType == ImportTypes.Stack)
                            {
                                int x = 0;
                                int y = 0;

                                if (boundingRectangle.Right + _stackMargin + fileFormatBoundingRectangle.Width <
                                    SlicerFile.ResolutionX)
                                {
                                    x = boundingRectangle.Right + _stackMargin;
                                }
                                else
                                {
                                    y = boundingRectangle.Bottom + _stackMargin;
                                }

                                if (x + fileFormatBoundingRectangle.Width >= SlicerFile.ResolutionX)
                                    continue;
                                if (y + fileFormatBoundingRectangle.Height >= SlicerFile.ResolutionY)
                                    continue;

                                roiRectangle = new Rectangle(x, y, fileFormatBoundingRectangle.Width,
                                    fileFormatBoundingRectangle.Height);
                            }

                            if (_extendBeyondLayerCount)
                            {
                                int layerCountDifference = (int)(_startLayerIndex + fileFormat.LayerCount - SlicerFile.LayerCount);
                                if (layerCountDifference > 0)
                                {
                                    SlicerFile.LayerManager.ReallocateEnd((uint)layerCountDifference, _importType == ImportTypes.Stack);
                                }
                            }

                            break;
                        case ImportTypes.MergeSum:
                        case ImportTypes.MergeMax:
                            if (SlicerFile.Resolution != fileFormat.Resolution) continue;
                            if (_extendBeyondLayerCount)
                            {
                                int layerCountDifference = (int)(_startLayerIndex + fileFormat.LayerCount - SlicerFile.LayerCount);
                                if (layerCountDifference > 0)
                                {
                                    SlicerFile.LayerManager.ReallocateEnd((uint)layerCountDifference, true);
                                }
                            }
                            break;
                        case ImportTypes.Subtract:
                        case ImportTypes.AbsDiff:
                        case ImportTypes.BitwiseAnd:
                        case ImportTypes.BitwiseOr:
                        case ImportTypes.BitwiseXOr:
                            if (SlicerFile.Resolution != fileFormat.Resolution) continue;
                            break;
                    }

                    progress.Reset(ProgressAction, fileFormat.LayerCount);
                    Parallel.For(0, fileFormat.LayerCount, CoreSettings.ParallelOptions, i =>
                        {
                            if (progress.Token.IsCancellationRequested) return;
                            uint layerIndex = (uint)(_startLayerIndex + i);

                            switch (_importType)
                            {
                                case ImportTypes.Insert:
                                    {
                                        if (layerIndex >= SlicerFile.LayerCount) return;
                                        if (SlicerFile.Resolution == fileFormat.Resolution)
                                        {
                                            SlicerFile[layerIndex] = fileFormat[i];
                                            break;
                                        }

                                        using var layer = fileFormat[i].LayerMat;
                                        using var layerRoi = layer.NewMatFromCenterRoi(SlicerFile.Resolution, fileFormatBoundingRectangle);
                                        SlicerFile[layerIndex] = new Layer(layerIndex, layerRoi, SlicerFile);

                                        break;
                                    }
                                case ImportTypes.Replace:
                                    {
                                        if (layerIndex >= SlicerFile.LayerCount) return;
                                        if (SlicerFile.Resolution == fileFormat.Resolution)
                                        {
                                            SlicerFile[layerIndex] = fileFormat[i];
                                            break;
                                        }

                                        using var layer = fileFormat[i].LayerMat;
                                        using var layerRoi = layer.NewMatFromCenterRoi(SlicerFile.Resolution, fileFormatBoundingRectangle);
                                        SlicerFile[layerIndex] = new Layer(layerIndex, layerRoi, SlicerFile);
                                        break;
                                    }
                                case ImportTypes.Stack:
                                    {
                                        if (layerIndex >= SlicerFile.LayerCount) return;
                                        using var mat = SlicerFile[layerIndex].LayerMat;
                                        using var importMat = fileFormat[i].LayerMat;
                                        var matRoi = new Mat(mat, roiRectangle);
                                        var importMatRoi = new Mat(importMat, fileFormatBoundingRectangle);
                                        importMatRoi.CopyTo(matRoi);
                                        SlicerFile[layerIndex].LayerMat = mat;

                                        break;
                                    }
                                case ImportTypes.MergeSum:
                                    {
                                        if (layerIndex >= SlicerFile.LayerCount) return;
                                        using var originalMat = SlicerFile[layerIndex].LayerMat;
                                        using var newMat = fileFormat[i].LayerMat;
                                        CvInvoke.Add(originalMat, newMat, newMat);
                                        SlicerFile[layerIndex].LayerMat = newMat;
                                        break;
                                    }
                                case ImportTypes.MergeMax:
                                    {
                                        if (layerIndex >= SlicerFile.LayerCount) return;
                                        using var originalMat = SlicerFile[layerIndex].LayerMat;
                                        using var newMat = fileFormat[i].LayerMat;
                                        CvInvoke.Max(originalMat, newMat, newMat);
                                        SlicerFile[layerIndex].LayerMat = newMat;
                                        break;
                                    }
                                case ImportTypes.Subtract:
                                    {
                                        if (layerIndex >= SlicerFile.LayerCount) return;
                                        using var originalMat = SlicerFile[layerIndex].LayerMat;
                                        using var newMat = fileFormat[i].LayerMat;
                                        CvInvoke.Subtract(originalMat, newMat, newMat);
                                        SlicerFile[layerIndex].LayerMat = newMat;
                                        break;
                                    }
                                case ImportTypes.AbsDiff:
                                {
                                    if (layerIndex >= SlicerFile.LayerCount) return;
                                    using var originalMat = SlicerFile[layerIndex].LayerMat;
                                    using var newMat = fileFormat[i].LayerMat;
                                    CvInvoke.AbsDiff(originalMat, newMat, newMat);
                                    SlicerFile[layerIndex].LayerMat = newMat;
                                    break;
                                }
                                case ImportTypes.BitwiseAnd:
                                    {
                                        if (layerIndex >= SlicerFile.LayerCount) return;
                                        using var originalMat = SlicerFile[layerIndex].LayerMat;
                                        using var newMat = fileFormat[i].LayerMat;
                                        CvInvoke.BitwiseAnd(originalMat, newMat, newMat);
                                        SlicerFile[layerIndex].LayerMat = newMat;
                                        break;
                                    }
                                case ImportTypes.BitwiseOr:
                                    {
                                        if (layerIndex >= SlicerFile.LayerCount) return;
                                        using var originalMat = SlicerFile[layerIndex].LayerMat;
                                        using var newMat = fileFormat[i].LayerMat;
                                        CvInvoke.BitwiseOr(originalMat, newMat, newMat);
                                        SlicerFile[layerIndex].LayerMat = newMat;
                                        break;
                                    }
                                case ImportTypes.BitwiseXOr:
                                    {
                                        if (layerIndex >= SlicerFile.LayerCount) return;
                                        using var originalMat = SlicerFile[layerIndex].LayerMat;
                                        using var newMat = fileFormat[i].LayerMat;
                                        CvInvoke.BitwiseXor(originalMat, newMat, newMat);
                                        SlicerFile[layerIndex].LayerMat = newMat;
                                        break;
                                    }
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }


                            lock (progress.Mutex)
                            {
                                lastProcessedLayerIndex = Math.Max(lastProcessedLayerIndex, (int)layerIndex);
                                progress++;
                            }
                        });

                    fileFormat.Dispose();
                    progress.Token.ThrowIfCancellationRequested();
                }

                if (_importType == ImportTypes.Stack)
                {
                    new OperationMove(SlicerFile).Execute(progress);
                }

                if (lastProcessedLayerIndex <= -1) return false;

                if (lastProcessedLayerIndex + 1 < SlicerFile.LayerCount && _discardUnmodifiedLayers)
                {
                    SlicerFile.LayerManager.Reallocate((uint)lastProcessedLayerIndex + 1);
                }

                return true;
            }, true);
            
            return !progress.Token.IsCancellationRequested && result;
        }

        #endregion
    }
}
