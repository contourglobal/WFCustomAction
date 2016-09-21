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
    public class SetFullPermissionsQoBC
    {
        public Hashtable SetItemFullPermissionsQoBC(SPUserCodeWorkflowContext context, string id, string sourceList)
        {
            Hashtable results = new Hashtable();
            results["result"] = string.Empty;
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
                            SPList list = web.Lists[sourceList];
                            if (list != null)
                            {
                                SPListItem item = list.GetItemById(currentId);
                                if (item != null)
                                {
                                    using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                    {
                                        if (!item.HasUniqueRoleAssignments)
                                        {
                                            item.BreakRoleInheritance(true);
                                        }
                                        foreach (SPRoleAssignment assignment in item.RoleAssignments)
                                        {
                                            assignment.RoleDefinitionBindings.RemoveAll();
                                            assignment.Update();
                                        }
                                        
                                        SPRoleDefinition roleDefinition = web.RoleDefinitions["Full Control"];
                                        GetSPUserObject(item, "Created By", roleDefinition);
                                        SPGroup group = web.Groups["QoBCAdmins"];
                                        if (group != null)
                                        {
                                            AddPermissions(item, group, roleDefinition);
                                        }

                                        //SPRoleDefinition roleDefinitionContribute = web.RoleDefinitions["Contribute"];
                                        SPRoleDefinition roleDefinitionContribute = web.RoleDefinitions.GetByType(SPRoleType.Contributor);  // For Contribute
                                        SPGroup groupResponders = web.Groups["QoBCResponders"];
                                        if (groupResponders != null)
                                        {
                                            AddPermissions(item, groupResponders, roleDefinitionContribute);
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
                result += e.ToString();
                results["success"] = false;
            }
            results["result"] = result;

            return results;
        }

        private void GetSPUserObject(SPListItem item, String fieldName, SPRoleDefinition roleDefinition)
        {
            try
            {
                if (fieldName != string.Empty)
                {
                    SPFieldUser field = item.Fields[fieldName] as SPFieldUser;
                    if (field != null && item[fieldName] != null)
                    {
                        SPFieldUserValue fieldValue = field.GetFieldValue(item[fieldName].ToString()) as SPFieldUserValue;
                        if (fieldValue != null)
                        {
                            AddPermissions(item, fieldValue.User, roleDefinition);
                        }
                        else
                        {
                            SPFieldUserValueCollection fieldValues = field.GetFieldValue(item[fieldName].ToString()) as SPFieldUserValueCollection;
                            foreach (SPFieldUserValue fv in fieldValues)
                            {
                                AddPermissions(item, fv.User, roleDefinition);
                            }
                        }
                    }
                    else
                    {
                        if (field == null) throw new Exception("GetSPUserObject: field is null ");
                        if (item[fieldName] == null) throw new Exception("GetSPUserObject: spListItem[fieldName] is null ");
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
