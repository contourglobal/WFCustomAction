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
    public class CheckReqReady
    {
        private string result = string.Empty;

        public Hashtable CheckRequirementReady(SPUserCodeWorkflowContext context, string id)
        {
            Hashtable results = new Hashtable();
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        int reqId;
                        if (int.TryParse(id, out reqId))
                        {
                            SPList requirementsList = web.Lists["Investor relations requirements"];
                            SPList contributorsList = web.Lists["Contributors"];

                            if (requirementsList != null && contributorsList != null)
                            {
                                if (IsReqReady(contributorsList, reqId))
                                {
                                    SPListItem requirement = requirementsList.GetItemById(reqId);
                                    if (requirement != null)
                                    {
                                        requirement["Status"] = "Late Draft for Review";
                                        requirement.Update();
                                    }
                                }
                            }
                        }
                    }
                }

                results["result"] = result;
                results["success"] = true;
            }
            catch (Exception e)
            {
                results = new Hashtable();
                results["result"] = e.ToString();
                results["success"] = false;
            }
            return results;
        }

        private bool IsReqReady(SPList contributorsList, int reqId)
        {
            SPQuery query = new SPQuery();
            query.Query = "<Where><Eq><FieldRef Name='IRR_x0020_ID' /><Value Type='Text'>" + reqId + "</Value></Eq></Where>";

            SPListItemCollection items = contributorsList.GetItems(query);

            if (items != null && items.Count > 0)
            {
                foreach (SPListItem contributor in items)
                {
                    if ((bool)contributor["IsCompleted"] == false)
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
