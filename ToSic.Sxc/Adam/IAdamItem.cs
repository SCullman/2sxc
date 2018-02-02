﻿namespace ToSic.SexyContent.Adam
{
    internal interface IAdamItem
    {
        #region Metadata
        bool HasMetadata { get; }
        DynamicEntity Metadata { get; }
        #endregion


        string Url { get; }

        string Type { get; }

        string Name { get; }
    };
}

