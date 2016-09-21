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
    //Not used
    public class UpdateManagers
    {
        public Hashtable UpdateManagersRecords(SPUserCodeWorkflowContext context, string id, string sourceList, string targetList, bool isDev)
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
                                SPListItem sourceItem = source.GetItemById(currentId);

                                if (sourceItem != null)
                                {
                                    SPListItemCollection listItems = null;

                                    listItems = GetChildrenDev(target, sourceItem);

                                    using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                    {
                                        foreach (SPListItem listItem in listItems)
                                        {
                                            listItem["Responsible"] = sourceItem["Responsible"];
                                            listItem["Responsible 2"] = sourceItem["Responsible 2"];
                                            listItem["Person in charge"] = sourceItem["Person in charge"];
                                            listItem["Legal"] = sourceItem["Legal"];
                                            listItem["Operational"] = sourceItem["Operational"];
                                            listItem["HS"] = sourceItem["HS"];
                                            listItem["Tax"] = sourceItem["Tax"];
                                            listItem["Approver"] = sourceItem["Approver"];
                                            listItem["Insurance"] = sourceItem["Insurance"];
                                            listItem["Compliance"] = sourceItem["Compliance"];
                                            listItem["Environment"] = sourceItem["Environment"];
                                            listItem["Construction"] = sourceItem["Construction"];
                                            listItem.Update();
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

        private SPListItemCollection GetChildrenDev(SPList list, SPListItem sourceItem)
        {
            SPQuery query = new SPQuery();
            query.Query = "<Where><Eq><FieldRef Name='Project_x0020_Id' /><Value Type='Text'>" + sourceItem["ID"] + "</Value></Eq></Where>";

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
                                SPListItem sourceItem = source.GetItemById(currentId);

                                if (sourceItem != null)
                                {
                                    SPListItemCollection listItems = null;

                                    listItems = GetChildrenProd(target, sourceItem);

                                    using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                    {
                                        foreach (SPListItem listItem in listItems)
                                        {
                                            listItem["Responsible"] = sourceItem["Responsible"];
                                            listItem["Responsible 2"] = sourceItem["Responsible 2"];
                                            listItem["Person in charge"] = sourceItem["Person in charge"];
                                            listItem["Legal"] = sourceItem["Legal"];
                                            listItem["Operational"] = sourceItem["Operational"];
                                            listItem["HS"] = sourceItem["HS"];
                                            listItem["Tax"] = sourceItem["Tax"];
                                            listItem["Approver"] = sourceItem["Approver"];
                                            listItem["Insurance"] = sourceItem["Insurance"];
                                            listItem["Compliance"] = sourceItem["Compliance"];
                                            listItem["Environment"] = sourceItem["Environment"];
                                            listItem["Construction"] = sourceItem["Construction"];
                                            listItem.Update();
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

        private SPListItemCollection GetChildrenProd(SPList list, SPListItem sourceItem)
        {
            SPQuery query = new SPQuery();
            query.Query = "<Where><Eq><FieldRef Name='Project_x0020_Id' /><Value Type='Text'>" + sourceItem["ID"] + "</Value></Eq></Where>";

            return list.GetItems(query);
        }

        #endregion
    }
}
