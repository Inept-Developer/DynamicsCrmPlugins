using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;
using System.Activities;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using System.Collections.ObjectModel;

namespace RegisterCandidatesToApplication
{
    public class RegisterCandidates: IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {

            ITracingService tracingService =(ITracingService)serviceProvider.GetService(typeof(ITracingService));
             
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            IOrganizationServiceFactory serviceFactory =(IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                var guid = ((EntityReference)context.InputParameters["EntityId"]).Id;
                Entity enquiry = new Entity("wssm_equiry");
                enquiry.Id = guid;                
                var guardianName = enquiry.GetAttributeValue<string>("wssm_name");
                var contactNumber = enquiry.GetAttributeValue<string>("wssm_contactnumber");
                var emailId = enquiry.GetAttributeValue<string>("emailaddress");
                string applicationGuid = null;
                OptionSetValue statecode = new OptionSetValue(1);
                OptionSetValue statuscode = new OptionSetValue(2);
                string fetchXml = @"<fetch version='1.0' mapping='logical' no-lock='false' distinct='true'>
                                        <entity name='wssm_candidate'>
                                        <attribute name='wssm_name'/>
                                        <attribute name='createdon'/>
                                        <order attribute='wssm_name' descending='false'/>
                                        <attribute name='wssm_status'/>
                                        <attribute name='wssm_seatavailability'/>
                                        <attribute name='wssm_fathername'/>
                                        <attribute name='wssm_class'/>
                                        <attribute name='wssm_candidateid'/>
                                        <attribute name='wssm_lastname'/>
                                        <filter type='and'><condition attribute='wssm_enquirianname' operator='eq' value='{3d7e39b3-ce59-ed11-9562-002248d6249c}' >
                                        </filter></entity></fetch>";
                
                    fetchXml = string.Format(fetchXml, guid);
                    var qe = new FetchExpression(fetchXml);
                    var result = service.RetrieveMultiple(qe);

                foreach (var e in result.Entities)
                {
                    Entity updatedCandidate = new Entity(e.LogicalName);
                    Entity Application = new Entity("lead");
                    updatedCandidate.Id = e.Id;                   
                    var seatAvaliablity = e["wssm_seatavailability"]; 
                    if (seatAvaliablity.ToString().ToLower() == "available")
                    {
                        var selection = new OptionSetValue(433360003);
                        updatedCandidate["wssm_status"] = selection;
                        service.Update(updatedCandidate);
                        Application["lastname"] = e.GetAttributeValue<string>("wssm_lastname");
                        Application["firstname"] = e.GetAttributeValue<string>("wssm_name");
                        Application["wssm_class"] = e.GetAttributeValue<OptionSetValue>("wssm_class");
                        Application["companyname"] = e.GetAttributeValue<string>("wssm_fathername");
                        Application["wssm_guardianname"] = guardianName;
                        Application["mobilephone"] = contactNumber;
                        Application["emailaddress1"] = emailId;
                        applicationGuid = service.Create(Application).ToString();
                    }
                    else if (seatAvaliablity.ToString().ToLower() == "full")
                    {
                        var selection = new OptionSetValue(433360001);
                        updatedCandidate["wssm_status"] = selection;
                        service.Update(updatedCandidate);
                    }
                    else 
                    {
                        var selection = new OptionSetValue(433360002);
                        updatedCandidate["wssm_status"] = selection;
                        service.Update(updatedCandidate);

                    }          

                }
               
                enquiry["statecode"] = statecode;
                enquiry["statuscode"] = statuscode;
                service.Update(enquiry);
                context.OutputParameters["ApplicationId"] = applicationGuid.ToString();


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



