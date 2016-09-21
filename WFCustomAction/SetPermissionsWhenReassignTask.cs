using Microsoft.SharePoint;
using Microsoft.SharePoint.UserCode;
using Microsoft.SharePoint.Utilities;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace WFCustomAction
{
    public class SetPermissionsWhenReassignTask
    {
        string result = string.Empty;
        bool isUserAdded;

        public Hashtable SetItemPermissionsWhenReassignTask(SPUserCodeWorkflowContext context, string id, string sourceList, string assignedTo)
        {
            Hashtable results = new Hashtable();
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        SPList source = web.Lists[sourceList];
                        if (source != null)
                        {
                            if (!string.IsNullOrEmpty(assignedTo))
                            {
                                int itemId;
                                string username = string.Empty;

                                if (int.TryParse(id, out itemId))
                                {
                                    SPListItem item = source.GetItemById(itemId);
                                    if (item != null)
                                    {
                                        isUserAdded = false;

                                        if (!item.HasUniqueRoleAssignments)
                                        {
                                            item.BreakRoleInheritance(true);
                                        }

                                        result += assignedTo;

                                        foreach (SPRoleAssignment assignment in item.RoleAssignments)
                                        {
                                            if (assignment.Member.LoginName == assignedTo && assignment.RoleDefinitionBindings.Contains(web.RoleDefinitions.GetByType(SPRoleType.Contributor)))
                                            {
                                                isUserAdded = true;
                                                result += "isUserAdded = true";
                                            }
                                        }
                                        SPPrincipal spPrincipal = GetPrincipal(site, assignedTo);
                                        result += "SPPrincipal: " + spPrincipal.Name;
                                        if (spPrincipal != null)
                                        {
                                            UpdateItemPermissions(web, item, spPrincipal);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                results = new Hashtable();
                result += e.ToString();
                results["success"] = false;
            }
            results["result"] = result;
            return results;
        }

        private SPPrincipal GetPrincipal(SPSite site, string name)
        {
            SPPrincipal spPrincipal = null;
            if (SPUtility.IsLoginValid(site, name))
            {
                spPrincipal = site.RootWeb.EnsureUser(name);
            }
            else
            {
                foreach (SPGroup group in site.RootWeb.SiteGroups)
                {
                    if (group.Name.ToUpper(CultureInfo.InvariantCulture) == name.ToUpper(CultureInfo.InvariantCulture))
                    {
                        spPrincipal = group;
                        break;
                    }
                }
            }
            return spPrincipal;
        }

        private void UpdateItemPermissions(SPWeb web, SPListItem item, SPPrincipal spPrincipal)
        {
            if (!isUserAdded)
            {
                result += "AddPermissions" + spPrincipal.LoginName;
                AddPermissions(item, spPrincipal, web.RoleDefinitions.GetByType(SPRoleType.Contributor));
            }
        }

        private void AddPermissions(SPListItem item, SPPrincipal principal, SPRoleDefinition roleDefinition)
        {
            SPRoleAssignment ra = new SPRoleAssignment(principal);
            ra.RoleDefinitionBindings.Add(roleDefinition);
            item.RoleAssignments.Add(ra);
            result += "AddedPer";
        }



        //InfoPath related
        //public Hashtable SetItemPermissionsWhenReassignTask(SPUserCodeWorkflowContext context, string targetList, string fileUrl)
        //{
        //    Hashtable results = new Hashtable();
        //    try
        //    {
        //        using (SPSite site = new SPSite(context.CurrentWebUrl))
        //        {
        //            using (SPWeb web = site.OpenWeb())
        //            {
        //                SPList target = web.Lists[targetList];
        //                if (target != null)
        //                {
        //                    string assignedTo = string.Empty;
        //                    string workflowLink = string.Empty;

        //                    GetDataFromFile(fileUrl, web, ref assignedTo, ref workflowLink);
        //                    result += "WorkflowLink: " + workflowLink + ". AssignedTo: " + assignedTo;

        //                    if (!string.IsNullOrEmpty(assignedTo) && !string.IsNullOrEmpty(workflowLink))
        //                    {
        //                        int itemId;
        //                        string itemIdString = GetItemIdFromLink(workflowLink);
        //                        string username = string.Empty;

        //                        if (int.TryParse(itemIdString, out itemId))
        //                        {
        //                            SPListItem item = target.GetItemById(itemId);
        //                            if (item != null)
        //                            {
        //                                isUserAdded = false;

        //                                foreach (SPRoleAssignment assignment in item.RoleAssignments)
        //                                {
        //                                    if (assignment.Member.LoginName == assignedTo && assignment.RoleDefinitionBindings.Contains(web.RoleDefinitions.GetByType(SPRoleType.Contributor)))
        //                                    {
        //                                        isUserAdded = true;
        //                                    }
        //                                }
        //                                SPPrincipal spPrincipal = GetPrincipal(site, assignedTo);
        //                                result += "SPPrincipal: " + spPrincipal.Name;
        //                                if (spPrincipal != null)
        //                                {
        //                                    UpdateItemPermissions(web, item, spPrincipal);
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        results = new Hashtable();
        //        result += e.ToString();
        //        results["success"] = false;
        //    }
        //    results["result"] = result;
        //    return results;
        //}

        //private void GetDataFromFile(string fileUrl, SPWeb web, ref string assignedTo, ref string workflowLink)
        //{
        //    SPFile file = web.GetFile(fileUrl);

        //    byte[] bytes = file.OpenBinary();
        //    Stream ms = new MemoryStream(bytes);
        //    XmlDocument xmlDocument = new XmlDocument();
        //    xmlDocument.Load(ms);

        //    XmlNamespaceManager ns = new XmlNamespaceManager(xmlDocument.NameTable);
        //    ns.AddNamespace("dfs", "http://schemas.microsoft.com/office/infopath/2003/dataFormSolution");
        //    ns.AddNamespace("d", "http://schemas.microsoft.com/office/infopath/2009/WSSList/dataFields");
        //    ns.AddNamespace("pc", "http://schemas.microsoft.com/office/infopath/2007/PartnerControls");

        //    workflowLink = xmlDocument.SelectSingleNode("/dfs:myFields/dfs:dataFields/d:SharePointListItem_RW/d:WorkflowLink", ns).InnerText;
        //    assignedTo = xmlDocument.SelectSingleNode("/dfs:myFields/dfs:dataFields/d:SharePointListItem_RW/d:FieldName_DelegateTo/pc:Person/pc:AccountId", ns).InnerText;
        //}

        //private static string GetItemIdFromLink(string workflowLink)
        //{
        //    string[] workflowLinkArr = workflowLink.Split('/');
        //    string[] workflowLinkLastPart = workflowLinkArr.Last().Split('=');
        //    string itemIdString = workflowLinkLastPart.Last();
        //    return itemIdString;
        //}
    }
}
