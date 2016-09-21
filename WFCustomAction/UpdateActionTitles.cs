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
    public class UpdateTitles
    {
        public Hashtable UpdateActionTitles(SPUserCodeWorkflowContext context, string lookupField, string id, string title, string targetList)
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
                            SPList target = web.Lists[targetList];
                            if (target != null)
                            {
                                SPListItemCollection items = GetActions(title, target);

                                using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                {
                                    foreach (SPListItem item in items)
                                    {
                                        item[lookupField] = new SPFieldLookupValue(currentId, title);
                                        item["Temp Title"] = string.Empty;
                                        item.Update();
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

        private SPListItemCollection GetActions(string title, SPList targetList)
        {
            SPQuery query = new SPQuery();
            query.Query = "<Where><Eq><FieldRef Name='Temp_x0020_Title' /><Value Type='Text'>" + title + "</Value></Eq></Where>";
            query.ViewFields = string.Concat(
                                   "<FieldRef Name='Temp_x0020_Title' />",
                                   "<FieldRef Name='_x0035__x0020_Whys_x0020_Title' />");
            query.ViewFieldsOnly = true;

            return targetList.GetItems(query);
        }
    }
}
