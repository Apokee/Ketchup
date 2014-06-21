using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyProduct("KSG Ketchup Mod")]
[assembly: AssemblyCompany("Dwayne Bent")]
[assembly: AssemblyCopyright("Copyright © 2013-2014")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyInformationalVersion("0.6.0 Alpha")]

[assembly: ComVisible(false)]

[assembly: InternalsVisibleTo("Ksg.Ketchup.Tests")]
