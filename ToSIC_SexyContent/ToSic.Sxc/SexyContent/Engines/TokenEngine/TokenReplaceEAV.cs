﻿// using DotNetNuke.Entities.Portals;

using ToSic.Eav.ValueProviders;

namespace ToSic.SexyContent.Engines.TokenEngine
{
    internal class TokenReplaceEav: Eav.Tokens.TokenReplace
    {
        public int ModuleId;
        //public PortalSettings PortalSettings;

        public TokenReplaceEav(App app, int instanceId, /*PortalSettings portalSettings,*/ IValueCollectionProvider provider)
        {
            InitAppAndPortalSettings(app, instanceId, /*portalSettings,*/ provider);
        }

        public void InitAppAndPortalSettings(App app, int moduleId, /*PortalSettings portalSettings,*/ IValueCollectionProvider provider)
        {
            foreach (var valueProvider in provider.Sources)
                ValueSources.Add(valueProvider.Key, valueProvider.Value);

            ModuleId = moduleId;
            //PortalSettings = portalSettings;  
        }

    }
}