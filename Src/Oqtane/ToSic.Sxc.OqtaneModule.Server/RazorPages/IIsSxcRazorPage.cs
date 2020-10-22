﻿using ToSic.Sxc.Blocks;
using ToSic.Sxc.Code;

namespace ToSic.Sxc.OqtaneModule.Server.RazorPages
{
    public interface IIsSxcRazorPage
    {
        DynamicCodeRoot DynCode { get; set; }

        string VirtualPath { get; set; }

        Purpose Purpose { get; set; }

    }
}
