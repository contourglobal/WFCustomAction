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
    public class GetStatusById
    {
        public Hashtable GetHistoryStatusById(SPUserCodeWorkflowContext context, string id, bool isDev)
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
                            results["result"] = GetCompletionStatusDev(web, currentId, isDev);
                        }
                    }
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

        private string GetCompletionStatusDev(SPWeb web, int id, bool isDev)
        {
            SPList historyList = web.Lists["CG Debts History" + (isDev ? " Dev" : string.Empty)];
            SPQuery query = new SPQuery();
            query.Query = "<Where><Eq><FieldRef Name='ID' /><Value Type='Text'>" + id + "</Value></Eq></Where>";

            SPListItemCollection items = historyList.GetItems(query);

            if (items != null && items.Count > 0 && items[0]["Completion Status"] != null)
            {
                return items[0]["Completion Status"].ToString();
            }
            return "";
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
                            results["result"] = GetCompletionStatusProd(web, currentId, isDev);
                        }
                    }
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

        private string GetCompletionStatusProd(SPWeb web, int id, bool isDev)
        {
            SPList historyList = web.Lists["CG Debts History" + (isDev ? " Dev" : string.Empty)];
            SPQuery query = new SPQuery();
            query.Query = "<Where><Eq><FieldRef Name='ID' /><Value Type='Text'>" + id + "</Value></Eq></Where>";

            SPListItemCollection items = historyList.GetItems(query);

            if (items != null && items.Count > 0 && items[0]["Completion Status"] != null)
            {
                return items[0]["Completion Status"].ToString();
            }
            return "";
        }

        #endregion
    }
}
