using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

#if DEBUG

[assembly: AssemblyConfiguration("Debug")]
[assembly: AssemblyDescription("QTranslate, based on STranslate (https://github.com/STranslate/STranslate)")]
#else
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyDescription("QTranslate, based on STranslate (https://github.com/STranslate/STranslate)")]
#endif

[assembly: AssemblyCompany("QTranslate")]
[assembly: AssemblyProduct("QTranslate")]
[assembly: AssemblyCopyright("The MIT License (MIT)")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.3.0")]
[assembly: AssemblyFileVersion("1.0.3.0")]
[assembly: AssemblyInformationalVersion("1.0.3.0")]
[assembly: InternalsVisibleTo("STranslate.Tests")]
