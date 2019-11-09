﻿using System;
using System.Collections.Generic;
using ToSic.Eav;
using ToSic.Eav.Apps;
using ToSic.Eav.Logging;
using ToSic.SexyContent;
using ToSic.SexyContent.DataSources;
using ToSic.Sxc.DnnWebForms.Helpers;
using ToSic.Sxc.Interfaces;
using App = ToSic.SexyContent.App;

namespace ToSic.Sxc.Blocks
{
    internal sealed class BlockFromModule: BlockBase
    {
        public IInstanceInfo InstanceInfo;

        public override BlockConfigBase Manager => new BlockConfigForModule(CmsInstance);

        public override bool ParentIsEntity => false;


        public override IBlockDataSource Data => _dataSource 
            ?? (_dataSource = BlockDataSource.ForContentGroupInSxc(CmsInstance, View, App?.ConfigurationProvider, Log, InstanceInfo.Id));

        private readonly IEnumerable<KeyValuePair<string, string>> _urlParams;

        /// <summary>
        /// Create a module-content block
        /// </summary>
        /// <param name="instanceInfo">the dnn module-info</param>
        /// <param name="parentLog">a parent-log; can be null but where possible you should wire one up</param>
        /// <param name="tenant"></param>
        /// <param name="overrideParams">optional override parameters</param>
        public BlockFromModule(IInstanceInfo instanceInfo, ILog parentLog, ITenant tenant, IEnumerable<KeyValuePair<string, string>> overrideParams = null): base(parentLog, "CB.Mod")
        {
            InstanceInfo = instanceInfo ?? throw new Exception("Need valid Instance/ModuleInfo / ModuleConfiguration of runtime");
            ParentId = instanceInfo.Id;
            ContentBlockId = ParentId;

            // url-params
            _urlParams = overrideParams ?? SystemWeb.GetUrlParams();

            // Ensure we know what portal the stuff is coming from
            // PortalSettings is null, when in search mode
            Tenant = tenant;

            // important: don't use the SxcInstance.Environment, as it would try to init the Sxc-object before the app is known, causing various side-effects
            var tempEnv = Factory.Resolve<IEnvironmentFactory>().Environment(parentLog);
            ZoneId = tempEnv.ZoneMapper.GetZoneId(tenant.Id); // use tenant as reference, as it can be different from instance.TennantId
            
            AppId = Factory.Resolve<IMapAppToInstance>().GetAppIdFromInstance(instanceInfo, ZoneId) ?? 0;// fallback/undefined YET

            Log.Add($"parent#{ParentId}, content-block#{ContentBlockId}, z#{ZoneId}, a#{AppId}");

            if (AppId == Settings.DataIsMissingInDb)
            {
                _dataIsMissing = true;
                Log.Add("data is missing, will stop here");
                return;
            }

            // 2018-09-22 new with auto-init-data
            CmsInstance = new CmsInstance(this, InstanceInfo, _urlParams, Log);

            if (AppId != 0)
            {
                Log.Add("real app, will load data");

                // 2018-09-22 new with auto-init-data
                App = new App(Tenant, ZoneId, AppId, ConfigurationProvider.Build(CmsInstance, false), true, Log);

                Configuration = App.BlocksManager.GetInstanceContentGroup(instanceInfo.Id, instanceInfo.PageId);

                if (Configuration.DataIsMissing)
                {
                    _dataIsMissing = true;
                    App = null;
                    return;
                }

                ((CmsInstance)CmsInstance).SetTemplateOrOverrideFromUrl(Configuration.View);                
            }
        }


        //public override SxcInstance SxcInstance
        //    => _sxcInstance ?? (_sxcInstance = new SxcInstance(this, InstanceInfo, _urlParams, Log));

        public override bool IsContentApp => InstanceInfo.IsPrimary;

    }
}