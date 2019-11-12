﻿using System;
using System.Collections.Generic;
using DotNetNuke.Entities.Modules;
using ToSic.Eav.Apps;
using ToSic.Eav.Documentation;
using ToSic.SexyContent.Search;
using ToSic.Sxc.Blocks;
using ToSic.Sxc.Engines;
using ToSic.Sxc.Search;

// ReSharper disable UnusedMemberInSuper.Global

// ReSharper disable once CheckNamespace
namespace ToSic.Sxc.Dnn
{
    /// <summary>
    /// All DNN Razor Pages inherit from this class
    /// </summary>
    [PublicApi]
    public interface IRazor: IDynamicCode
    {
        /// <summary>
        /// Helper for Html.Raw - for creating raw html output which doesn't encode &gt; and &lt;
        /// </summary>
        IHtmlHelper Html { get; }


        /// <summary>
        /// Override this to have your code change the (already initialized) Data object. 
        /// If you don't override this, nothing will be changed/customized. 
        /// </summary>
        void CustomizeData();

        /// <summary>
        /// Customize how the search will process data on this page. 
        /// </summary>
        /// <param name="searchInfos"></param>
        /// <param name="moduleInfo"></param>
        /// <param name="beginDate"></param>
        void CustomizeSearch(Dictionary<string, List<ISearchItem>> searchInfos, IInstanceInfo moduleInfo,
            DateTime beginDate);

        Purpose Purpose { get; }

    }
}
