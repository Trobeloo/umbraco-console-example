using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Our.Umbraco.Community.PowerShellModule
{
    class UmbracoInstanceContainer
    {
        internal static UmbracoInstance instance = null;
        private static AppDomain umbracoDomain;

        internal static UmbracoInstance Instance
        {
            get
            {
                return instance;
            }
        }

        public static void Initialize(string psPath)
        {
            if (instance == null)
            {
                instance = new UmbracoInstance(
                    AppDomain.CurrentDomain.BaseDirectory,
                    psPath
                );
                CreateAndRunDomain(psPath);
            }
        }

        private static void CreateAndRunDomain(string psPath)
        {
            umbracoDomain = AppDomain.CreateDomain(
                "Umbraco",
                new Evidence(),
                new AppDomainSetup
                {
                    ApplicationBase = Environment.CurrentDirectory, // psPath,
                    PrivateBinPath = Path.Combine(psPath),
                    //PrivateBinPathProbe = "NonNullToOnlyUsePrivateBin",
                    ConfigurationFile = Path.Combine(psPath, "web.config")
                }
            );
            umbracoDomain.SetData(".appPath", psPath);
            //var assembly = File.ReadAllBytes(Path.Combine(toolPath, "UmbConsole.exe"));
            //umbracoDomain.Load(assembly);

            //umbracoDomain.AssemblyLoad += (sender, eventArgs) => { return; };

            //var thisFile = File.ReadAllBytes(typeof(UmbracoInstanceContainer).Assembly.Location);
            //umbracoDomain.Load(thisFile);

            // WTF? Fusion log shows path of powershell. :/
            umbracoDomain.DoCallBack(instance.Start);
        }


    }

    [Cmdlet("Create", "UmbracoInstance")]
    public class CreateUmbracoCmdlet : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            UmbracoInstanceContainer.Initialize(SessionState.Path.CurrentLocation.Path);
            WriteObject(UmbracoInstanceContainer.Instance);
        }
    }
}
