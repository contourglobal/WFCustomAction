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
    public class RejectProcess
    {
        public Hashtable Reject(SPUserCodeWorkflowContext context, string id, string historyId, bool isDev)
        {
            if (isDev)
            {
                return DevMethod(context, id, historyId, isDev);
            }
            else
            {
                return ProductionMethod(context, id, historyId, isDev);
            }
        }

        #region Dev

        private Hashtable DevMethod(SPUserCodeWorkflowContext context, string id, string historyId, bool isDev)
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
                        int currentHistoryId;
                        if (int.TryParse(id, out currentId) && int.TryParse(historyId, out currentHistoryId))
                        {
                            SPList requirementsList = web.Lists["Requirements" + (isDev ? " Dev" : string.Empty)];
                            SPList historyList = web.Lists["CG Debts History" + (isDev ? " Dev" : string.Empty)];

                            if (requirementsList != null && historyList != null)
                            {
                                SPListItem sourceItem = requirementsList.GetItemById(currentId);
                                SPListItem historyItem = historyList.GetItemById(currentHistoryId);

                                if (sourceItem != null && historyItem != null && historyItem["Type of Due Date completed"] != null)
                                {
                                    using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                    {
                                        string typeDueDate = historyItem["Type of Due Date completed"].ToString();
                                        sourceItem[GetStatusFieldDev(typeDueDate) + " Status"] = string.Empty;
                                        sourceItem[GetStatusFieldDev(typeDueDate) + " Status Covenant"] = string.Empty;
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

        private string GetStatusFieldDev(string type)
        {
            switch (type)
            {
                case "Quarterly 1":
                    return "1st Quarter";
                case "Quarterly 2":
                    return "2nd Quarter";
                case "Quarterly 3":
                    return "3rd Quarter";
                case "Quarterly 4":
                    return "4th Quarter";
                case "Semi-Annual 1":
                    return "1st Semi-Annual";
                case "Semi-Annual 2":
                    return "2nd Semi-Annual";
                default:
                    return type;
            }
        }

        #endregion

        #region Production

        private Hashtable ProductionMethod(SPUserCodeWorkflowContext context, string id, string historyId, bool isDev)
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
                        int currentHistoryId;
                        if (int.TryParse(id, out currentId) && int.TryParse(historyId, out currentHistoryId))
                        {
                            SPList requirementsList = web.Lists["Requirements" + (isDev ? " Dev" : string.Empty)];
                            SPList historyList = web.Lists["CG Debts History" + (isDev ? " Dev" : string.Empty)];

                            if (requirementsList != null && historyList != null)
                            {
                                SPListItem sourceItem = requirementsList.GetItemById(currentId);
                                SPListItem historyItem = historyList.GetItemById(currentHistoryId);

                                if (sourceItem != null && historyItem != null && historyItem["Type of Due Date completed"] != null)
                                {
                                    //using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                    //{
                                        string typeDueDate = historyItem["Type of Due Date completed"].ToString();
                                        sourceItem[GetStatusFieldProd(typeDueDate) + " Status"] = string.Empty;
                                        sourceItem[GetStatusFieldProd(typeDueDate) + " Status Covenant"] = string.Empty;
                                        sourceItem.Update();
                                        results["result"] = "RejUpd - " + GetStatusFieldProd(typeDueDate) + " Status";
                                    //}
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

        private string GetStatusFieldProd(string type)
        {
            switch (type)
            {
                case "Quarterly 1":
                    return "1st Quarter";
                case "Quarterly 2":
                    return "2nd Quarter";
                case "Quarterly 3":
                    return "3rd Quarter";
                case "Quarterly 4":
                    return "4th Quarter";
                case "Semi-Annual 1":
                    return "1st Semi-Annual";
                case "Semi-Annual 2":
                    return "2nd Semi-Annual";
                default:
                    return type;
            }
        }

        #endregion
    }
}
