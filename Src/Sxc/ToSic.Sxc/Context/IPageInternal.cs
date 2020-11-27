﻿using System.Collections.Generic;
using ToSic.Eav.Apps.Run;
using ToSic.Eav.Documentation;

namespace ToSic.Sxc.Context
{
    // ReSharper disable once PossibleInterfaceMemberAmbiguity
    public interface IPageInternal : IWebResource
    {
        /// <summary>
        /// These parameters can reconfigure what view is used or change
        /// </summary>
        [PrivateApi("wip")] List<KeyValuePair<string, string>> Parameters { get; set; }

        IPageInternal Init(int id);

    }
}
