using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.MSIdentity.Properties;
using Microsoft.DotNet.MSIdentity.Shared;
using Microsoft.Graph;

namespace Microsoft.DotNet.MSIdentity.Tool
{
    public interface IGraphObjectRetriever
    {
        /// <summary>
        /// Requests all directory objects from the graph service client for the given account
        /// </summary>
        /// <returns></returns>
        public Task<List<DirectoryObject>?> GetGraphObjects();

        /// <summary>
        /// Retrieves the Tenant object associated with the GraphServiceClient
        /// </summary>
        /// <returns></returns>
        public Task<Organization?> GetTenant();
    }

    public class GraphObjectRetriever : IGraphObjectRetriever
    {
        private readonly GraphServiceClient _graphServiceClient;
        private readonly IConsoleLogger _consoleLogger;

        public GraphObjectRetriever(GraphServiceClient graphServiceClient, IConsoleLogger consoleLogger)
        {
            _graphServiceClient = graphServiceClient;
            _consoleLogger = consoleLogger;
        }

        public async Task<List<DirectoryObject>?> GetGraphObjects()
        {
            List<DirectoryObject> graphObjectsList = new List<DirectoryObject>();
            try
            {
                var graphObjects = await _graphServiceClient.Me.OwnedObjects
                    .Request()
                    .GetAsync();

                if (graphObjects != null)
                {
                    graphObjectsList.AddRange(graphObjects.ToList());

                    var nextPage = graphObjects.NextPageRequest;
                    while (nextPage != null)
                    {
                        try
                        {
                            var additionalGraphObjects = await nextPage.GetAsync();
                            if (additionalGraphObjects != null)
                            {
                                graphObjectsList.AddRange(additionalGraphObjects.ToList());
                                nextPage = additionalGraphObjects.NextPageRequest;
                            }
                            else
                            {
                                nextPage = null;
                            }
                        }
                        catch (ServiceException)
                        {
                            nextPage = null;
                            _consoleLogger.LogMessage(Resources.FailedToRetrieveADObjectsError, LogMessageType.Error);
                            return null;
                        }
                    }
                }
            }
            catch (ServiceException)
            {
                _consoleLogger.LogMessage(Resources.FailedToRetrieveADObjectsError, LogMessageType.Error);
                return null;
            }

            return graphObjectsList;
        }

        public async Task<Organization?> GetTenant()
        {
            Organization? tenant;
            try
            {
                tenant = (await _graphServiceClient.Organization
                    .Request()
                    .GetAsync()).FirstOrDefault();
            }
            catch (ServiceException ex)
            {
                string? errorMessage;
                if (ex.InnerException != null)
                {
                    errorMessage = ex.InnerException.Message;
                }
                else
                {
                    if (ex.Message.Contains("User was not found") || ex.Message.Contains("not found in tenant"))
                    {
                        errorMessage = "User was not found.\nUse both --tenant-id <tenant> --username <username@tenant>.\nAnd re-run the tool.";
                    }
                    else
                    {
                        errorMessage = ex.Message;
                    }
                }

                _consoleLogger.LogJsonMessage(new JsonResponse(null, State.Fail, output:errorMessage));
                return null;
            }

            return tenant;
        }
    }
}
