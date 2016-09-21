using Microsoft.SharePoint;
using Microsoft.SharePoint.UserCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace WFCustomAction
{
    public class UpdateAllItems
    {
        public Hashtable UpdateItems(SPUserCodeWorkflowContext context, string tableName, string setField, string setValue, string whereField, string whereValue)
        {
            Hashtable results = new Hashtable();
            results["result"] = string.Empty;
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        SPList table = web.Lists[tableName];

                        if (table != null)
                        {
                            SPListItemCollection items = GetItems(table, whereField, whereValue);

                            using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                            {
                                foreach (SPListItem item in items)
                                {
                                    item[setField] = setValue;
                                    item.Update();
                                }
                            }
                        }
                    }

                    results["success"] = true;
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

        private SPListItemCollection GetItems(SPList table, string whereField, string whereValue)
        {
            SPQuery query = new SPQuery();
            query.Query = "<Where><Eq><FieldRef Name='" + whereField + "' /><Value Type='Text'>" + whereValue + "</Value></Eq></Where>";

            return table.GetItems(query);
        }
    }
}
