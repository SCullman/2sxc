﻿using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oqtane.Repository;
using ToSic.Eav.Run;
using ToSic.Sxc.WebApi.Cms;

// #Todo - change the official route of this - probably /cms/link/xyz

namespace ToSic.Sxc.OqtaneModule.Server.Controllers
{
    [ApiController]
    [Route(WebApiConstants.WebApiRoot + "/dnn/[controller]/[action]")]
    public class HyperlinkController: SxcStatefulControllerBase
    {
        protected override string HistoryLogName => WebApiConstants.MvcApiLogPrefix + "HypLnk";

        public HyperlinkController(SxcOqtane sxcOqtane, IZoneMapper zoneMapper, ITenantResolver tenantResolver) : base(sxcOqtane, zoneMapper, tenantResolver) { }


        [HttpGet]
        [AllowAnonymous]   // will check security internally, so assume no requirements
        public string ResolveHyperlink(string hyperlink, int appId, string contentType = default, Guid guid = default, string field = default)
            => new HyperlinkBackend<int, int>().Init(Log).ResolveHyperlink(GetBlock(), hyperlink, appId, contentType, guid, field);

    }
}
