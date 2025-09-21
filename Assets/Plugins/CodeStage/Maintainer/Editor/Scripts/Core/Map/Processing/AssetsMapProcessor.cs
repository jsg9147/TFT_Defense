#region copyright
// -------------------------------------------------------
// Copyright (C) Dmitry Yuhanov [https://codestage.net]
// -------------------------------------------------------
#endregion

namespace CodeStage.Maintainer.Core.Map.Processing
{
    using System;
    using UnityEngine;
    using Progress;

    internal class AssetsMapProcessor
    {
        private readonly IProgressReporter progressReporter;
        private readonly AssetUpdateProcessor updateProcessor;
        private readonly ReferenceProcessor referenceProcessor;

        public AssetsMapProcessor(IProgressReporter progressReporter)
        {
            this.progressReporter = progressReporter;
            updateProcessor = new AssetUpdateProcessor(progressReporter);
            referenceProcessor = new ReferenceProcessor(progressReporter);
        }

        public bool ProcessMap(AssetsMap map)
        {
            try
            {
                if (!ProcessExistingAssets(map))
                    return false;

                if (!ProcessNewAssets(map))
                    return false;

                if (!ProcessReferences(map))
                    return false;

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
            finally
            {
                progressReporter.ClearProgress();
            }
        }

        private bool ProcessExistingAssets(AssetsMap map)
        {
            return updateProcessor.ProcessExistingAssets(map);
        }

        private bool ProcessNewAssets(AssetsMap map)
        {
            return updateProcessor.ProcessNewAssets(map);
        }

        private bool ProcessReferences(AssetsMap map)
        {
            return referenceProcessor.ProcessReferences(map);
        }
    }
} 