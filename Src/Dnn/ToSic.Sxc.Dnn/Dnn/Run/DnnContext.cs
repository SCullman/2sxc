﻿using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Entities.Users;
using ToSic.Eav.Run;
using ToSic.Sxc.Context;
using ToSic.Sxc.Run.Context;

namespace ToSic.Sxc.Dnn.Run
{
    public class DnnContextOld : IDnnContext
    {
        /// <summary>
        /// Build DNN Helper
        /// Note that the context can be null, in which case it will have no module context, and default to the current portal
        /// </summary>
        /// <param name="moduleContext"></param>
        public DnnContextOld(IModuleInternal moduleContext)
        {
            Module = (moduleContext as ModuleInternal<ModuleInfo>)?.UnwrappedContents;
            // note: this may be a bug, I assume it should be Module.OwnerPortalId
            Portal = PortalSettings.Current ?? 
                (moduleContext != null ? new PortalSettings(Module.PortalID): null);
        }

        public ModuleInfo Module { get; }

        public TabInfo Tab => Portal?.ActiveTab;

        public PortalSettings Portal { get; }

        public UserInfo User => Portal.UserInfo;
    }
}