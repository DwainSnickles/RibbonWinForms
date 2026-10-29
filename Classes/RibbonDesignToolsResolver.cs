using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace RibbonLib
{
    /// <summary>
    /// Lets this assembly use the design-tools version Visual Studio hosts instead of the exact
    /// version it was compiled against.
    /// </summary>
    /// <remarks>
    /// RibbonLib is compiled against Microsoft.DotNet.DesignTools.Server as shipped by the
    /// Microsoft.WinForms.Designer.SDK package (assembly version 1.6.0.0). The design server that
    /// Visual Studio runs is built independently and carries a different assembly version
    /// (18.12.0.0 in the release that reported the failure). A strong-named reference to an exact
    /// version is not unified automatically on .NET, so resolving any design-time type threw
    /// <c>FileNotFoundException: Could not load file or assembly
    /// 'Microsoft.DotNet.DesignTools.Server, Version=1.6.0.0'</c> inside the design server and
    /// killed the designer.
    ///
    /// This hook answers any request for a design-tools assembly with the copy that is already
    /// loaded, whatever its version. Outside Visual Studio nothing is loaded, the hook returns
    /// null, and the loader behaves exactly as it did before - no dependency on design-tools is
    /// introduced at runtime.
    /// </remarks>
    internal static class RibbonDesignToolsResolver
    {
        private const string DesignToolsAssemblyPrefix = "Microsoft.DotNet.DesignTools";

        private static bool _registered;

        [ModuleInitializer]
        internal static void Register()
        {
            if (_registered)
                return;

            _registered = true;

            try
            {
                AssemblyLoadContext? ownContext =
                    AssemblyLoadContext.GetLoadContext(typeof(RibbonDesignToolsResolver).Assembly);

                if (ownContext != null)
                    ownContext.Resolving += OnResolving;

                AssemblyLoadContext.Default.Resolving += OnResolving;
            }
            catch (Exception)
            {
                // Loading must never fail because of the compatibility hook.
            }
        }

        private static Assembly? OnResolving(AssemblyLoadContext context, AssemblyName name)
        {
            string? requested = name.Name;

            if (requested == null ||
                !requested.StartsWith(DesignToolsAssemblyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Prefer the loaded copy in this context, then any other: they are all the same
            // design server, only the version differs from what was compiled against.
            Assembly? loaded = FindLoaded(context, requested);

            if (loaded != null)
                return loaded;

            foreach (AssemblyLoadContext other in AssemblyLoadContext.All)
            {
                loaded = FindLoaded(other, requested);

                if (loaded != null)
                    return loaded;
            }

            return null;
        }

        private static Assembly? FindLoaded(AssemblyLoadContext context, string simpleName)
        {
            foreach (Assembly assembly in context.Assemblies)
            {
                if (string.Equals(assembly.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase))
                    return assembly;
            }

            return null;
        }
    }
}
