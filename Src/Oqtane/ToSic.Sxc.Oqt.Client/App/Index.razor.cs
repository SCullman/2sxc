﻿using Oqtane.Models;
using Oqtane.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Oqtane.Shared;
using ToSic.Sxc.Oqt.Shared.Run;

// ReSharper disable once CheckNamespace
namespace ToSic.Sxc.Oqt.App
{
    public partial class Index
    {
        [Inject]
        public ISxcOqtane SxcEngine { get; set; }

        public override List<Resource> Resources => new List<Resource>();

        protected override async Task OnInitializedAsync()
        {
            // prepare the html / headers
            SxcEngine.Prepare(PageState.Site, PageState.Page, ModuleState);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await base.OnAfterRenderAsync(firstRender);

                if (PageState.Runtime == Runtime.Server)
                {
                    var interop = new Interop(JSRuntime);

                    // HACK: Lets load all 2sxc js dependencies (js / styles)
                    foreach (var resource in SxcEngine.AssetsAndHeaders.Scripts())
                        await interop.IncludeScript("", resource, "", "", "", "head", "");

                    await interop.IncludeScripts(SxcEngine.Resources
                        .Where(r => r.ResourceType == ResourceType.Script)
                        .Select(a => new { href = a.Url })
                        .Cast<object>()
                        .ToArray());

                    foreach (var style in SxcEngine.AssetsAndHeaders.Styles())
                        await interop.IncludeLink("", "stylesheet", style, "text/css", "", "", ""); //.IncludeScript("", resource.Url, resource.Integrity ?? "", resource.CrossOrigin ?? "", "", "head", "");

                    await interop.IncludeLinks(SxcEngine.Resources
                        .Where(r => r.ResourceType == ResourceType.Stylesheet)
                        .Select(a => new { rel = "stylesheet", href = a.Url, type = "text/css" })
                        .Cast<object>()
                        .ToArray());

                    var aAndH = SxcEngine.AssetsAndHeaders;
                    if (aAndH.AddContextMeta)
                        await interop.IncludeMeta("sxc-tmp-context-id", "name", aAndH.ContextMetaName, aAndH.ContextMetaContents(), "id");
                }
            }
        }
    }
}
