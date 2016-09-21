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
    public class MoveInsAttachments
    {
        public Hashtable MoveCurrentInsuranceAttachments(SPUserCodeWorkflowContext context, string id, string attType, string sourceList, string targetList, bool isDev)
        {
            if (isDev)
            {
                return DevMethod(context, id, attType, sourceList, targetList);
            }
            else
            {
                return ProductionMethod(context, id, attType, sourceList, targetList);
            }
        }

        #region Dev

        private Hashtable DevMethod(SPUserCodeWorkflowContext context, string id, string attType, string sourceList, string targetList)
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
                                    SPListItem targetItem = target.AddItem();

                                    foreach (string fileName in sourceItem.Attachments)
                                    {
                                        SPFile file = sourceItem.ParentList.ParentWeb.GetFile(sourceItem.Attachments.UrlPrefix + fileName);
                                        byte[] imageData = file.OpenBinary();
                                        targetItem.Attachments.Add(fileName, imageData);
                                    }

                                    targetItem["Title"] = sourceItem["Project"];
                                    targetItem["Project"] = sourceItem["Project"];
                                    targetItem["Source"] = sourceList;
                                    targetItem["Source Id"] = sourceItem["ID"];
                                    targetItem["Attachment Type"] = attType;

                                    targetItem.Update();

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

        #endregion

        #region Production

        private Hashtable ProductionMethod(SPUserCodeWorkflowContext context, string id, string attType, string sourceList, string targetList)
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
                                    SPListItem targetItem = target.AddItem();

                                    foreach (string fileName in sourceItem.Attachments)
                                    {
                                        SPFile file = sourceItem.ParentList.ParentWeb.GetFile(sourceItem.Attachments.UrlPrefix + fileName);
                                        byte[] imageData = file.OpenBinary();
                                        targetItem.Attachments.Add(fileName, imageData);
                                    }

                                    targetItem["Title"] = sourceItem["Project"];
                                    targetItem["Project"] = sourceItem["Project"];
                                    targetItem["Source"] = sourceList;
                                    targetItem["Source Id"] = sourceItem["ID"];
                                    targetItem["Attachment Type"] = attType;

                                    targetItem.Update();

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

        #endregion
    }
}
