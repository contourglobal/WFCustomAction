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
    public class DeleteAllItems
    {
        public Hashtable DeleteAllItemsFromList(SPUserCodeWorkflowContext context, string listName)
        {
            Hashtable results = new Hashtable();
            results["result"] = string.Empty;
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        SPList list = web.Lists[listName];

                        if (list != null)
                        {
                            for (int i = list.Items.Count - 1; i >= 0; i--)
                            {
                                list.Items[i].Delete();
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
    }
}
