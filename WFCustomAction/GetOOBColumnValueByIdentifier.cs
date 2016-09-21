using Microsoft.SharePoint;
using Microsoft.SharePoint.UserCode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppraisalForm.Utils;

namespace WFCustomAction
{
    public class GetOOBColumnValueByIdentifierAction
    {
        public Hashtable GetOOBColumnValueByIdentifier(SPUserCodeWorkflowContext context, int itemId, string columnIdentifier)
        {
            Hashtable results = new Hashtable();
            results["result"] = string.Empty;
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        if (itemId != 0 && columnIdentifier != string.Empty)
                        {
                            string internalName = OOBColumnIdentifier.GetColumnInternalNameByBreakdownType(columnIdentifier);
                            if (columnIdentifier.ToLower() == "impact")
                            {
                                internalName = OOBColumnIdentifier.GetImpactColumnInternalName();
                            }
                            results["result"] = GetOOBColumnValue(web, itemId, internalName);
                            results["internalName"] = internalName;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                results = new Hashtable();
                results["result"] = 0;
                results["internalName"] = string.Format("InternalName: {0}", e.ToString());
            }

            return results;
        }

        private double GetOOBColumnValue(SPWeb web, int itemId, string internalName)
        {
            SPList list = web.Lists["Sharepoint for Out of Budget expenses"];
            if (list != null)
            {
                
                SPQuery query = new SPQuery();
                query.Query = "<Where><Eq><FieldRef Name='ID'></FieldRef><Value Type='Counter'>" + itemId.ToString() + "</Value></Eq></Where>";

                SPListItemCollection items = list.GetItems(query);

                if (items != null && items.Count > 0)
                {

                    return (items[0][internalName] == null) ? 0 : Convert.ToDouble(items[0][internalName]);
                }
            }
            return 0;
        }
    }
}
