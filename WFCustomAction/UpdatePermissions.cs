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
    public class UpdatePermissions
    {
        public Hashtable UpdateChildrenPermissions(SPUserCodeWorkflowContext context, string id, string sourceList, string targetList, bool isDev)
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

        private Dictionary<string, UserValueDev> managersDev = new Dictionary<string, UserValueDev>();
        //private string secondApprover;
        //private bool isRegionGroupAddedDev;
        private bool isFinanceGroupAddedDev;
        private bool isCorporateGroupAddedDev;

        string resultDev = string.Empty;

        private Hashtable DevMethod(SPUserCodeWorkflowContext context, string id, string sourceList, string targetList)
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
                            //SPList target = web.Lists[targetList];

                            if (source != null/* && target != null*/)
                            {
                                SPListItem sourceItem = source.GetItemById(currentId);

                                if (sourceItem != null)
                                {
                                    //SPListItemCollection listItems = GetChildren(target, sourceItem);

                                    //commented out as not used
                                    //string region = string.Empty;
                                    //if (sourceItem["Region"] != null){
                                    //    region = sourceItem["Region"].ToString();
                                    //}

                                    string finance = string.Empty;
                                    if (sourceItem["Finance Organization"] != null)
                                    {
                                        finance = sourceItem["Finance Organization"].ToString();
                                    }

                                    LoadManagersDev(sourceItem);

                                    using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                    {
                                        #region Old

                                        //foreach (SPListItem listItem in listItems)
                                        //{
                                        //    LoadSecondApprover(sourceItem, listItem["Category"]);
                                        //    isRegionGroupAdded = false;
                                        //    isCorporateGroupAdded = false;

                                        //    if (!listItem.HasUniqueRoleAssignments)
                                        //    {
                                        //        listItem.BreakRoleInheritance(true);
                                        //    }

                                        //    foreach (SPRoleAssignment assignment in listItem.RoleAssignments)
                                        //    {
                                        //        bool shouldRemove = true;

                                        //        if (assignment.Member.LoginName == secondApprover && assignment.RoleDefinitionBindings.Contains(web.RoleDefinitions.GetByType(SPRoleType.Contributor)))
                                        //        {
                                        //            secondApprover = string.Empty;
                                        //            shouldRemove = false;
                                        //        }

                                        //        if (managers.ContainsKey(assignment.Member.LoginName) && assignment.RoleDefinitionBindings.Contains(web.RoleDefinitions.GetByType(SPRoleType.Contributor)))
                                        //        {
                                        //            managers[assignment.Member.LoginName] = true;
                                        //            shouldRemove = false;
                                        //        }

                                        //        if (assignment.Member.LoginName == "CG Debts " + region + " Admins" && assignment.RoleDefinitionBindings.Contains(web.RoleDefinitions.GetByType(SPRoleType.Contributor)))
                                        //        {
                                        //            isRegionGroupAdded = true;
                                        //            shouldRemove = false;
                                        //        }

                                        //        if (assignment.Member.LoginName == "CG Debts Corporate Level" && assignment.RoleDefinitionBindings.Contains(web.RoleDefinitions.GetByType(SPRoleType.Contributor)))
                                        //        {
                                        //            isCorporateGroupAdded = true;
                                        //            shouldRemove = false;
                                        //        }

                                        //        if (shouldRemove)
                                        //        {
                                        //            assignment.RoleDefinitionBindings.RemoveAll();
                                        //            assignment.Update();
                                        //        }
                                        //    }

                                        //    UpdateItemPermissions(web, listItem, region);
                                        //}
                                        #endregion

                                        isFinanceGroupAddedDev = false;
                                        isCorporateGroupAddedDev = false;

                                        if (!sourceItem.HasUniqueRoleAssignments)
                                        {
                                            sourceItem.BreakRoleInheritance(true);
                                        }

                                        foreach (SPRoleAssignment assignment in sourceItem.RoleAssignments)
                                        {
                                            bool shouldRemove = true;

                                            if (managersDev.ContainsKey(assignment.Member.LoginName) && assignment.RoleDefinitionBindings.Contains(web.RoleDefinitions.GetByType(SPRoleType.Contributor)))
                                            {
                                                managersDev[assignment.Member.LoginName].IsAdded = true;
                                                shouldRemove = false;
                                            }

                                            if (assignment.Member.LoginName == "CG Debts " + finance + " Admins" && assignment.RoleDefinitionBindings.Contains(web.RoleDefinitions.GetByType(SPRoleType.Contributor)))
                                            {
                                                isFinanceGroupAddedDev = true;
                                                shouldRemove = false;
                                            }

                                            if (assignment.Member.LoginName == "CG Debts Corporate Level" && assignment.RoleDefinitionBindings.Contains(web.RoleDefinitions.GetByType(SPRoleType.Contributor)))
                                            {
                                                isCorporateGroupAddedDev = true;
                                                shouldRemove = false;
                                            }

                                            if (shouldRemove)
                                            {
                                                assignment.RoleDefinitionBindings.RemoveAll();
                                                assignment.Update();
                                            }
                                        }

                                        UpdateItemPermissionsDev(web, sourceItem, finance);
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
                resultDev += e.ToString();
                results["success"] = false;
            }
            results["result"] = resultDev;
            return results;
        }

        private void UpdateItemPermissionsDev(SPWeb web, SPListItem item, string finance)
        {
            List<string> keys = new List<string>(managersDev.Keys);
            foreach (string key in keys)
            {
                if (managersDev[key].IsAdded)
                {
                    managersDev[key].IsAdded = false;
                }
                else
                {
                    AddPermissionsDev(item, managersDev[key].User, web.RoleDefinitions.GetByType(SPRoleType.Contributor));
                }
            }

            //if (secondApprover != string.Empty)
            //{
            //    AddPermissions(item, web.SiteUsers[secondApprover], web.RoleDefinitions.GetByType(SPRoleType.Contributor));
            //}

            if (finance != string.Empty && !isFinanceGroupAddedDev)
            {
                AddPermissionsDev(item, web.Groups.GetByName("CG Debts " + finance + " Admins"), web.RoleDefinitions.GetByType(SPRoleType.Contributor));
            }

            if (!isCorporateGroupAddedDev)
            {
                AddPermissionsDev(item, web.Groups.GetByName("CG Debts Corporate Level"), web.RoleDefinitions.GetByType(SPRoleType.Contributor));
            }
        }

        private void LoadManagersDev(SPListItem sourceItem)
        {
            managersDev.Clear();

            if (sourceItem["Responsible"] != null)
            {
                AddManagerDev(GetManagerDev(sourceItem, "Responsible"));
            }
            if (sourceItem["Responsible 2"] != null)
            {
                AddManagerDev(GetManagerDev(sourceItem, "Responsible 2"));
            }
            if (sourceItem["Person in charge"] != null)
            {
                AddManagerDev(GetManagerDev(sourceItem, "Person in charge"));
            }
            if (sourceItem["Legal"] != null)
            {
                AddManagerDev(GetManagerDev(sourceItem, "Legal"));
            }
            if (sourceItem["Insurance"] != null)
            {
                AddManagerDev(GetManagerDev(sourceItem, "Insurance"));
            }
            if (sourceItem["HS"] != null)
            {
                AddManagerDev(GetManagerDev(sourceItem, "HS"));
            }
            if (sourceItem["Environment"] != null)
            {
                AddManagerDev(GetManagerDev(sourceItem, "Environment"));
            }
            if (sourceItem["Tax"] != null)
            {
                AddManagerDev(GetManagerDev(sourceItem, "Tax"));
            }
            if (sourceItem["Construction"] != null)
            {
                AddManagerDev(GetManagerDev(sourceItem, "Construction"));
            }
            if (sourceItem["Operational"] != null)
            {
                AddManagerDev(GetManagerDev(sourceItem, "Operational"));
            }
            if (sourceItem["Compliance"] != null)
            {
                AddManagerDev(GetManagerDev(sourceItem, "Compliance"));
            }
            if (sourceItem["Controller"] != null)
            {
                AddManagerDev(GetManagerDev(sourceItem, "Controller"));
            }
            if (sourceItem["Approver"] != null)
            {
                AddManagerDev(GetManagerDev(sourceItem, "Approver"));
            }
        }

        private void AddManagerDev(List<SPUser> users)
        {
            foreach (SPUser user in users)
            {
                if (!managersDev.ContainsKey(user.LoginName))
                {
                    UserValueDev uv = new UserValueDev();
                    uv.User = user;
                    uv.IsAdded = false;
                    managersDev.Add(user.LoginName, uv);
                }
            }
        }

        #region Old

        //private void LoadSecondApprover(SPListItem sourceItem, object category)
        //{
        //    secondApprover = string.Empty;
        //    if (category != null)
        //    {
        //        switch (category.ToString())
        //        {
        //            case "Insurance":
        //            case "Legal":
        //                if (sourceItem["Legal"] != null)
        //                {
        //                    secondApprover = GetManager(sourceItem, "Legal");
        //                }
        //                break;
        //            case "Environnemental & societal":
        //            case "Health and Safety compliance":
        //                if (sourceItem["HS"] != null)
        //                {
        //                    secondApprover = GetManager(sourceItem, "HS");
        //                }
        //                break;
        //            case "Tax":
        //                if (sourceItem["Tax"] != null)
        //                {
        //                    secondApprover = GetManager(sourceItem, "Tax");
        //                }
        //                break;
        //            case "Construction":
        //                if (sourceItem["Operational"] != null)
        //                {
        //                    secondApprover = GetManager(sourceItem, "Operational");
        //                }
        //                break;
        //        }
        //    }
        //}

        #endregion

        private SPListItemCollection GetChildrenDev(SPList list, SPListItem sourceItem)
        {
            SPQuery query = new SPQuery();
            query.Query = "<Where><Eq><FieldRef Name='Project_x0020_Id' /><Value Type='Text'>" + sourceItem["ID"] + "</Value></Eq></Where>";

            return list.GetItems(query);
        }

        private List<SPUser> GetManagerDev(SPListItem sourceItem, String fieldName)
        {
            try
            {
                List<SPUser> allManagers = new List<SPUser>();

                if (fieldName != string.Empty)
                {
                    SPFieldUser field = sourceItem.Fields[fieldName] as SPFieldUser;
                    if (field != null && sourceItem[fieldName] != null)
                    {
                        SPFieldUserValue fieldValue = field.GetFieldValue(sourceItem[fieldName].ToString()) as SPFieldUserValue;
                        if (fieldValue != null)
                        {
                            allManagers.Add(fieldValue.User);
                        }
                        else
                        {
                            SPFieldUserValueCollection fieldValues = field.GetFieldValue(sourceItem[fieldName].ToString()) as SPFieldUserValueCollection;
                            foreach (SPFieldUserValue fv in fieldValues)
                            {
                                allManagers.Add(fv.User);
                            }
                        }
                    }
                    else
                    {
                        if (field == null) throw new Exception("GetManager: field is null ");
                    }
                }
                return allManagers;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void AddPermissionsDev(SPListItem item, SPPrincipal principal, SPRoleDefinition roleDefinition)
        {
            SPRoleAssignment ra = new SPRoleAssignment(principal);
            ra.RoleDefinitionBindings.Add(roleDefinition);
            item.RoleAssignments.Add(ra);
        }

        #endregion

        #region Production

        private Dictionary<string, UserValueProd> managersProd = new Dictionary<string, UserValueProd>();
        private bool isFinanceGroupAddedProd;
        private bool isCorporateGroupAddedProd;

        string resultProd = string.Empty;

        private Hashtable ProductionMethod(SPUserCodeWorkflowContext context, string id, string sourceList, string targetList)
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

                            if (source != null/* && target != null*/)
                            {
                                SPListItem sourceItem = source.GetItemById(currentId);

                                if (sourceItem != null)
                                {
                                    //string region = string.Empty;
                                    //if (sourceItem["Region"] != null)
                                    //{
                                    //    region = sourceItem["Region"].ToString();
                                    //}

                                    string finance = string.Empty;
                                    if (sourceItem["Finance Organization"] != null)
                                    {
                                        finance = sourceItem["Finance Organization"].ToString();
                                    }
                                    LoadManagersProd(sourceItem);

                                    using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                    {
                                        isFinanceGroupAddedProd = false;
                                        isCorporateGroupAddedProd = false;

                                        if (!sourceItem.HasUniqueRoleAssignments)
                                        {
                                            sourceItem.BreakRoleInheritance(true);
                                        }

                                        foreach (SPRoleAssignment assignment in sourceItem.RoleAssignments)
                                        {
                                            bool shouldRemove = true;

                                            if (managersProd.ContainsKey(assignment.Member.LoginName) && assignment.RoleDefinitionBindings.Contains(web.RoleDefinitions.GetByType(SPRoleType.Contributor)))
                                            {
                                                managersProd[assignment.Member.LoginName].IsAdded = true;
                                                shouldRemove = false;
                                            }

                                            if (assignment.Member.LoginName == "CG Debts " + finance + " Admins" && assignment.RoleDefinitionBindings.Contains(web.RoleDefinitions.GetByType(SPRoleType.Contributor)))
                                            {
                                                isFinanceGroupAddedProd = true;
                                                shouldRemove = false;
                                            }

                                            if (assignment.Member.LoginName == "CG Debts Corporate Level" && assignment.RoleDefinitionBindings.Contains(web.RoleDefinitions.GetByType(SPRoleType.Contributor)))
                                            {
                                                isCorporateGroupAddedProd = true;
                                                shouldRemove = false;
                                            }

                                            if (shouldRemove)
                                            {
                                                assignment.RoleDefinitionBindings.RemoveAll();
                                                assignment.Update();
                                            }
                                        }

                                        UpdateItemPermissionsProd(web, sourceItem, finance);
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
                resultProd += e.ToString();
                results["success"] = false;
            }
            results["result"] = resultProd;
            return results;
        }

        private void UpdateItemPermissionsProd(SPWeb web, SPListItem item, string finance)
        {
            List<string> keys = new List<string>(managersProd.Keys);
            foreach (string key in keys)
            {
                if (managersProd[key].IsAdded)
                {
                    managersProd[key].IsAdded = false;
                }
                else
                {
                    AddPermissionsProd(item, managersProd[key].User, web.RoleDefinitions.GetByType(SPRoleType.Contributor));
                }
            }

            if (finance != string.Empty && !isFinanceGroupAddedProd)
            {
                AddPermissionsProd(item, web.Groups.GetByName("CG Debts " + finance + " Admins"), web.RoleDefinitions.GetByType(SPRoleType.Contributor));
            }

            if (!isCorporateGroupAddedProd)
            {
                AddPermissionsProd(item, web.Groups.GetByName("CG Debts Corporate Level"), web.RoleDefinitions.GetByType(SPRoleType.Contributor));
            }
        }

        private void LoadManagersProd(SPListItem sourceItem)
        {
            managersProd.Clear();

            if (sourceItem["Responsible"] != null)
            {
                AddManagerProd(GetManagerProd(sourceItem, "Responsible"));
            }
            if (sourceItem["Responsible 2"] != null)
            {
                AddManagerProd(GetManagerProd(sourceItem, "Responsible 2"));
            }
            if (sourceItem["Person in charge"] != null)
            {
                AddManagerProd(GetManagerProd(sourceItem, "Person in charge"));
            }
            if (sourceItem["Legal"] != null)
            {
                AddManagerProd(GetManagerProd(sourceItem, "Legal"));
            }
            if (sourceItem["Insurance"] != null)
            {
                AddManagerProd(GetManagerProd(sourceItem, "Insurance"));
            }
            if (sourceItem["HS"] != null)
            {
                AddManagerProd(GetManagerProd(sourceItem, "HS"));
            }
            if (sourceItem["Environment"] != null)
            {
                AddManagerProd(GetManagerProd(sourceItem, "Environment"));
            }
            if (sourceItem["Tax"] != null)
            {
                AddManagerProd(GetManagerProd(sourceItem, "Tax"));
            }
            if (sourceItem["Construction"] != null)
            {
                AddManagerProd(GetManagerProd(sourceItem, "Construction"));
            }
            if (sourceItem["Operational"] != null)
            {
                AddManagerProd(GetManagerProd(sourceItem, "Operational"));
            }
            if (sourceItem["Compliance"] != null)
            {
                AddManagerProd(GetManagerProd(sourceItem, "Compliance"));
            }
            if (sourceItem["Controller"] != null)
            {
                AddManagerProd(GetManagerProd(sourceItem, "Controller"));
            }
            if (sourceItem["Approver"] != null)
            {
                AddManagerProd(GetManagerProd(sourceItem, "Approver"));
            }
        }

        private void AddManagerProd(List<SPUser> users)
        {
            foreach (SPUser user in users)
            {
                if (!managersProd.ContainsKey(user.LoginName))
                {
                    UserValueProd uv = new UserValueProd();
                    uv.User = user;
                    uv.IsAdded = false;
                    managersProd.Add(user.LoginName, uv);
                }
            }
        }

        private SPListItemCollection GetChildrenProd(SPList list, SPListItem sourceItem)
        {
            SPQuery query = new SPQuery();
            query.Query = "<Where><Eq><FieldRef Name='Project_x0020_Id' /><Value Type='Text'>" + sourceItem["ID"] + "</Value></Eq></Where>";

            return list.GetItems(query);
        }

        private List<SPUser> GetManagerProd(SPListItem sourceItem, String fieldName)
        {
            try
            {
                List<SPUser> allManagers = new List<SPUser>();

                if (fieldName != string.Empty)
                {
                    SPFieldUser field = sourceItem.Fields[fieldName] as SPFieldUser;
                    if (field != null && sourceItem[fieldName] != null)
                    {
                        SPFieldUserValue fieldValue = field.GetFieldValue(sourceItem[fieldName].ToString()) as SPFieldUserValue;
                        if (fieldValue != null)
                        {
                            allManagers.Add(fieldValue.User);
                        }
                        else
                        {
                            SPFieldUserValueCollection fieldValues = field.GetFieldValue(sourceItem[fieldName].ToString()) as SPFieldUserValueCollection;
                            foreach (SPFieldUserValue fv in fieldValues)
                            {
                                allManagers.Add(fv.User);
                            }
                        }
                    }
                    else
                    {
                        if (field == null) throw new Exception("GetManager: field is null ");
                    }
                }
                return allManagers;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void AddPermissionsProd(SPListItem item, SPPrincipal principal, SPRoleDefinition roleDefinition)
        {
            SPRoleAssignment ra = new SPRoleAssignment(principal);
            ra.RoleDefinitionBindings.Add(roleDefinition);
            item.RoleAssignments.Add(ra);
        }

        #endregion
    }

    public class UserValueDev
    {
        public SPUser User { get; set; }
        public bool IsAdded { get; set; }
    }

    public class UserValueProd
    {
        public SPUser User { get; set; }
        public bool IsAdded { get; set; }
    }
}
