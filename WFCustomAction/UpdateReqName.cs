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
    public class UpdateReqName                                                          //Unused !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    {
        private string result = string.Empty;

        public Hashtable UpdateRequirementName(SPUserCodeWorkflowContext context)
        {
            Hashtable results = new Hashtable();
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        SPList historyList = web.Lists["CG Debts History"];

                        if (historyList != null)
                        {
                            using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                            {
                                SPListItemCollection items = historyList.Items;

                                foreach (SPListItem item in items)
                                {
                                    //if (item["ID"].ToString() == "382" || item["ID"].ToString() == "384" || item["ID"].ToString() == "385")     //temporary for tests only
                                    {
                                        if (item["Requirement Id"] != null && (item["Requirement Name"] == null || item["Requirement Name"].ToString() != string.Empty))
                                        {
                                            item["Requirement Name"] = GetRequirementName(web, item["Requirement Id"].ToString());
                                            item.Update();
                                        }
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

        private string GetRequirementName(SPWeb web, string id)
        {
            SPList requirementsList = web.Lists["Requirements"];
            SPQuery query = new SPQuery();
            query.Query = "<Where><Eq><FieldRef Name='ID' /><Value Type='Text'>" + id + "</Value></Eq></Where>";

            SPListItemCollection items = requirementsList.GetItems(query);

            if (items != null && items.Count > 0)
            {
                if (items[0]["Name"] != null && items[0]["Name"].ToString() != string.Empty)
                {
                    return items[0]["Name"].ToString();
                }
                else
                {
                    if (items[0]["Category"] != null)
                    {
                        return items[0]["Category"].ToString();
                    }
                }
            }
            return "";
        }
    }
}
