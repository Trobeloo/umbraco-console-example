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
        private string psPath;

        public UmbracoInstance(string currentAssemblyPath, string psPath)
        {
            this.currentAssemblyPath = currentAssemblyPath;
            this.psPath = psPath;
        }

        public void Start()
        {
            RunUmbraco();
        }

        private void RunUmbraco()
        {
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

                Console.Title = "Umbraco Console";

                Assembly.Load("Examine");
                Assembly.Load("Lucene.Net");

                applicationContext = InitializeApplication();
            }
            catch
            {
                throw;
            }
        }

        private Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                var assemblyName = args.Name;

                var probeFolders = new[] { Path.Combine(psPath, "bin"), currentAssemblyPath };

                if (environmentAssemblies == null)
                {
                    var allEnvironmentAssemblyFiles = probeFolders
                        .SelectMany(Directory.GetFiles)
                        .DistinctBy(x => x.Substring(x.LastIndexOf(@"\") + 1))
                        .Where(x => x.EndsWith(".dll") || x.EndsWith(".exe"))
                        .ToArray();

                    environmentAssemblies = allEnvironmentAssemblyFiles
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
            Environment.CurrentDirectory = psPath;

            application = new ConsoleApplicationBase(psPath);
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
