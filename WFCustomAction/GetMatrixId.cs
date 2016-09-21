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
    public class GetMatrixId
    {
        public Hashtable GetApprovalMatrixId(SPUserCodeWorkflowContext context, string region, string criticality, string confidentiality)
        {
            Hashtable results = new Hashtable();
            results["result"] = string.Empty;
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        if (region != string.Empty && criticality != string.Empty && confidentiality != string.Empty)
                        {
                            results["result"] = GetCompletionStatus(web, region, criticality, confidentiality);
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

        private string GetCompletionStatus(SPWeb web, string region, string criticality, string confidentiality)
        {
            SPList matrixList = web.Lists["Approval Matrix"];
            if (matrixList != null)
            {
                SPQuery query = new SPQuery();
                query.Query = "<Where><And><And><Eq><FieldRef Name='Region'></FieldRef><Value Type='Text'>" + region + "</Value></Eq>" +
                                "<Eq><FieldRef Name='Criticality'></FieldRef><Value Type='Text'>" + criticality + "</Value></Eq></And>" +
                                "<Eq><FieldRef Name='Confidentiality'></FieldRef><Value Type='Text'>" + confidentiality + "</Value></Eq></And></Where>";

                SPListItemCollection items = matrixList.GetItems(query);

                if (items != null && items.Count > 0)
                {
                    return items[0]["ID"].ToString();
                }
            }
            return "";
        }
    }
}
