using Microsoft.Azure.NotificationHubs;
using PushApi.Common.Models;
using PushApi.Common.Repositories;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace PushApi.Send.Controllers
{
    //[Authorize]
    public class PushController : ApiController
    {
        DbRepository _dbRepo;
 

        public PushController()
        {
            _dbRepo = new DbRepository();
            
        }

        [SwaggerResponse(System.Net.HttpStatusCode.OK, "HttpResponseMessage", typeof(HttpResponseMessage))]
        public async Task<HttpResponseMessage> Post([FromBody]SendPayload sendPayload)
        {
            HttpStatusCode ret = HttpStatusCode.InternalServerError;
            List<NotificationOutcome> outcomes = null;
            AzureNotificationHub hub = await _dbRepo.GetAzureNotificationHubEndpoint(sendPayload.AppId);

            if (string.IsNullOrEmpty(hub.Endpoint) && string.IsNullOrEmpty(hub.HubName))
            {
                throw new Exception($"Unable to find an enpoint for appId = '{sendPayload.AppId}'");
            }

            var _nhRepo = new NotificationHubRepository(hub.Endpoint, hub.HubName);

            if (!string.IsNullOrEmpty(sendPayload.UserId))
            {
                var pns = await _dbRepo.GetPns(sendPayload.AppId, sendPayload.UserId);

                outcomes = await _nhRepo.Send(sendPayload, pns);
            }
            else {
                outcomes = await _nhRepo.Send(sendPayload);
            }

            if (outcomes != null)
            {
                ret = HttpStatusCode.OK;

                foreach (var outcome in outcomes)
                {
                    if ((outcome.State == NotificationOutcomeState.Abandoned) ||
                        (outcome.State == NotificationOutcomeState.Unknown))
                    {
                        ret = HttpStatusCode.InternalServerError;
                        break;
                    }
                }
            }

            return Request.CreateResponse(ret, "{\"result\":\"OK\"}");
        }
    }
}