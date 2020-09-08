﻿using System;
using Microsoft.Extensions.Primitives;
using ToSic.Eav.Apps.Run;
using ToSic.Sxc.Blocks;
using ToSic.Sxc.Mvc.Dev;
using ToSic.Sxc.Mvc.Run;

namespace ToSic.Sxc.Mvc.WebApi
{
    public abstract class SxcStatefullControllerBase: SxcStatelessControllerBase
    {

        protected IInstanceContext GetContext()
        {
            // in case the initial request didn't yet find a block builder, we need to create it now
            var context = // BlockBuilder?.Context ??
                new InstanceContext(new MvcTenant(HttpContext), new PageNull(), new ContainerNull(), new MvcUser());
            return context;
        }

        protected IBlock GetBlock(bool allowNoContextFound = true)
        {
            var wrapLog = Log.Call<IBlock>(parameters: $"request:..., {nameof(allowNoContextFound)}: {allowNoContextFound}");

            var containerId = GetTypedHeader(Sxc.WebApi.WebApiConstants.HeaderInstanceId, -1);
            var contentblockId = GetTypedHeader(Sxc.WebApi.WebApiConstants.HeaderContentBlockId, 0); // this can be negative, so use 0
            var pageId = GetTypedHeader(Sxc.WebApi.WebApiConstants.HeaderPageId, -1);
            var instance = TestIds.FindInstance(containerId);

            if (containerId == -1 || pageId == -1 || instance == null)
            {
                if (allowNoContextFound) return wrapLog("not found", null);
                throw new Exception("No context found, cannot continue");
            }

            var ctx = SxcMvc.CreateContext(HttpContext, instance.Zone, pageId, containerId, instance.App,
                instance.Block);
            IBlock block = new BlockFromModule().Init(ctx, Log);

            // only if it's negative, do we load the inner block
            if (contentblockId > 0) return wrapLog("found", block);

            Log.Add($"Inner Content: {contentblockId}");
            block = new BlockFromEntity().Init(block, contentblockId, Log);

            return wrapLog("found", block);
        }

        private T GetTypedHeader<T>(string headerName, T fallback)
        {
            var valueString = Request.Headers[headerName];
            if (valueString == StringValues.Empty) return fallback;

            try
            {
                return (T) Convert.ChangeType(valueString.ToString(), typeof(T));
            }
            catch
            {
                return fallback;
            }

        }
    }
}
