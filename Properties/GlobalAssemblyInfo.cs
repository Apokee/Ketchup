using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyProduct("Ketchup Mod")]
[assembly: AssemblyCompany("Kerbal Systems Group")]
[assembly: AssemblyCopyright("Copyright © 2013-2014")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyInformationalVersion("0.6.0 Alpha")]

[assembly: ComVisible(false)]

[assembly: InternalsVisibleTo("Ketchup.Tests")]
