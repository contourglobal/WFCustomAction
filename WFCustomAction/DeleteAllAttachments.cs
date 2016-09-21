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
    public class DeleteAllAttachments
    {
        public Hashtable DeleteAttachments(SPUserCodeWorkflowContext context, string id, string sourceList)
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

                            if (source != null)
                            {
                                SPListItem sourceItem = source.GetItemById(currentId);

                                if (sourceItem != null)
                                {
                                    for (int i = sourceItem.Attachments.Count; i > 0; i--)
                                    {
                                        sourceItem.Attachments.Delete(sourceItem.Attachments[i - 1]);
                                    }
                                    using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                    {
                                        sourceItem.Update();
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
    }
}
