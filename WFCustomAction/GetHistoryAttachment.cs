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
    public class GetHistoryAttachment
    {
        public Hashtable GetHistoryAttachmentName(SPUserCodeWorkflowContext context, string id, bool isDev)
        {
            if (isDev)
            {
                return DevMethod(context, id, isDev);
            }
            else
            {
                return ProductionMethod(context, id, isDev);
            }
        }

        #region Dev

        private Hashtable DevMethod(SPUserCodeWorkflowContext context, string id, bool isDev)
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
                            SPList historyList = web.Lists["CG Debts History" + (isDev ? " Dev" : string.Empty)];

                            if (historyList != null)
                            {
                                SPListItem historyItem = historyList.GetItemById(currentId);

                                if (historyItem != null)
                                {
                                    string attachmentNames = string.Empty;
                                    foreach (string fileName in historyItem.Attachments)
                                    {
                                        attachmentNames += fileName + "/n";
                                    }
                                    if (attachmentNames == string.Empty) attachmentNames = "None";
                                    results["result"] = attachmentNames;
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

        #endregion

        #region Production

        private Hashtable ProductionMethod(SPUserCodeWorkflowContext context, string id, bool isDev)
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
                            SPList historyList = web.Lists["CG Debts History" + (isDev ? " Dev" : string.Empty)];

                            if (historyList != null)
                            {
                                SPListItem historyItem = historyList.GetItemById(currentId);

                                if (historyItem != null)
                                {
                                    string attachmentNames = string.Empty;
                                    foreach (string fileName in historyItem.Attachments)
                                    {
                                        attachmentNames += fileName + "/n";
                                    }
                                    if (attachmentNames == string.Empty) attachmentNames = "None";
                                    results["result"] = attachmentNames;
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

        #endregion
    }
}
