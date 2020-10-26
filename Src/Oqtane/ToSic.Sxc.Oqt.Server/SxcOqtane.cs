﻿using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Oqtane.Models;
using ToSic.Eav.Apps.Run;
using ToSic.Eav.Logging;
using ToSic.Sxc.Blocks;
using ToSic.Sxc.Oqt.Server.Page;
using ToSic.Sxc.Oqt.Server.Run;
using ToSic.Sxc.Oqt.Server.Wip;
using ToSic.Sxc.Oqt.Shared.Dev;
using ToSic.Sxc.Oqt.Shared.Run;
using ToSic.Sxc.Razor.Debug;

namespace ToSic.Sxc.Oqt.Server
{
    public class SxcOqtane: HasLog, IRenderTestIds
    {
        #region Constructor and DI
        
        public SxcOqtane(IHttpContextAccessor httpContextAccessor, OqtAssetsAndHeaders assetsAndHeaders, RazorReferenceManager debugRefMan) : base("Mvc.View")
        {
            _assetsAndHeaders = assetsAndHeaders;
            _debugRefMan = debugRefMan;
            _httpContext = httpContextAccessor.HttpContext;
            // add log to history!
            History.Add("oqt-view", Log);
        }

        private readonly HttpContext _httpContext;
        public IOqtAssetsAndHeader AssetsAndHeaders => _assetsAndHeaders;
        private readonly OqtAssetsAndHeaders _assetsAndHeaders;
        private readonly RazorReferenceManager _debugRefMan;

        #endregion

        #region Prepare

        /// <summary>
        /// Prepare must always be the first thing to be called - to ensure that afterwards both headers and html are known. 
        /// </summary>
        public void Prepare(Site site, Oqtane.Models.Page page, Module module)
        {
            if (_renderDone) throw new Exception("already prepared this module");

            _site = site;
            _page = page;
            _module = module;

            var idSet = LookupTestIdSet();
            if (idSet == null)
            {
                GeneratedHtml = (MarkupString)$"Error - module id {_module.ModuleId} not found";
                return;
            }

            _block = GetBlock(idSet);
            _assetsAndHeaders.Init(_block.BlockBuilder);
            GeneratedHtml = (MarkupString) RenderString();
            _renderDone = true;
        }

        private Site _site;
        private Oqtane.Models.Page _page;
        private Module _module;
        private IBlock _block;

        private bool _renderDone;
        public MarkupString GeneratedHtml { get; private set; }

        #endregion

        private InstanceId LookupTestIdSet()
        {
            var mid = _module.ModuleId;
            InstanceId ids = null;
            switch (mid)
            {
                case 27:
                    ids = TestIds.Token;
                    break;
                case 28: // test site 2dm, root site
                case 1049: // test sub-site 2dm "blogsite"
                case 56: // test sub-site Tonci "blogsite"
                    ids = TestIds.Blog;
                    break;
            }

            return ids;
        }

        public string Test()
        {
            return _debugRefMan.CompilationReferences.Count.ToString();
        }

        private string RenderString()
        {

            var result = _block.BlockBuilder.Render();

            _assetsAndHeaders.Init(_block.BlockBuilder);

            // todo: set parameters for loading scripts etc.
            //PageProperties.Headers += "hello!!!";

            return result;
        }

        private IBlock GetBlock(InstanceId id) => CreateBlock(id.Zone, id.Page, id.Container, id.App, id.Block, Log);

        public IBlock CreateBlock(int zoneId, int pageId, int containerId, int appId, Guid blockGuid, ILog log)
        {
            var context = CreateContext(_httpContext, zoneId, pageId, containerId, appId, blockGuid);
            var block = new BlockFromModule().Init(context, log);
            return block;
        }

        public InstanceContext CreateContext(HttpContext http, int zoneId, int pageId, int containerId, int appId,
            Guid blockGuid)
            => new InstanceContext(
                new WipTenant(http).Init(zoneId), 
                //_zoneMapper.TenantOfZone(zoneId),
                new OqtanePage(pageId, null),
                new OqtaneContainer(tenantId: zoneId, id: containerId, appId: appId, block: blockGuid),
                new OqtaneUser(WipConstants.NullUser)
            );
    }
}