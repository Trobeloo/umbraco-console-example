using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Security.Policy;
using Umbraco.Core;
using Umbraco.Core.Persistence;

namespace Our.Umbraco.Community.PowerShellModule
{
    /// <summary>
    /// Before running this console app please ensure that the "umbracoDbDSN" ConnectionString is pointing to your database.
    /// If you are using Sql Ce please replace "|DataDirectory|" with a real path or alternatively place 
    /// your database in the debug folder before running the application in debug mode.
    /// </summary>
    public class UmbracoInstance : MarshalByRefObject
    {
        private string toolPath = AppDomain.CurrentDomain.BaseDirectory;
        private Dictionary<string, Assembly> environmentAssemblies = null;
        private ConsoleApplicationBase application;
        private AppDomain umbracoDomain;
        private ApplicationContext applicationContext;

        public ApplicationContext Context => applicationContext;

        public ConsoleApplicationBase Application => application;

        private string currentAssemblyPath;

        public UmbracoInstance(string currentAssemblyPath)
        {
            this.currentAssemblyPath = currentAssemblyPath;
        }

        public void Start()
        {
            RunUmbraco();
        }

        private void RunUmbraco()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

            Console.Title = "Umbraco Console";

            Assembly.Load("Examine");
            Assembly.Load("Lucene.Net");

            try
            {
                applicationContext = InitializeApplication();
            }
            catch
            {
                throw;
            }
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                Assembly assembly = System.Reflection.Assembly.Load(args.Name);
                if (assembly != null)
                    return assembly;
            }
            catch
            {
                // ignore load error }

                // *** Try to load by filename - split out the filename of the full assembly name
                // *** and append the base path of the original assembly (ie. look in the same dir)
                // *** NOTE: this doesn't account for special search paths but then that never
                //           worked before either.
                string[] Parts = args.Name.Split(',');
                string File = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + Parts[0].Trim() +
                              ".dll";

                return System.Reflection.Assembly.LoadFrom(File);
            }

            return null;
        }

        private Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                var assemblyName = args.Name;

                var probeFolder = Path.Combine(Environment.CurrentDirectory, "bin");

                var currentDomain = AppDomain.CurrentDomain;
                if (environmentAssemblies == null)
                {
                    var allLoadedAssemblies = currentDomain.GetAssemblies().Where(x => !x.IsDynamic);
                    var loadedAssemblyFileNames = allLoadedAssemblies
                        .Select(x => {
                            try
                            {
                                return x.Location.Substring(x.Location.LastIndexOf(@"\") + 1);
                            }
                            catch
                            {
                                throw;
                            }
                        })
                        .ToArray();

                    var allEnvironmentAssemblyFiles = Directory.GetFiles(probeFolder)
                        .Where(x => x.EndsWith(".dll") || x.EndsWith(".exe"))
                        .ToArray();

                    var notLoadedEnvironmentAssemblies = allEnvironmentAssemblyFiles
                        .Where(x => !loadedAssemblyFileNames.Any(x.EndsWith))
                        .ToArray();

                    environmentAssemblies = notLoadedEnvironmentAssemblies
                        .Select(Assembly.LoadFrom)
                        .ToDictionary(x => x.GetName().FullName, x => x);
                }

                var isMatchedFullName = environmentAssemblies.ContainsKey(assemblyName);
                if (!isMatchedFullName)
                {
                    var fromSimpleName =
                        environmentAssemblies.Keys.FirstOrDefault(x => x.StartsWith(assemblyName + ","));
                    if (!String.IsNullOrEmpty(fromSimpleName))
                    {
                        assemblyName = fromSimpleName;
                        isMatchedFullName = true;
                    }
                }

                if (isMatchedFullName)
                {
                    return environmentAssemblies[assemblyName];
                }

                return null;
            }
            catch
            {
                throw;
            }
        }

        private ApplicationContext InitializeApplication()
        {
            application = new ConsoleApplicationBase();
            application.Start(application, new EventArgs());
            Console.WriteLine("Application Started");

            var context = ApplicationContext.Current;
            var databaseContext = context.DatabaseContext;
            var database = databaseContext.Database;

            Console.WriteLine("--------------------");
            //Write status for ApplicationContext
            Console.WriteLine("ApplicationContext is available: " + (context != null).ToString());
            //Write status for DatabaseContext
            Console.WriteLine("DatabaseContext is available: " + (databaseContext != null).ToString());
            //Write status for Database object
            Console.WriteLine("Database is available: " + (database != null).ToString());
            Console.WriteLine("--------------------");
            return context;
        }
    }
}
