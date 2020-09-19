using Microsoft.Data.Mashup;
using Microsoft.Data.Mashup.Preview;
using Microsoft.Mashup.OAuth;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PowerUT
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Authentication authentication = new Authentication();

            //Load additional data connctors like Kusto
            MashupLibraryProvider mp = MashupLibraryProvider.Assembly(new AssemblyName("PowerBIExtensions"));
            MashupLibraryProvider.SetProviders(mp);

            string code = File.ReadAllText(args[0]);
            PQDocument pqDoc = new PQDocument(code);

            PrintQueries(pqDoc);
            Console.Write("Select Query:");
            string queryName = Console.ReadLine();

            var queryParameters = pqDoc.Parameters;
            List<NamedExpression> parameterReplacements = new List<NamedExpression>();
            foreach (var queryParameter in queryParameters)
            {
                object defaultQueryValue = queryParameter.DefaultValue;
                Console.Write($"{queryParameter.Name} ({queryParameter.DefaultValue ?? "null"}):");
                string newQueryParameter = Console.ReadLine();
                if (newQueryParameter != "")
                {
                    parameterReplacements.Add(new NamedExpression(queryParameter.Name, PQLiteral.FromInput(queryParameter.Type, newQueryParameter).Code));
                }
            }
            pqDoc = pqDoc.UpdateExpressions(parameterReplacements);
            //Engine
            MashupConnectionStringBuilder connectionString = new MashupConnectionStringBuilder()
            {
                //Choose either Mashup or Package
                Mashup = pqDoc.Code, //Full document, like when a pq is extracted from a pbit

                //Package = MashupPackage.ToBase64String(queryName, "#shared"), //Single expression, as seen in Advanced view

                //Query to evaluate
                Location = queryName,
            };
            MashupConnection connection = new MashupConnection(connectionString.ToString());
            QueryExecutionStatus queryStatus = QueryExecutionStatus.NotStarted;
            Action authUpdateTask = null;
            object authUpdateLock = new object();
            DataSourceSetting ds = null;
            connection.DataSourceSettingNeeded += (missingAuthSender, e) =>
            {
                authUpdateTask = () =>
                {
                    Guid activityId = Guid.NewGuid();
                    Console.WriteLine(e.Details);
                    foreach (var dataSource in e.Details.DataSources)
                    {
                        DataSourceSetting dataSourceSetting;
                        if (dataSource.TryFindBestMatch(e.NewSettings.Keys, out var matchingDataSource))
                        {
                            dataSourceSetting = e.NewSettings[matchingDataSource];
                        }
                        else
                        {
                            bool success;
                            queryStatus = QueryExecutionStatus.AwaitingUserAuthentication;
                            (success, dataSourceSetting) = authentication.UserAAD(dataSource);
                            if (!success)
                            {
                                queryStatus = QueryExecutionStatus.Failed;
                                break;
                            }
                        }
                        e.NewSettings[dataSource] = dataSourceSetting;
                        ds = dataSourceSetting;
                    }
                    queryStatus = QueryExecutionStatus.RetryPending;
                };
                lock (authUpdateLock)
                {
                    queryStatus = QueryExecutionStatus.AuthenticationNeeded;
                    Monitor.PulseAll(authUpdateLock);
                    while (queryStatus == QueryExecutionStatus.AuthenticationNeeded)
                    {
                        Monitor.Wait(authUpdateLock);
                    }
                }
            };

            connection.Open();
            MashupCommand cmd = connection.CreateCommand();
            cmd.MashupCommandTextDialect = MashupCommandTextDialect.M;
            cmd.CommandText = connectionString.Location;
            cmd.CommandType = System.Data.CommandType.TableDirect;
            new Thread(() =>
            {
                while (queryStatus != QueryExecutionStatus.Complete)
                {
                    try
                    {
                        lock (authUpdateLock)
                        {
                            queryStatus = QueryExecutionStatus.Running;
                            Monitor.PulseAll(authUpdateLock);
                        }
                        using (var reader = cmd.ExecuteReader(CommandBehavior.Default, MashupCommandBehavior.Default))
                        {
                            lock (authUpdateLock)
                            {
                                queryStatus = QueryExecutionStatus.Serializing;
                                Monitor.PulseAll(authUpdateLock);
                            }
                            object[] rowValues = new object[reader.VisibleFieldCount];
                            while (reader.Read())
                            {
                                reader.GetValues(rowValues);
                                Console.WriteLine(string.Join(", ", rowValues));
                            }
                        }
                        lock (authUpdateLock)
                        {
                            queryStatus = QueryExecutionStatus.Complete;
                            Monitor.PulseAll(authUpdateLock);
                        }
                    }
                    catch (MashupCredentialException)
                    {
                        queryStatus = QueryExecutionStatus.NotStarted;
                    }
                    catch (MashupValueException)
                    {
                        queryStatus = QueryExecutionStatus.NotStarted;
                    }
                }
            }).Start();
            lock (authUpdateLock)
            {
                while (queryStatus != QueryExecutionStatus.Complete && queryStatus != QueryExecutionStatus.Failed && queryStatus != QueryExecutionStatus.RetryReady)
                {
                    if (queryStatus == QueryExecutionStatus.AuthenticationNeeded)
                    {
                        authUpdateTask();
                        Monitor.PulseAll(authUpdateLock);
                    }
                    else
                    {
                        Monitor.Wait(authUpdateLock);
                    }
                }
            }
        }

        enum QueryExecutionStatus
        {
            NotStarted,
            Running,
            Serializing,
            AwaitingUserAuthentication,
            AuthenticationNeeded,
            RetryPending,
            RetryReady,
            Canceled,
            Complete,
            Failed
        }

        public static void PrintQueries(PQDocument doc)
        {
            var sharedMembers = doc.SharedMembers;
            foreach (var sharedMember in sharedMembers)
            {
                Console.WriteLine(sharedMember.Name);
            }
        }

        private class Authentication
        {
            private delegate ref T RefFunc<T>();
            public delegate DataSourceSetting ResolveAuthentication(DataSource dataSource);

            public Resolvers UserResolvers { get; }
            private readonly Dictionary<DataSource, DataSourceSetting> dataSourceAuthentication = new Dictionary<DataSource, DataSourceSetting>();
            private readonly RefDictionary<DataSource, IDataSourceResolver> pendingDataSourceAuthentication = new RefDictionary<DataSource, IDataSourceResolver>();
            private readonly string activityId = Guid.NewGuid().ToString();

            public Authentication()
            {
                this.UserResolvers = new Resolvers(this);
            }

            public (Form, WebBrowser) AuthProvider(OAuthBrowserNavigation login)
            {
                WebBrowser authPage = new WebBrowser()
                {
                    Dock = DockStyle.Fill,
                    ScrollBarsEnabled = false
                };
                Form window = new Form();
                authPage.Url = login.LoginUri;
                window.Width = login.WindowWidth;
                window.Height = login.WindowHeight;
                window.Controls.Add(authPage);
                return (window, authPage);
            }

            public ref IDataSourceResolver AuthenticationResolver(DataSource dataSource)
            {
                return ref pendingDataSourceAuthentication[dataSource];
            }

            public (bool success, DataSourceSetting) UserAAD(DataSource dataSource)
            {
                var oauthProvider = dataSource.GetOAuthProvider(new OAuthClientApplication("a672d62c-fc7b-4e81-a576-e60dc46e951d", "", "https://de-users-preview.sqlazurelabs.com/account/reply/"));
                var startLogin = oauthProvider.StartLogin(activityId, "");
                (bool success, DataSourceSetting auth) result = (false, null);
                (Form window, WebBrowser authPage) = AuthProvider(startLogin);
                using (window)
                using (authPage)
                {
                    Uri callbackUri = null;
                    var pageNavListener = AuthPage_Navigated((uri) => { callbackUri = uri; });
                    WebBrowserNavigatedEventHandler AuthPage_Navigated(Action<Uri> redirectUri)
                    {
                        void Navigated(object sender, WebBrowserNavigatedEventArgs navEvent)
                        {
                            if (navEvent.Url.AbsoluteUri.StartsWith(startLogin.CallbackUri.AbsoluteUri, StringComparison.Ordinal))
                            {
                                redirectUri(navEvent.Url);
                                authPage.Navigated -= Navigated;
                                window.Close();
                            }
                        };
                        return Navigated;
                    }
                    authPage.Navigated += pageNavListener;
                    window.ShowDialog();
                    authPage.Navigated -= pageNavListener;
                    if (callbackUri != null)
                    {
                        var creds = oauthProvider.FinishLogin(startLogin.SerializedContext, callbackUri, activityId);
                        result = (true, DataSourceSetting.CreateOAuth2Credential(creds.AccessToken));
                    }
                    else
                    {
                        result = (false, null);
                    }
                }
                return result;
            }

            public class Resolvers
            {
                private Authentication Auth { get; }

                public Resolvers(Authentication auth)
                {
                    this.Auth = auth;
                }

                public class Ref<T>
                {
                    public T value;
                }
                private class DataSourceResolver : IDataSourceResolver
                {
                    public ResolveAuthentication Resolver { get; }

                    public DataSourceResolver(ResolveAuthentication resolver)
                    {
                        this.Resolver = resolver;
                    }

                    DataSourceSetting IDataSourceResolver.Resolve(DataSource datSource) => Resolver(datSource);
                }
            }

            public interface IDataSourceResolver
            {
                DataSourceSetting Resolve(DataSource dataSource);
            }
        }
    }
}
