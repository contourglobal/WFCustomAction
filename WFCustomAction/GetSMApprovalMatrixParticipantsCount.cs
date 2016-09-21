using Microsoft.SharePoint;
using Microsoft.SharePoint.UserCode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WFCustomAction
{
    public class GetSMApprovalMatrixParticipantsCount
    {
        public Hashtable GetApprovalMatrixParticipantsCount(SPUserCodeWorkflowContext context, string region, string documentGroup)
        {
            Hashtable results = new Hashtable();
            results["result"] = string.Empty;
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        if (region != string.Empty && documentGroup != string.Empty)
                        {
                            results["result"] = GetParticipants(web, region, documentGroup);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                results = new Hashtable();
                results["result"] = e.ToString();
                results["success"] = false;
            }

            return results;
        }

        private string GetParticipants(SPWeb web, string region, string documentGroup)
        {
            byte count = 0; 
            SPList matrixList = web.Lists["SM Approval Matrix"];
            if (matrixList != null)
            {
                SPQuery query = new SPQuery();
                query.Query = "<Where><And><Eq><FieldRef Name='Region'></FieldRef><Value Type='Text'>" + region + "</Value></Eq>" +
                                "<Eq><FieldRef Name='Document_x0020_Group'></FieldRef><Value Type='Text'>" + documentGroup + "</Value></Eq></And></Where>";

                SPListItemCollection items = matrixList.GetItems(query);

                if (items != null && items.Count > 0)
                {
                    if (items[0]["Legal"] != null && !string.IsNullOrEmpty(items[0]["Legal"].ToString()))
                    {
                        count++;
                    }
                    if (items[0]["Tax"] != null && !string.IsNullOrEmpty(items[0]["Tax"].ToString()))
                    {
                        count++;
                    }
                    if (items[0]["Finance"] != null && !string.IsNullOrEmpty(items[0]["Finance"].ToString()))
                    {
                        count++;
                    }
                    if (items[0]["HR"] != null && !string.IsNullOrEmpty(items[0]["HR"].ToString()))
                    {
                        count++;
                    }
                    if (items[0]["Compliance"] != null && !string.IsNullOrEmpty(items[0]["Compliance"].ToString()))
                    {
                        count++;
                    }
                    if (items[0]["Internal_x0020_Control"] != null && !string.IsNullOrEmpty(items[0]["Internal_x0020_Control"].ToString()))
                    {
                        count++;
                    }
                }
            }
            return count.ToString();
        }
    }
}
