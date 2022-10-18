using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;

namespace LinqQueriesLateBinding
{
    public class LinqQueris : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService =
            (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            // The InputParameters collection contains all the data passed in the message request.  
            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.  
                Entity entity = (Entity)context.InputParameters["Target"];
                var phoneNumber = entity.GetAttributeValue<string>("telephone1");
                // Obtain the organization service reference which you will need for  
                // web service calls.  
                IOrganizationServiceFactory serviceFactory =
                    (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                try
                {
                    if (context.Depth > 1) return;
                    using (OrganizationServiceContext orgSvcContext = new OrganizationServiceContext(service))
                    {
                        var query_dejoin = (from a in orgSvcContext.CreateQuery("account")
                                           join c in orgSvcContext.CreateQuery("contact") on a.GetAttributeValue<Guid>("accountid") equals c.GetAttributeValue<Guid>("parentcustomerid")
                                           where a.GetAttributeValue<Guid?>("accountid") == entity.Id
                                           select new {
                                               Contact = new
                                               {
                                                   ContactId = c.GetAttributeValue<Guid>("contactid"),
                                                   LogicalName = c.LogicalName.ToString()
                                               }
                                                   
                                           }).ToArray();

                        
                        foreach (var item in query_dejoin) 
                        {
                            Entity contact = new Entity(item.Contact.LogicalName);
                            contact.Id = item.Contact.ContactId;
                            contact["mobilephone"] = phoneNumber;
                            service.Update(contact);
                        }
                       

                    }
                }

                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("An error occurred in FollowUpPlugin.", ex);
                }

                catch (Exception ex)
                {
                    tracingService.Trace("FollowUpPlugin: {0}", ex.ToString());
                    throw;
                }
            }
        }
    }
}
