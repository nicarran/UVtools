﻿/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Text;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationLayerClone : Operation
    {
        #region Members
        private uint _clones = 1;
        private bool _keepSamePositionZ;

        #endregion

        #region Overrides

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.Current;
        public override bool CanROI => false;
        public override bool PassActualLayerIndex => true;

        public override string Title => "Clone layers";
        public override string Description =>
            "Clone layers.\n\n" +
            "Useful to increase the height of the model or add additional structure by duplicating layers. For example, can be used to increase the raft height for added stability.";
        public override string ConfirmationText =>
            $"clone layers {LayerIndexStart} through {LayerIndexEnd}, {Clones} time{(Clones != 1 ? "s" : "")}?";

        public override string ProgressTitle =>
            $"Cloning layers {LayerIndexStart} through {LayerIndexEnd}, {Clones} time{(Clones != 1 ? "s" : "")}";

        public override string ProgressAction => "Cloned layers";

        public override bool CanCancel => false;

        public override bool CanHaveProfiles => false;

        public override string ValidateInternally()
        {
            var sb = new StringBuilder();
            if (Clones <= 0)
            {
                sb.AppendLine("Clones must be a positive number");
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            var result = $"[Clones: {Clones}]" + LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets if cloned layers will keep same position z or get the height rebuilt
        /// </summary>
        public bool KeepSamePositionZ
        {
            get => _keepSamePositionZ;
            set => RaiseAndSetIfChanged(ref _keepSamePositionZ, value);
        }

        /// <summary>
        /// Gets or sets the number of clones
        /// </summary>
        public uint Clones
        {
            get => _clones;
            set
            {
                if(!RaiseAndSetIfChanged(ref _clones, value)) return;
                RaisePropertyChanged(nameof(ExtraLayers));
            }
        }

        public uint ExtraLayers => (uint)Math.Max(0, ((int)LayerIndexEnd - LayerIndexStart + 1) * _clones);

        #endregion

        #region Constructor

        public OperationLayerClone() { }

        public OperationLayerClone(FileFormat slicerFile) : base(slicerFile) { }

        #endregion

        #region Methods

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            uint totalClones = (LayerIndexEnd - LayerIndexStart + 1) * Clones;
            progress.Reset(ProgressAction, totalClones);

            var newLayers = new Layer[SlicerFile.LayerCount + totalClones];
            uint newLayerIndex = 0;
            for (uint layerIndex = 0; layerIndex < SlicerFile.LayerCount; layerIndex++)
            {
                newLayers[newLayerIndex++] = SlicerFile[layerIndex];
                if (layerIndex < LayerIndexStart || layerIndex > LayerIndexEnd) continue;
                for (uint i = 0; i < Clones; i++)
                {
                    newLayers[newLayerIndex++] = SlicerFile[layerIndex].Clone();
                    progress++;
                }
            }

            SlicerFile.SuppressRebuildPropertiesWork(() =>
            {
                SlicerFile.LayerManager.Layers = newLayers;
            }, !_keepSamePositionZ);
            

            /*var oldLayers = SlicerFile.LayerManager.Layers;

            uint totalClones = (LayerIndexEnd - LayerIndexStart + 1) * Clones;
            uint newLayerCount = (uint) (oldLayers.Length + totalClones);
            var layers = new Layer[newLayerCount];


            uint newLayerIndex = 0;
            for (uint layerIndex = 0; layerIndex < oldLayers.Length; layerIndex++)
            {
                layers[newLayerIndex] = oldLayers[layerIndex];
                if (layerIndex >= LayerIndexStart && layerIndex <= LayerIndexEnd)
                {
                    for (uint i = 0; i < Clones; i++)
                    {
                        newLayerIndex++;
                        layers[newLayerIndex] = oldLayers[layerIndex].Clone();
                        layers[newLayerIndex].IsModified = true;

                        progress++;
                    }
                }

                newLayerIndex++;
            }

            SlicerFile.LayerManager.Layers = layers;*/

            return !progress.Token.IsCancellationRequested;
        }

        #endregion
    }
}
