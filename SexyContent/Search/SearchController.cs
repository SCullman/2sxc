﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.Search.Entities;
using ToSic.Eav;
using ToSic.Eav.DataSources;
using ToSic.SexyContent.DataSources;
using ToSic.SexyContent.Engines;

namespace ToSic.SexyContent.Search
{
    public class SearchController
    {

        public IList<SearchDocument> GetModifiedSearchDocuments(ModuleInfo moduleInfo, DateTime beginDate)
        {
            var searchDocuments = new List<SearchDocument>();

            var isContentModule = moduleInfo.DesktopModule.ModuleName == "2sxc";

            // New Context because PortalSettings.Current is null
            var zoneId = SexyContent.GetZoneID(moduleInfo.PortalID);

            if (!zoneId.HasValue)
                return searchDocuments;

            var appId = SexyContent.GetDefaultAppId(zoneId.Value);

            if (!isContentModule)
            {
                // Get AppId from ModuleSettings
                var appIdString = moduleInfo.ModuleSettings[SexyContent.AppIDString];
                if (appIdString == null || !int.TryParse(appIdString.ToString(), out appId))
                    return searchDocuments;
            }

            var sexy = new SexyContent(zoneId.Value, appId, true);

            // This list will hold all EAV entities to be indexed
            var dataSource = sexy.GetViewDataSource(moduleInfo.ModuleID, false);
            var moduleDataSource = (ModuleDataSource)((IDataTarget)dataSource).In["Default"].Source;

            var elements = moduleDataSource.ContentElements.ToList();
            elements.Add(moduleDataSource.ListElement);

            if (!elements.Any() || !elements.Any(e => e.TemplateId.HasValue))
                return searchDocuments;

            var template = sexy.TemplateContext.GetTemplate(elements.First().TemplateId.Value);
            var engine = EngineFactory.CreateEngine(template);
            engine.Init(template, sexy.App, moduleInfo, dataSource);

            try
            {
                engine.CustomizeData();
            }
            catch (Exception e) // Catch errors here, because of references to Request etc.
            {
                Exceptions.LogException(e);
            }

            var searchInfos = new List<SearchInfo>();
            searchInfos.Add(new SearchInfo()
            {
                AdditionalBody = "",
                EntityLists = dataSource.Out.ToDictionary(p => p.Key, p => p.Value.List.Select(e => e.Value).ToList()),
            });

            engine.PrepareSearchData(searchInfos);

            // Get DNN SearchDocuments from 2Sexy SearchInfos
            foreach (var s in searchInfos)
            {
                var entities = new List<IEntity>();

                foreach (var entityList in s.EntityLists)
                    entities.AddRange(entityList.Value);

                string body = entities.Aggregate("", (current, entity) => current + GetJoinedAttributes(entity, sexy.GetCurrentLanguageName()));

                searchDocuments.Add(new SearchDocument()
                {
                    Url = s.Url,
                    // ToDo: UniqueKey!
                    UniqueKey = moduleInfo.ModuleID.ToString(),
                    PortalId = moduleInfo.PortalID,
                    // ToDo: Title!
                    Title = moduleInfo.ModuleTitle,
                    // ToDo: Description!
                    Description = "",
                    Body = body,
                    // ToDo: ModifiedTime!
                    ModifiedTimeUtc = DateTime.Now.ToUniversalTime()
                });
            }

            return searchDocuments;
        }

        private string StripHtmlAndHtmlDecode(string Text)
        {
            return HttpUtility.HtmlDecode(Regex.Replace(Text, "<.*?>", string.Empty));
        }

        private string GetJoinedAttributes(IEntity entity, string language)
        {
            return String.Join(", ",
                entity.Attributes.Select(x => x.Value[new[] {language}])
                    .Where(a => a != null)
                    .Select(a => StripHtmlAndHtmlDecode(a.ToString()))
                    .Where(x => !String.IsNullOrEmpty(x))) + " ";
        }
    }
}