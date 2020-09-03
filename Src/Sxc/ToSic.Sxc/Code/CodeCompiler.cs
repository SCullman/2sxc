﻿using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using ToSic.Eav.Logging;
using ToSic.Sxc.Web;
#if NET451
using System.Web.Compilation;
#else
using System.Security.Policy;
using HtmlString = Microsoft.AspNetCore.Html.HtmlString;
#endif


namespace ToSic.Sxc.Code
{
    internal class CodeCompiler: HasLog
    {
        internal CodeCompiler(ILog parentLog) : base("Sys.CsCmpl", parentLog)
        {
        }

        internal const string CsFileExtension = ".cs";
        internal const string CsHtmlFileExtension = ".cshtml";
        internal const string SharedCodeRootPathKeyInCache = "SharedCodeRootPath";
        
        internal object InstantiateClass(string virtualPath, string className = null, string relativePath = null, bool throwOnError = true)
        {
            var wrapLog = Log.Call($"{virtualPath}, {className}, {throwOnError}");
            string errorMsg = null;

            // Perform various checks on the path values
            var hasErrorMessage = CheckIfPathsOkAndCleanUp(ref virtualPath, relativePath);
            if (hasErrorMessage != null)
            {
                Log.Add($"Error: {hasErrorMessage}");
                wrapLog("failed");
                if(throwOnError) throw new Exception(hasErrorMessage);
                return null;
            }

            var pathLowerCase = virtualPath.ToLowerInvariant();
            var isCs = pathLowerCase.EndsWith(CsFileExtension);
            var isCshtml = pathLowerCase.EndsWith(CsHtmlFileExtension);

            Type compiledType = null;
            if (isCshtml && string.IsNullOrEmpty(className))
            {
#if NETSTANDARD
            throw new Exception("Not Yet Implemented in .net standard #TodoNetStandard");
#else
                compiledType = BuildManager.GetCompiledType(virtualPath);
#endif
                if (compiledType == null)
                    errorMsg = $"Couldn't create instance of {virtualPath}. Compiled type == null";
            }
            // compile .cs files
            else if (isCs || isCshtml)
            {
                // if no name provided, use the name which is the same as the file name
                className = className ?? Path.GetFileNameWithoutExtension(virtualPath) ?? "unknown";

                Assembly assembly;
#if NETSTANDARD
                throw new Exception("Not Yet Implemented in .net standard #TodoNetStandard");
#else
                assembly = BuildManager.GetCompiledAssembly(virtualPath);
#endif
                compiledType = assembly.GetType(className, throwOnError, true);

                if (compiledType == null) 
                    errorMsg = $"didn't find type '{className}' in {virtualPath}";
            }
            else
                errorMsg = $"Error: given path '{virtualPath}' doesn't point to a .cs or .cshtml";

            if (errorMsg != null)
            {
                Log.Add(errorMsg + $"; throw error: {throwOnError}");
                wrapLog("failed");
                if (throwOnError) throw new Exception(errorMsg);
                return null;
            }

            var instance = RuntimeHelpers.GetObjectValue(Activator.CreateInstance(compiledType));
            AttachRelativePath(virtualPath, instance);

            wrapLog($"found: {instance != null}");
            return instance;

        }

        /// <summary>
        /// Check the path and perform various corrections
        /// </summary>
        /// <param name="virtualPath">primary path to use</param>
        /// <param name="relativePath">optional second path to which the primary one would be attached to</param>
        /// <returns>null if all is ok, or an error message if not</returns>
        private string CheckIfPathsOkAndCleanUp(ref string virtualPath, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(virtualPath))
                return "no path/name provided";

            // if path relative, merge with shared code path
            virtualPath = virtualPath.Replace("\\", "/");
            if (!virtualPath.StartsWith("/"))
            {
                Log.Add($"Trying to resolve relative path: '{virtualPath}' using '{relativePath}'");
                if (relativePath == null)
                    return "Unexpected null value on relativePath";

                // if necessary, add trailing slash
                if (!relativePath.EndsWith("/"))
                    relativePath += "/";
                virtualPath = Eav.Factory.Resolve<IHttp>().Combine(relativePath, virtualPath);
                Log.Add($"final virtual path: '{virtualPath}'");
            }

            if (virtualPath.IndexOf(":", StringComparison.InvariantCultureIgnoreCase) > -1)
                return $"Tried to get .cs file, but found '{virtualPath}' containing ':', (not allowed)";

            return null;
        }


        private static void AttachRelativePath(string virtualPath, object instance)
        {
            // in case it supports shared code again, give it the relative path
            if (instance is ICreateInstance codeForwarding)
                codeForwarding.CreateInstancePath = Path.GetDirectoryName(virtualPath);
        }
    }
}