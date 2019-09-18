using System;
using System.IO;
using System.Management.Automation;
using System.Security.Policy;
using Lucene.Net.Messages;

namespace Our.Umbraco.Community.PowerShellModule
{
    [Cmdlet("Get", "Greeting")]
    public class GetGreeting : PSCmdlet
    {
        public static int greets = 0;

        [Parameter(Mandatory = false, Position = 0, ValueFromPipeline = true)] public string Greetee { get; set; } = "Stranger";

        private AppDomain umbracoDomain;

        protected override void ProcessRecord()
        {
            umbracoDomain = AppDomain.CreateDomain(
                "Umbraco",
                new Evidence(),
                new AppDomainSetup
                {
                    ApplicationBase = Environment.CurrentDirectory,
                    PrivateBinPath = Path.Combine(Environment.CurrentDirectory),
                }
            );

            var thisFile = File.ReadAllBytes(typeof(UmbracoInstanceContainer).Assembly.Location);
            umbracoDomain.Load(thisFile);

            var creater = new MessageCreater(Greetee, SessionState.Path.CurrentLocation.Path);

            umbracoDomain.DoCallBack(creater.CreateMessage);

            WriteObject(creater.MessageData);
        }
    }

    public class MessageCreater : MarshalByRefObject
    {
        private readonly string psPath;
        public string Greetee { get; }

        public MessageCreater(string greetee, string psPath)
        {
            this.psPath = psPath;
            Greetee = greetee;
        }

        public MessageData MessageData { get; private set; }

        public void CreateMessage()
        {
            MessageData = new MessageData
            {
                Message = $"Hi there, {Greetee} !\r\n" +
                          $"Environment: {Environment.CurrentDirectory}\r\n" +
                          $"PS: {psPath}"
            };
        }

    }

    public class MessageData
    {
        public string Message { get; set; }
    }
}
