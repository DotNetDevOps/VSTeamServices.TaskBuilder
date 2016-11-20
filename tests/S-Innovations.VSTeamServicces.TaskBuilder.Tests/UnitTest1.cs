using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SInnovations.VSTeamServices.TaskBuilder.ConsoleUtils;

namespace TaskInstallerTests
{
    public class Opt
    {
        [Option("property")]
        public string P { get; set; }
    }
   // https://www.visualstudio.com/en-us/integrate/get-started/client-libraries/samples
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task TestMethod0()
        {
            var var = "aa";
            var args = new string[] { }.LoadFrom<Opt>(null, o => o.P ?? var);
        }
        [TestMethod]
        public async Task TestMethod1()
        {
            //var m = Microsoft.ServiceBus.Messaging.QueueClient.CreateFromConnectionString(
            //    "Endpoint=sb://myfdsfsgsa.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=flypeCG42bfjl+P2YLXjc19sYWNs0k9cXXgv4Fhj6bs=", "cibuilds", Microsoft.ServiceBus.Messaging.ReceiveMode.PeekLock);

            //var a = m.Receive();

            //var b = a.ContentType;
            //var c = a.GetBody<string>();
            //var d = new Microsoft.TeamFoundation.Build.WebApi.Events.BuildCompletedEvent(new Microsoft.TeamFoundation.Build.WebApi.Build());
            //var build = JObject.Parse(c).SelectToken("resource").ToObject<Build>();
            //// Microsoft.TeamFoundation.Common.
            //// var client = new BuildHttpClient(build.Uri,new VssBasicToken(new )
            ////
            //var collectionUrl = "https://sinnovations.visualstudio.com/DefaultCollection/9c9f030e-f8a1-4512-9c8b-05303cdd2b00";// build.Project.Url;
            //                                                                                                                   // VssConnection connection = GetPersonCOnnection(collectionUrl);
            //                                                                                                                   //    VssConnection connection = GetPersonCOnnection(collectionUrl);
            //                                                                                                                   //   var client = new BuildHttpClient(new Uri(collectionUrl), new VssBasicCredential(string.Empty, "cexly75rijnu33w2efdvjvqrwxe6sdrx23rkdwt565t3wzqihoja"));

            ////     var client = connection.GetClient<BuildHttpClient>();

            //var client = new HttpClient();
            //client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(":cexly75rijnu33w2efdvjvqrwxe6sdrx23rkdwt565t3wzqihoja")));

            //var art = JObject.Parse(await client.GetStringAsync(build.Url + "/artifacts"));
            //Console.WriteLine(art.ToString(Formatting.Indented));
        }

        //private static VssConnection GetADDConnection(string collectionUrl)
        //{
        //    return new VssConnection(new Uri(collectionUrl), new VssAadCredential());
        //}

        //private static VssConnection GetPersonCOnnection(string collectionUrl)
        //{
        //    return new VssConnection(new Uri(collectionUrl), new VssBasicCredential(string.Empty, "cexly75rijnu33w2efdvjvqrwxe6sdrx23rkdwt565t3wzqihoja"));
        //}
    }
}
