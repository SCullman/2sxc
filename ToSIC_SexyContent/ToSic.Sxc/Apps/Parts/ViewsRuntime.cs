﻿using System;
using System.Collections.Generic;
using System.Linq;
using ToSic.Eav;
using ToSic.Eav.Apps;
using ToSic.Eav.Apps.Ui;
using ToSic.Eav.DataSources;
using ToSic.Eav.Logging;
using ToSic.Eav.Serializers;
using ToSic.SexyContent;
using ToSic.SexyContent.Internal;
using ToSic.Sxc.Apps.Blocks;
using ToSic.Sxc.Blocks;

// note: not sure if the final namespace should be Sxc.Apps or Sxc.Views
namespace ToSic.Sxc.Apps
{
	public class ViewsRuntime: HasLog
	{
		public readonly int ZoneId;
		public readonly int AppId;

		public ViewsRuntime(int zoneId, int appId, ILog parentLog): base("App.TmplMg", parentLog)
		{
			ZoneId = zoneId;
			AppId = appId;
		}

	    private IDataSource _templateDs;
		private IDataSource TemplateDataSource()
		{
            if(_templateDs!= null)return _templateDs;
		    // ReSharper disable once RedundantArgumentDefaultValue
			var dataSource = DataSource.GetInitialDataSource(ZoneId, AppId, false);
			dataSource = DataSource.GetDataSource<EntityTypeFilter>(ZoneId, AppId, dataSource);
		    ((EntityTypeFilter) dataSource).TypeName = Configuration.TemplateContentType;
		    _templateDs = dataSource;
			return dataSource;
		}

		public IEnumerable<IView> GetAllTemplates() 
            => TemplateDataSource().List.Select(p => new View(p)).OrderBy(p => p.Name);

		public IView GetTemplate(int templateId)
		{
			var dataSource = TemplateDataSource();
			dataSource = DataSource.GetDataSource<EntityIdFilter>(ZoneId, AppId, dataSource);
			((EntityIdFilter)dataSource).EntityIds = templateId.ToString();
			var templateEntity = dataSource.List.FirstOrDefault();

			if(templateEntity == null)
				throw new Exception("The template with id " + templateId + " does not exist.");

			return new View(templateEntity);
		}

        public bool DeleteTemplate(int templateId)
		{
            // really get template first, to be sure it is a template
			var template = GetTemplate(templateId);
            return new AppManager(ZoneId, AppId).Entities.Delete(template.Id);
		}
        

        internal IEnumerable<TemplateUiInfo> GetCompatibleTemplates(IApp app, BlockConfiguration blockConfiguration)
	    {
	        IEnumerable<IView> availableTemplates;
	        var items = blockConfiguration.Content;

            // if any items were already initialized...
	        if (items.Any(e => e != null))
	            availableTemplates = GetFullyCompatibleTemplates(blockConfiguration);

            // if it's only nulls, and only one (no list yet)
	        else if (items.Count <= 1)
	            availableTemplates = GetAllTemplates(); 

            // if it's a list of nulls, only allow lists
	        else
	            availableTemplates = GetAllTemplates().Where(p => p.UseForList);

	        return availableTemplates.Select(t => new TemplateUiInfo
	        {
	            TemplateId = t.Id,
	            Name = t.Name,
	            ContentTypeStaticName = t.ContentType,
	            IsHidden = t.IsHidden,
	            Thumbnail = TemplateHelpers.GetTemplateThumbnail(app, t.Location, t.Path)
	        });
	    }


        /// <summary>
        /// Get templates which match the signature of possible content-items, presentation etc. of the current template
        /// </summary>
        /// <param name="blockConfiguration"></param>
        /// <returns></returns>
	    private IEnumerable<IView> GetFullyCompatibleTemplates(BlockConfiguration blockConfiguration)
        {
            var isList = blockConfiguration.Content.Count > 1;

            var compatibleTemplates = GetAllTemplates().Where(t => t.UseForList || !isList);
            compatibleTemplates = compatibleTemplates
                .Where(t => blockConfiguration.Content.All(c => c == null) || blockConfiguration.Content.First(e => e != null).Type.StaticName == t.ContentType)
                .Where(t => blockConfiguration.Presentation.All(c => c == null) || blockConfiguration.Presentation.First(e => e != null).Type.StaticName == t.PresentationType)
                .Where(t => blockConfiguration.ListContent.All(c => c == null) || blockConfiguration.ListContent.First(e => e != null).Type.StaticName == t.HeaderType)
                .Where(t => blockConfiguration.ListPresentation.All(c => c == null) || blockConfiguration.ListPresentation.First(e => e != null).Type.StaticName == t.HeaderPresentationType);

            return compatibleTemplates;
        }

        // todo: check if this call could be replaced with the normal ContentTypeController.Get to prevent redundant code
        public IEnumerable<ContentTypeUiInfo> GetContentTypesWithStatus()
        {
            var templates = GetAllTemplates().ToList();
            var visible = templates.Where(t => !t.IsHidden).ToList();
            var serializer = new Serializer();

            return new AppRuntime(ZoneId, AppId, Log).ContentTypes.FromScope(Settings.AttributeSetScope) 
                .Where(ct => templates.Any(t => t.ContentType == ct.StaticName)) // must exist in at least 1 template
                .OrderBy(ct => ct.Name)
                .Select(ct =>
                {
                    var metadata = ct.Metadata.Description;
                    return new ContentTypeUiInfo {
                        StaticName = ct.StaticName,
                        Name = ct.Name,
                        IsHidden = visible.All(t => t.ContentType != ct.StaticName),   // must check if *any* template is visible, otherise tell the UI that it's hidden
                        Thumbnail = metadata?.GetBestValue(View.TemplateIcon, true)?.ToString(),
                        Metadata = serializer.Prepare(metadata)
                    };
                });
        }



    }
}