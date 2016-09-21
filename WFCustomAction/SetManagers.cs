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
    public class SetManagers
    {
        public Hashtable SetManagersRecords(SPUserCodeWorkflowContext context, string id, string sourceList, string targetList, bool isDev)
        {
            if (isDev)
            {
                return DevMethod(context, id, sourceList, targetList);
            }
            else
            {
                return ProductionMethod(context, id, sourceList, targetList);
            }
        }

        #region Dev

        private Hashtable DevMethod(SPUserCodeWorkflowContext context, string id, string sourceList, string targetList)
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
                                SPListItem item = target.GetItemById(currentId);

                                if (item != null)
                                {
                                    SPListItemCollection listItems = GetParentDev(source, item);

                                    using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                    {
                                        if (listItems.Count > 0)
                                        {
                                            SPListItem listItem = listItems[0];

                                            item["Responsible"] = listItem["Responsible"];
                                            item["Responsible 2"] = listItem["Responsible 2"];
                                            item["Person in charge"] = listItem["Person in charge"];
                                            item["Legal"] = listItem["Legal"];
                                            item["Operational"] = listItem["Operational"];
                                            item["HS"] = listItem["HS"];
                                            item["Tax"] = listItem["Tax"];
                                            item["Approver"] = listItem["Approver"];
                                            item["Insurance"] = listItem["Insurance"];
                                            item["Compliance"] = listItem["Compliance"];
                                            item["Environment"] = listItem["Environment"];
                                            item["Construction"] = listItem["Construction"];
                                            item["Controller"] = listItem["Controller"];
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

        private SPListItemCollection GetParentDev(SPList list, SPListItem item)
        {
            SPQuery query = new SPQuery();
            query.Query = "<Where><Eq><FieldRef Name='ID' /><Value Type='Text'>" + item["Project Id"] + "</Value></Eq></Where>";

            return list.GetItems(query);
        }

        #endregion

        #region Production

        private Hashtable ProductionMethod(SPUserCodeWorkflowContext context, string id, string sourceList, string targetList)
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
                                SPListItem item = target.GetItemById(currentId);

                                if (item != null)
                                {
                                    SPListItemCollection listItems = GetParentProd(source, item);

                                    using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                    {
                                        if (listItems.Count > 0)
                                        {
                                            SPListItem listItem = listItems[0];

                                            item["Responsible"] = listItem["Responsible"];
                                            item["Responsible 2"] = listItem["Responsible 2"];
                                            item["Person in charge"] = listItem["Person in charge"];
                                            item["Legal"] = listItem["Legal"];
                                            item["Operational"] = listItem["Operational"];
                                            item["HS"] = listItem["HS"];
                                            item["Tax"] = listItem["Tax"];
                                            item["Approver"] = listItem["Approver"];
                                            item["Insurance"] = listItem["Insurance"];
                                            item["Compliance"] = listItem["Compliance"];
                                            item["Environment"] = listItem["Environment"];
                                            item["Construction"] = listItem["Construction"];
                                            item["Controller"] = listItem["Controller"];
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

        private SPListItemCollection GetParentProd(SPList list, SPListItem item)
        {
            SPQuery query = new SPQuery();
            query.Query = "<Where><Eq><FieldRef Name='ID' /><Value Type='Text'>" + item["Project Id"] + "</Value></Eq></Where>";

            return list.GetItems(query);
        }

        #endregion
    }
}
