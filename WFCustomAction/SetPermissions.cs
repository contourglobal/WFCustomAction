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
    public class SetPermissions
    {
        //Not used
        public Hashtable SetItemPermissions(SPUserCodeWorkflowContext context, string id, string sourceList, string targetList)
        {
            Hashtable results = new Hashtable();
            string result = string.Empty;
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        int currentId;
                        if (int.TryParse(id, out currentId))
                        {
                            SPList target = web.Lists[targetList];
                            SPList source = web.Lists[sourceList];

                            if (target != null && source != null)
                            {
                                SPListItem targetItem = target.GetItemById(currentId);

                                if (targetItem != null)
                                {
                                    SPListItem listItem = GetParent(source, targetItem);

                                    //commented out as not used
                                    //string region = string.Empty;
                                    //if (listItem["Region"] != null)
                                    //{
                                    //    region = listItem["Region"].ToString();
                                    //}

                                    string finance = string.Empty;
                                    if (listItem["Finance Organization"] != null)
                                    {
                                        finance = listItem["Finance Organization"].ToString();
                                    }

                                    using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                    {
                                        if (!targetItem.HasUniqueRoleAssignments)
                                        {
                                            targetItem.BreakRoleInheritance(true);
                                        }

                                        foreach (SPRoleAssignment assignment in targetItem.RoleAssignments)
                                        {
                                            assignment.RoleDefinitionBindings.RemoveAll();
                                            assignment.Update();
                                        }

                                        if (targetItem["Created By"] != null)
                                        {
                                            GetSPUserObject(targetItem, "Created By", targetItem, web.RoleDefinitions.GetByType(SPRoleType.Contributor));
                                        }
                                        if (listItem["Responsible"] != null)
                                        {
                                            GetSPUserObject(listItem, "Responsible", targetItem, web.RoleDefinitions.GetByType(SPRoleType.Contributor));
                                        }
                                        if (listItem["Responsible 2"] != null)
                                        {
                                            GetSPUserObject(listItem, "Responsible 2", targetItem, web.RoleDefinitions.GetByType(SPRoleType.Contributor));
                                        }
                                        if (listItem["Person in charge"] != null)
                                        {
                                            GetSPUserObject(listItem, "Person in charge", targetItem, web.RoleDefinitions.GetByType(SPRoleType.Contributor));
                                        }
                                        if (listItem["Approver"] != null)
                                        {
                                            GetSPUserObject(listItem, "Approver", targetItem, web.RoleDefinitions.GetByType(SPRoleType.Contributor));
                                        }
                                        AddSecondApprover(listItem, targetItem, web.RoleDefinitions.GetByType(SPRoleType.Contributor));

                                        if (finance != string.Empty)
                                        {
                                            AddPermissions(targetItem, web.Groups.GetByName("CG Debts " + finance + " Admins"), web.RoleDefinitions.GetByType(SPRoleType.Contributor));
                                        }
                                        AddPermissions(targetItem, web.Groups.GetByName("CG Debts Corporate Level"), web.RoleDefinitions.GetByType(SPRoleType.Contributor));
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
                result += e.ToString();
                results["success"] = false;
            }
            results["result"] = result;
            return results;
        }

        private void AddSecondApprover(SPListItem listItem, SPListItem targetItem, SPRoleDefinition sPRoleDefinition)
        {
            if (targetItem["Category"] != null)
            {
                switch (targetItem["Category"].ToString())
                {
                    case "Insurance":
                    case "Legal":
                        if (listItem["Legal"] != null)
                        {
                            GetSPUserObject(listItem, "Legal", targetItem, sPRoleDefinition);
                        }
                        break;
                    case "Environnemental & societal":
                    case "Health and Safety compliance":
                        if (listItem["HS"] != null)
                        {
                            GetSPUserObject(listItem, "HS", targetItem, sPRoleDefinition);
                        }
                        break;
                    case "Tax":
                        if (listItem["Tax"] != null)
                        {
                            GetSPUserObject(listItem, "Tax", targetItem, sPRoleDefinition);
                        }
                        break;
                    case "Construction":
                        if (listItem["Operational"] != null)
                        {
                            GetSPUserObject(listItem, "Operational", targetItem, sPRoleDefinition);
                        }
                        break;
                }
            }
        }

        private SPListItem GetParent(SPList list, SPListItem targetItem)
        {
            SPQuery query = new SPQuery();
            query.Query = "<Where><Eq><FieldRef Name='ID' /><Value Type='Text'>" + targetItem["Project Id"] + "</Value></Eq></Where>";

            SPListItemCollection items = list.GetItems(query);

            return items.Count > 0 ? items[0] : null;
        }

        private void GetSPUserObject(SPListItem sourceItem, String fieldName, SPListItem listItem, SPRoleDefinition roleDefinition)
        {
            try
            {
                if (fieldName != string.Empty)
                {
                    SPFieldUser field = sourceItem.Fields[fieldName] as SPFieldUser;
                    if (field != null && sourceItem[fieldName] != null)
                    {
                        SPFieldUserValue fieldValue = field.GetFieldValue(sourceItem[fieldName].ToString()) as SPFieldUserValue;
                        if (fieldValue != null)
                        {
                            AddPermissions(listItem, fieldValue.User, roleDefinition);
                        }
                        else
                        {
                            SPFieldUserValueCollection fieldValues = field.GetFieldValue(sourceItem[fieldName].ToString()) as SPFieldUserValueCollection;
                            foreach (SPFieldUserValue fv in fieldValues)
                            {
                                AddPermissions(listItem, fv.User, roleDefinition);
                            }
                        }
                    }
                    else
                    {
                        if (field == null) throw new Exception("GetSPUserObject: field is null ");
                        if (sourceItem[fieldName] == null) throw new Exception("GetSPUserObject: spListItem[fieldName] is null ");
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void AddPermissions(SPListItem item, SPPrincipal principal, SPRoleDefinition roleDefinition)
        {
            SPRoleAssignment ra = new SPRoleAssignment(principal);
            ra.RoleDefinitionBindings.Add(roleDefinition);
            item.RoleAssignments.Add(ra);
        }
    }
}
