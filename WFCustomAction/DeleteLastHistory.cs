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
    public class DeleteLastHistory
    {
        //Not used
        public Hashtable Delete(SPUserCodeWorkflowContext context, string id, string sourceList, string targetList)
        {
            Hashtable results = new Hashtable();
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
                                    Dictionary<int, DateTime> dict = new Dictionary<int, DateTime>();
                                    foreach (SPListItem item in target.Items)
                                    {
                                        if (item["Project Id"] != null && sourceItem["ID"] != null && item["Project Id"].ToString() == sourceItem["ID"].ToString())
                                        {
                                            dict.Add((int)item["ID"], (DateTime)item["Created"]);
                                        }
                                    }

                                    if (dict.Count > 0)
                                    {
                                        DateTime max = dict.Values.Max();
                                        int lastTargetId = dict.Where(pair => max.Equals(pair.Value)).Select(pair => pair.Key).FirstOrDefault();
                                        SPListItem targetItem = target.GetItemById(lastTargetId);

                                        if (targetItem != null && sourceItem["Reporting frequency"] != null)
                                        {
                                            switch (sourceItem["Reporting frequency"].ToString())
                                            {
                                                case "Annual":
                                                    sourceItem["Next Due Date"] = ((DateTime)sourceItem["Next Due Date"]).AddYears(-1);
                                                    break;
                                                case "Bi-annual":
                                                    sourceItem["Next Due Date"] = ((DateTime)sourceItem["Next Due Date"]).AddMonths(-6);
                                                    break;
                                                case "Quarterly":
                                                    sourceItem["Next Due Date"] = ((DateTime)sourceItem["Next Due Date"]).AddMonths(-3);
                                                    break;
                                            }

                                            using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                            {
                                                sourceItem.Update();
                                            }
                                            targetItem.Delete();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                results["success"] = true;
                results["exception"] = string.Empty;
            }
            catch (Exception e)
            {
                results = new Hashtable();
                results["exception"] = e.ToString();
                results["success"] = false;
            }

            return results;
        }
    }
}
