---
uid: Articles.ChangeLog.BreakingChanges
---

# Breaking Changes in EAV and 2sxc

We try to minimize breaking changes, and most breaking changes won't affect your work, because it's internal API. 
We're documenting it here to ensure you know what happened, in case you still run into this.

## Version 10

Version 10 has a lot of small breaking changes because we restructured the internal API so it's consistent when we publish it. 
All these things shouldn't affect you, because they were internal APIs, but in case it does - here's what we did.

#### Version 10.10

1. the internal interface `IInPageEditingHelpers` was moved from `ToSic.SexyContent.Interfaces` to the namespace `ToSic.Sxc`
1. the internal interface `IHtmlHelper` and `ILinkHelper` was moved to `ToSic.Sxc.Dnn`
1. the interface `ToSic.Sxc.Adam.IFolder` was moved to `ToSic.Eav.Apps.Assets.IAdamFolder`
1. the internal namespace `ToSic.Eav.ValueProvider` was changed to `ToSic.Eav.ValueProviders` (added an 's' for consistency)
1. the property `Configuration` on dynamic entities was deprecated in 2sxc 4 and removed in 2sxc 10 - we don't think it was ever used
1. moved internal Metadata interfaces (ca. 5) into final namespace @ToSic.Eav.Metadata
1. Moved a bunch of internal interfaces which we believe were never used externally from `ToSic.Eav.Interfaces` to `ToSic.Eav.Data`
	1. `ToSic.Eav.Data.IAttribute`
	1. `ToSic.Eav.IAttribute<T>`
	1. `IAttributeBase`
	1. `IAttributeDefinition`
	1. `IChildEntities` 
	1. `IContentType`
	1. `IDimension`
	1. `IEntityLight`
	1. `ILanguage`
	1. `IRelationshipManager`
	1. `IValue`
	1. `IValue<T>`
	1. `IValueOfDimension<T>`

##### Deprecated/Changed, but not broken

1. the interface `ToSic.Sxc.Adam.IFile` was moved to `ToSic.Eav.Apps.Assets.IAdamFile` but the old interface still exists so it shouln't break  
	_it was used by Mobius Forms_
1. the internal interface `ToSic.SexyContent.IAppAndDataHelpers` was renamed to `ToSic.Sxc.IDynamicCode` but the old interface still exists, so it shouldn't break  
	_it was used by Mobius Forms_
1. moved `ToSic.Eav.Interfaces.IEntity` to `ToSic.Eav.Data.IEntity` - but preserved the old interface for compatibility
	_it was used everywhere_