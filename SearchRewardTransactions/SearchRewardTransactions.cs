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
    public class SearchRewardTransactions
    {
        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");

            try
            {
                // Get request body
                string reqString = await req.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<SearchRewardTrxRequest>(reqString);

                log.Verbose($"received data :={data}", "JE.RMS.Services.SearchRewardTransactions");
                string errmsg = "";

                bool flgvalidate = true;
                if (data.SourceSystemUniqueID != null && data.SourceSystemUniqueIDTypes != null && flgvalidate)
                {
                    if (string.IsNullOrEmpty(data.SourceSystemUniqueID) && string.IsNullOrEmpty(data.SourceSystemUniqueIDTypes))
                    {
                        flgvalidate = true;
                    }
                    else
                    {
                        if (data.SourceSystemUniqueID.ToString() != "")
                        {
                            if (data.SourceSystemUniqueIDTypes.ToString() != "")
                            {
                                flgvalidate = true;
                            }
                            else
                            {
                                flgvalidate = false;
                                errmsg = "Validation Failed : SourceSystemUniqueID & sourceSystemUniqueIDTypes either none or both are required.";
                            }
                        }
                        if (data.SourceSystemUniqueIDTypes.ToString() != "")
                        {
                            if (data.SourceSystemUniqueID.ToString() != "")
                            {
                                flgvalidate = true;
                            }
                            else
                            {
                                flgvalidate = false;
                                errmsg = "Validation Failed : SourceSystemUniqueID & sourceSystemUniqueIDTypes either none or both are required.";
                            }
                        }
                    }
                }
                else if(data.SourceSystemUniqueID != null && flgvalidate)
                {
                    if(data.SourceSystemUniqueIDTypes != null)
                    {

                    }
                    else
                    {
                        if (data.SourceSystemUniqueID != "")
                        {
                            flgvalidate = false;
                            errmsg = "Validation Failed : SourceSystemUniqueID & sourceSystemUniqueIDTypes either none or both are required.";
                        }
                    }
                }
                else if (data.SourceSystemUniqueIDTypes != null && flgvalidate)
                {
                    if (data.SourceSystemUniqueID != null)
                    {

                    }
                    else
                    {
                        if(data.SourceSystemUniqueIDTypes !="")
                        {
                            flgvalidate = false;
                            errmsg = "Validation Failed : SourceSystemUniqueID & sourceSystemUniqueIDTypes either none or both are required.";
                        }
                        
                    }
                }

                if(data.SourceSystemIDs != null && flgvalidate)
                {
                    string isvalidids = data.SourceSystemIDs.ToString();
                    if(isvalidids !="")
                    {
                        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"^(\d+(,\d+)*)?$");
                        System.Text.RegularExpressions.Match match = regex.Match(isvalidids);
                        if (!match.Success)
                        {
                            flgvalidate = false;
                            errmsg = "Validation Failed : all comma separated values for sourceSystemIDs must be numeric.";
                        }
                    }
                }

                if (data.CountryIDs != null && flgvalidate)
                {
                    string isvalidids = data.CountryIDs.ToString();
                    if (isvalidids != "")
                    {
                        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"^(\d+(,\d+)*)?$");
                        System.Text.RegularExpressions.Match match = regex.Match(isvalidids);
                        if (!match.Success)
                        {
                            flgvalidate = false;
                            errmsg = "Validation Failed : all comma separated values for countryIDs must be numeric.";
                        }
                    }
                }

                if (data.JurisdictionIDs != null && flgvalidate)
                {
                    string isvalidids = data.JurisdictionIDs.ToString();
                    if (isvalidids != "")
                    {
                        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"^(\d+(,\d+)*)?$");
                        System.Text.RegularExpressions.Match match = regex.Match(isvalidids);
                        if (!match.Success)
                        {
                            flgvalidate = false;
                            errmsg = "Validation Failed : all comma separated values for jurisdictionIDs must be numeric.";
                        }
                    }
                }

                if (data.TransactionTypeIDs != null && flgvalidate)
                {
                    string isvalidids = data.TransactionTypeIDs.ToString();
                    if (isvalidids != "")
                    {
                        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"^(\d+(,\d+)*)?$");
                        System.Text.RegularExpressions.Match match = regex.Match(isvalidids);
                        if (!match.Success)
                        {
                            flgvalidate = false;
                            errmsg = "Validation Failed : all comma separated values for transactionTypeIDs must be numeric.";
                        }
                    }
                }

                if (data.TransactionStatusIDs != null && flgvalidate)
                {
                    string isvalidids = data.TransactionStatusIDs.ToString();
                    if (isvalidids != "")
                    {
                        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"^(\d+(,\d+)*)?$");
                        System.Text.RegularExpressions.Match match = regex.Match(isvalidids);
                        if (!match.Success)
                        {
                            flgvalidate = false;
                            errmsg = "Validation Failed : all comma separated values for transactionStatusIDs must be numeric.";
                        }
                    }
                }

                if (flgvalidate)
                {
                    List<SqlParameter> objprmlookup = new List<SqlParameter>();
                    objprmlookup.Add(new SqlParameter("@sourceSystemNames", data.SourceSystemNames == null ? "" : data.SourceSystemNames.ToString()));
                    objprmlookup.Add(new SqlParameter("@transactionTypes", data.TransactionTypes == null ? "" : data.TransactionTypes.ToString()));
                    objprmlookup.Add(new SqlParameter("@transactionStatuses", data.TransactionStatuses == null ? "" : data.TransactionStatuses.ToString()));

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
                        foreach (var temp in objvalidate)
                        {
                            Message += temp.ToString();
                        }

                        return req.CreateResponse(HttpStatusCode.BadRequest, new { Message });
                    }


                    List<SqlParameter> objprm = new List<SqlParameter>();
                    objprm.Add(new SqlParameter("@startDate", data.StartDate == null ? "" : data.StartDate.ToString()));
                    objprm.Add(new SqlParameter("@endDate", data.EndDate == null ? "" : data.EndDate.ToString()));
                    objprm.Add(new SqlParameter("@sourceSystemUniqueID", data.SourceSystemUniqueID == null ? "" : data.SourceSystemUniqueID.ToString()));
                    objprm.Add(new SqlParameter("@email", data.Email == null ? "" : data.Email.ToString()));
                    objprm.Add(new SqlParameter("@sourceSystemIDs", data.SourceSystemIDs == null ? "" : data.SourceSystemIDs.ToString()));
                    objprm.Add(new SqlParameter("@countryIDs", data.CountryIDs == null ? "" : data.CountryIDs.ToString()));
                    objprm.Add(new SqlParameter("@jurisdictionIDs", data.JurisdictionIDs == null ? "" : data.JurisdictionIDs.ToString()));
                    objprm.Add(new SqlParameter("@transactionTypeIDs", data.TransactionTypeIDs == null ? "" : data.TransactionTypeIDs.ToString()));
                    objprm.Add(new SqlParameter("@transactionStatusIDs", data.TransactionStatusIDs == null ? "" : data.TransactionStatusIDs.ToString()));
                    objprm.Add(new SqlParameter("@sourceSystemNames", data.SourceSystemNames == null ? "" : data.SourceSystemNames.ToString()));
                    objprm.Add(new SqlParameter("@transactionTypes", data.TransactionTypes == null ? "" : data.TransactionTypes.ToString()));
                    objprm.Add(new SqlParameter("@transactionStatuses", data.TransactionStatuses == null ? "" : data.TransactionStatuses.ToString()));
                    objprm.Add(new SqlParameter("@sourceSystemUniqueIDTypes", data.SourceSystemUniqueIDTypes == null ? "" : data.SourceSystemUniqueIDTypes.ToString()));
                    objprm.Add(new SqlParameter("@pageNumber", data.PageNumber == null ? 1 : Convert.ToInt32(data.PageNumber.ToString())));
                    objprm.Add(new SqlParameter("@pageSize", data.PageSize == null ? 50 : Convert.ToInt32(data.PageSize.ToString())));
                    log.Verbose($"calling sp", "JE.RMS.Services.SearchRewardTransactions");

                    List<Common.Model.RewardTrx> retobj = MSSQLConnection.ExecuteStoredProcedure<Common.Model.RewardTrx>(Common.Constants.USPContstants.SearchRewardsTrx, objprm);

                    Common.Model.SearchRewardTrx obj = new Common.Model.SearchRewardTrx();

                    if (retobj.Count > 0)
                    {
                        //JsonConvert.DeserializeObject
                        obj.TotalRows = retobj.FirstOrDefault().TotalRows;
                        obj.RewardTransactions = new List<Common.Model.RewardTrx>();
                        obj.RewardTransactions = retobj;

                        //var rewardobjJSON= JsonConvert.SerializeObject(obj);
                        var RewardResponse = req.CreateResponse(HttpStatusCode.OK);
                        RewardResponse.Content = new StringContent(JsonConvert.SerializeObject(obj), System.Text.Encoding.UTF8, "application/json");
                        return RewardResponse;
                    }
                    else
                    {
                        //JsonConvert.DeserializeObject
                        obj.TotalRows = 0;
                        obj.RewardTransactions = new List<Common.Model.RewardTrx>();
                        obj.RewardTransactions = retobj;

                        //var rewardobjJSON= JsonConvert.SerializeObject(obj);
                        var RewardResponse = req.CreateResponse(HttpStatusCode.OK);
                        RewardResponse.Content = new StringContent(JsonConvert.SerializeObject(obj), System.Text.Encoding.UTF8, "application/json");
                        return RewardResponse;
                    }
                    //return req.CreateResponse(HttpStatusCode.OK, rewardobjJSON);
                }
                else
                {
                    log.Verbose($"validation failed", "JE.RMS.Services.SearchCustomers");
                    return req.CreateErrorResponse(HttpStatusCode.BadRequest, errmsg);
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