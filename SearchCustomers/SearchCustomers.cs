using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using JE.RMS.Common;
using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;
using System.Data.SqlClient;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using JE.RMS.Common.Model;
using System.Text.RegularExpressions;

namespace JE.RMS.Services
{
    public class SearchCustomers
    {
        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");

            try
            {
                // Get request body
                string reqString = await req.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<SearchCustomerRequest>(reqString);

                log.Verbose($"received data :={data}", "JE.RMS.Services.SearchCustomers");
                string errmsg = "";
                string tempsourceSystemId = "0";
                bool flgvalidate = true,flgdone=false;
                
                if (data.SourceSystemUniqueID != null && data.SourceSystemUniqueIDType != null && flgvalidate)
                {

                    if (string.IsNullOrEmpty(data.SourceSystemUniqueID) && string.IsNullOrEmpty(data.SourceSystemUniqueIDType))
                    {
                        flgvalidate = true;
                    }
                    else
                    {
                        if (data.SourceSystemUniqueID != "")
                        {
                            if (data.SourceSystemUniqueIDType != "")
                            {
                                flgvalidate = true;
                            }
                            else
                            {
                                flgvalidate = false;
                                flgdone = true;
                                errmsg += "Validation Failed : SourceSystemUniqueID & SourceSystemUniqueIDType, either none or both are required.";
                            }
                        }
                        else
                        {
                            flgvalidate = false;
                            if (!flgdone)
                            {
                                errmsg += "Validation Failed : SourceSystemUniqueID & SourceSystemUniqueIDType, either none or both are required.";
                                flgdone = true;
                            }
                        }
                        if (data.SourceSystemUniqueIDType != "")
                        {
                            if (data.SourceSystemUniqueID != "")
                            {
                                flgvalidate = true;
                            }
                            else
                            {
                                flgvalidate = false;
                                if (!flgdone)
                                {
                                    errmsg += "Validation Failed : SourceSystemUniqueID & SourceSystemUniqueIDType, either none or both are required.";
                                    flgdone = true;
                                }
                            }
                        }
                        else
                        {
                            flgvalidate = false;
                            if (!flgdone)
                            {
                                errmsg += "Validation Failed : SourceSystemUniqueID & SourceSystemUniqueIDType, either none or both are required.";
                                flgdone = true;
                            }
                        }
                    }
                }
                else if (data.SourceSystemUniqueID != null && flgvalidate)
                {
                    if (data.SourceSystemUniqueIDType != null)
                    {

                    }
                    else
                    {
                        flgvalidate = false;
                        if (!flgdone)
                        {
                            if (data.SourceSystemUniqueID != "")
                            {
                                errmsg += "Validation Failed : SourceSystemUniqueID & SourceSystemUniqueIDType, either none or both are required.";
                                flgdone = true;
                            }
                        }
                    }
                }
                else if (data.SourceSystemUniqueIDType != null && flgvalidate)
                {
                    if (data.SourceSystemUniqueID != null)
                    {

                    }
                    else
                    {
                        flgvalidate = false;
                        if (!flgdone)
                        {
                            if (data.SourceSystemUniqueIDType != "")
                            {
                                errmsg += "Validation Failed : SourceSystemUniqueID & SourceSystemUniqueIDType, either none or both are required.";
                                flgdone = true;
                            }
                        }
                    }
                }
                
                if(data.SourceSystemID != null && flgvalidate)
                {
                    tempsourceSystemId = data.SourceSystemID.ToString();
                    if (tempsourceSystemId != "" && tempsourceSystemId !="0")
                    {
                        try
                        {

                            int tid = 0;
                            int.TryParse(tempsourceSystemId, out tid);
                            if (tid <= 0)
                            {
                                flgvalidate = false;
                                errmsg += "Validation Failed : SourceSystemID must be numeric";
                            }
                        }
                        catch (Exception ex)
                        {
                            flgvalidate = false;
                            errmsg += "Validation Failed : sourceSystemID must be numeric";
                        }
                    }
                    else
                    {
                        tempsourceSystemId = "0";
                    }
                }

                if (flgvalidate)
                {

                    List<SqlParameter> objprmlookup = new List<SqlParameter>();
                    objprmlookup.Add(new SqlParameter("@sourceSystemID", data.SourceSystemID == null ? "0" : data.SourceSystemID.ToString()));
                    objprmlookup.Add(new SqlParameter("@sourceSystemNames", data.SourceSystemName == null ? "" : data.SourceSystemName.ToString()));
                  
                    var objvalidate = MSSQLConnection.ExecuteStoredProcedure<string>(Common.Constants.USPContstants.CheckLookupValidation, objprmlookup);
                    var Message = "";
                    if (objvalidate.Count == 1)
                    {
                        if (!objvalidate[0].ToString().ToLower().Equals("all validation passed"))
                        {
                            Message = objvalidate[0].ToString();

                            return req.CreateResponse(HttpStatusCode.BadRequest, new { Message });
                        }
                    }
                    else
                    {
                        foreach(var temp in objvalidate)
                        {
                            Message += temp.ToString();
                        }

                        return req.CreateResponse(HttpStatusCode.BadRequest, new { Message });
                    }


                    List<SqlParameter> objprm = new List<SqlParameter>();

                    objprm.Add(new SqlParameter("@email", data.Email == null ? "" : data.Email.ToString()));
                    objprm.Add(new SqlParameter("@masterID", data.MasterID == null ? "" : data.MasterID.ToString()));
                    objprm.Add(new SqlParameter("@sourceSystemUniqueID", data.SourceSystemUniqueID == null ? "" : data.SourceSystemUniqueID));
                    objprm.Add(new SqlParameter("@sourceSystemUniqueIDType", data.SourceSystemUniqueIDType == null ? "" : data.SourceSystemUniqueIDType));
                    objprm.Add(new SqlParameter("@sourceSystemID", data.SourceSystemID == null ? "0" : tempsourceSystemId));
                    objprm.Add(new SqlParameter("@sourceSystemName", data.SourceSystemName == null ? "" : data.SourceSystemName.ToString()));

                    log.Verbose($"calling sp", "JE.RMS.Services.SearchCustomers");

                    List<Common.Model.CustomerTemp> retobj = MSSQLConnection.ExecuteStoredProcedure<Common.Model.CustomerTemp>(Common.Constants.USPContstants.SearchCustomers, objprm);
                    Common.Model.SearchCustomerExtended ceobj;

                    Common.Model.SearchCustomers obj = new Common.Model.SearchCustomers();
                    List<Common.Model.SearchCustomers> objlist = new List<Common.Model.SearchCustomers>();
                    int previd = -1;
                    if (retobj.Count > 0)
                    {
                        foreach (var temp in retobj)
                        {
                            ceobj = new Common.Model.SearchCustomerExtended();

                            if (temp.CustomerID == previd)
                            {
                                if (temp.CustomerExtendedID != null)
                                {

                                    ceobj.AccountAcceptanceDate = temp.AccountAcceptanceDate;
                                    ceobj.AccountStatus = temp.AccountStatus;
                                    ceobj.AvailablePointBalance = temp.AvailablePointBalance;
                                    ceobj.AvailablePointBalanceDollars = temp.AvailablePointBalanceDollars;
                                    ceobj.ChannelCode = temp.ChannelCode;
                                    ceobj.ChannelName = temp.ChannelName;
                                    ceobj.NextRewardDueDate = temp.NextRewardDueDate;
                                    ceobj.NumberofTransactions = temp.NumberofTransactions;
                                    ceobj.StartingPointBalance = temp.StartingPointBalance;
                                    ceobj.UniqueID = temp.UniqueID;
                                    obj.CustomerExtended.Add(ceobj);

                                }

                            }
                            else
                            {
                                obj = new Common.Model.SearchCustomers();
                                obj.CustomerExtended = new List<Common.Model.SearchCustomerExtended>();

                                obj.AddressLine1 = temp.AddressLine1;
                                obj.AddressLine2 = temp.AddressLine2;
                                obj.City = temp.City;
                                obj.CompanyName = temp.CompanyName;
                                obj.Email = temp.Email;
                                obj.FirstName = temp.FirstName;
                                obj.Language = temp.Language;
                                obj.LastName = temp.LastName;
                                obj.MasterID = temp.MasterID;
                                obj.Phone1 = temp.Phone1;
                                obj.Product = temp.Product;
                                obj.StateProvince = temp.StateProvince;
                                obj.ZipPostalCode = temp.ZipPostalCode;
                                obj.CustomerStatus = temp.CustomerStatus;
                                obj.Jurisdiction = temp.Jurisdiction;
                                obj.Country = temp.Country;

                                if (temp.CustomerExtendedID != null)
                                {
                                    ceobj.AccountAcceptanceDate = temp.AccountAcceptanceDate;
                                    ceobj.AccountStatus = temp.AccountStatus;
                                    ceobj.AvailablePointBalance = temp.AvailablePointBalance;
                                    ceobj.AvailablePointBalanceDollars = temp.AvailablePointBalanceDollars;
                                    ceobj.ChannelCode = temp.ChannelCode;
                                    ceobj.ChannelName = temp.ChannelName;
                                    ceobj.NextRewardDueDate = temp.NextRewardDueDate;
                                    ceobj.NumberofTransactions = temp.NumberofTransactions;
                                    ceobj.StartingPointBalance = temp.StartingPointBalance;
                                    ceobj.UniqueID = temp.UniqueID;
                                    obj.CustomerExtended.Add(ceobj);

                                }
                                objlist.Add(obj);

                            }
                            previd = temp.CustomerID;
                        }
                    }
                    log.Verbose($"final response", "JE.RMS.Services.SearchCustomers");

                    return req.CreateResponse(HttpStatusCode.OK, objlist);
                }
                else
                {
                    log.Verbose($"validation failed", "JE.RMS.Services.SearchCustomers");
                    return req.CreateErrorResponse(HttpStatusCode.BadRequest, errmsg );
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception ={ex}");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }

        }
        
    }


}