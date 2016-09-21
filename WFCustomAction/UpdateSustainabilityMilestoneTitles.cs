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
    public class UpdateMilestoneTitles
    {
        public Hashtable UpdateSustainabilityMilestoneTitles(SPUserCodeWorkflowContext context, string id, string sourceList, string targetList)
        {
            Hashtable results = new Hashtable();
            results["result"] = string.Empty;
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        int currentId;
                        if (int.TryParse(id, out currentId))
                        {
                            SPList source = web.Lists[sourceList];
                            SPList target = web.Lists[targetList];

                            if (source != null && target != null)
                            {
                                SPListItem sourceItem = source.GetItemById(currentId);

                                if (sourceItem != null && sourceItem["Proposed Project Name"] != null)
                                {
                                    SPListItemCollection items = GetMilestones(sourceItem["Proposed Project Name"], target);

                                    using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                    {
                                        foreach (SPListItem item in items)
                                        {
                                            item["Project Name"] = new SPFieldLookupValue(int.Parse(sourceItem["ID"].ToString()), sourceItem["Proposed Project Name"].ToString());
                                            item["Project Name Temp"] = string.Empty;
                                            item.Update();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

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

        private SPListItemCollection GetMilestones(object title, SPList targetList)
        {
            SPQuery query = new SPQuery();
            query.Query = "<Where><Eq><FieldRef Name='Project_x0020_Name_x0020_Temp' /><Value Type='Text'>" + title + "</Value></Eq></Where>";

            return targetList.GetItems(query);
        }
    }
}
