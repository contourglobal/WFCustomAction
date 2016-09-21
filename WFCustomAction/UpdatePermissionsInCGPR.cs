using Microsoft.SharePoint;
using Microsoft.SharePoint.UserCode;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WFCustomAction
{
    public class UpdatePermissionsInCGPR
    {
        Dictionary<string, UserValue> managers = new Dictionary<string, UserValue>();
        string result = string.Empty;
        string nameOfCorporateGroup = "CG PR - Corporate";
        string nameOfRegionalGroup = "CG PR - Regional";

        bool isRegionGroupAdded;
        bool isCorporateGroupAdded;


        public Hashtable UpdateChildrenPermissionsInCGPR(SPUserCodeWorkflowContext context, string id, string sourceList, string relatedList)
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
                            SPList list = web.Lists[sourceList];
                            SPList related = web.Lists[relatedList];
                            if (list != null && related != null)
                            {
                                SPListItem listItem = list.GetItemById(currentId);
                                if (listItem != null)
                                {
                                    SPListItemCollection relatedListItems = GetChildren(related, listItem);
                                    string region = string.Empty;

                                    if (listItem["Region"] != null)
                                    {
                                        region = listItem["Region"].ToString();
                                    }

                                    LoadManagers(listItem);

                                    using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                    {
                                        BreakInheritenceAndUpdateItemPermissions(web, region, listItem);

                                        foreach (SPListItem relatedListItem in relatedListItems)
                                        {
                                            BreakInheritenceAndUpdateItemPermissions(web, region, relatedListItem);
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

        private void BreakInheritenceAndUpdateItemPermissions(SPWeb web, string region, SPListItem currentListItem)
        {
            isRegionGroupAdded = false;
            isCorporateGroupAdded = false;

            if (!currentListItem.HasUniqueRoleAssignments)
            {
                currentListItem.BreakRoleInheritance(true);
            }
            foreach (SPRoleAssignment assignment in currentListItem.RoleAssignments)
            {
                bool toRemove = true;

                if (managers.ContainsKey(assignment.Member.LoginName) && assignment.RoleDefinitionBindings.Contains(web.RoleDefinitions.GetByType(SPRoleType.Contributor)))
                {
                    managers[assignment.Member.LoginName].IsAdded = true;
                    toRemove = false;
                }

                string fullNameOfRegionalGroup = string.Format("{0} {1}", nameOfRegionalGroup, region);
                if (assignment.Member.LoginName == fullNameOfRegionalGroup && assignment.RoleDefinitionBindings.Contains(web.RoleDefinitions.GetByType(SPRoleType.Contributor)))
                {
                    isRegionGroupAdded = true;
                    toRemove = false;
                }

                if (assignment.Member.LoginName == nameOfCorporateGroup && assignment.RoleDefinitionBindings.Contains(web.RoleDefinitions.GetByType(SPRoleType.Contributor)))
                {
                    isCorporateGroupAdded = true;
                    toRemove = false;
                }

                if (toRemove)
                {
                    assignment.RoleDefinitionBindings.RemoveAll();
                    assignment.Update();
                }

            }
            UpdateItemPermissions(web, currentListItem, region);
        }

        /// <summary>
        /// Gets related items from a parent list item through lookup.
        /// </summary>
        private SPListItemCollection GetChildren(SPList list, SPListItem sourceItem)
        {
            SPQuery query = new SPQuery();
            query.Query = "<Where><Eq><FieldRef Name='Project' /><Value Type='Text'>" + sourceItem["Title"] + "</Value></Eq></Where>";
            return list.GetItems(query);
        }

        private void AddPermissions(SPListItem item, SPPrincipal principal, SPRoleDefinition roleDefinition)
        {
            SPRoleAssignment ra = new SPRoleAssignment(principal);
            ra.RoleDefinitionBindings.Add(roleDefinition);
            item.RoleAssignments.Add(ra);
        }

        private void UpdateItemPermissions(SPWeb web, SPListItem item, string region)
        {
            List<string> keys = new List<string>(managers.Keys);
            foreach (string key in keys)
            {
                if (managers[key].IsAdded)
                {
                    managers[key].IsAdded = false;
                }
                else
                {
                    AddPermissions(item, managers[key].User, web.RoleDefinitions.GetByType(SPRoleType.Contributor));
                }
            }

            if (region != string.Empty && !isRegionGroupAdded)
            {
                string fullNameOfRegionalGroup = string.Format("{0} {1}", nameOfRegionalGroup, region);
                AddPermissions(item, web.Groups.GetByName(fullNameOfRegionalGroup), web.RoleDefinitions.GetByType(SPRoleType.Contributor));
            }

            if (!isCorporateGroupAdded)
            {
                AddPermissions(item, web.Groups.GetByName(nameOfCorporateGroup), web.RoleDefinitions.GetByType(SPRoleType.Contributor));
            }
        }

        private void LoadManagers(SPListItem item)
        {
            managers.Clear();

            if (item["Project Director"] != null)
            {
                List<SPUser> projectDirectors = GetManager(item, "Project Director");
                AddManager(projectDirectors);
            }
            if (item["Project Controller"] != null)
            {
                List<SPUser> projectControllers = GetManager(item, "Project Controller");
                AddManager(projectControllers);
            }
        }

        private void AddManager(List<SPUser> users)
        {
            foreach (SPUser user in users)
            {
                if (!managers.ContainsKey(user.LoginName))
                {
                    UserValue uv = new UserValue();
                    uv.User = user;
                    uv.IsAdded = false;
                    managers.Add(user.LoginName, uv);
                }
            }
        }


        private List<SPUser> GetManager(SPListItem item, String fieldName)
        {
            try
            {
                List<SPUser> allManagers = new List<SPUser>();

                if (fieldName != string.Empty)
                {
                    SPFieldUser field = item.Fields[fieldName] as SPFieldUser;
                    if (field != null && item[fieldName] != null)
                    {
                        SPFieldUserValue fieldValue = field.GetFieldValue(item[fieldName].ToString()) as SPFieldUserValue;
                        if (fieldValue != null)
                        {
                            allManagers.Add(fieldValue.User);
                        }
                        else
                        {
                            SPFieldUserValueCollection fieldValues = field.GetFieldValue(item[fieldName].ToString()) as SPFieldUserValueCollection;
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
    }

    public class UserValue
    {
        public SPUser User { get; set; }
        public bool IsAdded { get; set; }
    }

}
