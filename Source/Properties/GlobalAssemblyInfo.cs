using System.Reflection;

[assembly: AssemblyProduct("Ketchup Add-on")]
[assembly: AssemblyCompany("Dwayne Bent")]
[assembly: AssemblyCopyright("Copyright © 2013")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyVersion("0.5.0.0")]
