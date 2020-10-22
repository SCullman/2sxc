﻿using System.Collections.Generic;
using ToSic.Eav.Apps.Run;
using ToSic.Eav.WebApi.Dto;
using ToSic.Sxc.Blocks;
using ToSic.Sxc.OqtaneModule.Shared.Dev;
using ToSic.Sxc.Web;
using ToSic.Sxc.WebApi.Context;

namespace ToSic.Sxc.OqtaneModule.Server.Controllers
{
    public class OqtaneContextBuilder: ContextBuilderBase
    {
        public OqtaneContextBuilder(IHttp http)
        {
            _http = http;
        }

        internal OqtaneContextBuilder Init(IBlock block)
        {
            _context = block.Context;
            InitApp(block.ZoneId, block.App);
            return this;
        }

        private readonly IHttp _http;
        private IInstanceContext _context;


        protected override LanguageDto GetLanguage()
        {
            return new LanguageDto
            {
                Current = WipConstants.DefaultLanguage,
                Primary = WipConstants.DefaultLanguage,
                All = new Dictionary<string, string>
                {
                    {WipConstants.DefaultLanguage, WipConstants.DefaultLanguageText}
                }
            };
        }

        protected override WebResourceDto GetSystem() =>
            new WebResourceDto
            {
                Url = _http.ToAbsolute("~/")
            };

        protected override WebResourceDto GetSite() =>
            new WebResourceDto
            {
                Id = _context.Tenant.Id,
                Url = "//" + _context.Tenant.Url,
            };

        protected override WebResourceDto GetPage() =>
            new WebResourceDto
            {
                Id = _context.Page.Id,
            };

        protected override EnableDto GetEnable()
        {
            return new EnableDto
            {
                AppPermissions = true,
                CodeEditor = true,
                Query = true
            };
        }

        protected override string GetGettingStartedUrl() => "#todo-not-yet-implemented-getting-started";
    }
}
