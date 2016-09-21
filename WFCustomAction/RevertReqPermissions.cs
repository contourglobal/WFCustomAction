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
    public class RevertReqPermissions
    {
        //Not used
        private string result = string.Empty;

        public Hashtable RevertPermissions(SPUserCodeWorkflowContext context, string fromId, string toId)
        {
            Hashtable results = new Hashtable();
            string result = string.Empty;
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        int fId;
                        int tId;
                        if (int.TryParse(fromId, out fId) && int.TryParse(toId, out tId))
                        {
                            SPList requirementsList = web.Lists["Requirements"];

                            if (requirementsList != null)
                            {
                                for (int i = fId; i <= tId; i++)
                                {
                                    try
                                    {
                                        SPListItem item = requirementsList.GetItemById(i);

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

                                                AddPermissions(item, web.Groups.GetByName("CG Debts Corporate Level"), web.RoleDefinitions.GetByType(SPRoleType.Contributor));
                                                AddPermissions(item, web.Groups.GetByName("CG Debts Africa Admins"), web.RoleDefinitions.GetByType(SPRoleType.Contributor));
                                                AddPermissions(item, web.Groups.GetByName("CG Debts Europe Admins"), web.RoleDefinitions.GetByType(SPRoleType.Contributor));
                                                AddPermissions(item, web.Groups.GetByName("CG Debts Latam Admins"), web.RoleDefinitions.GetByType(SPRoleType.Contributor));
                                                AddPermissions(item, web.Groups.GetByName("CG Debts Members"), web.RoleDefinitions.GetByType(SPRoleType.Contributor));
                                                AddPermissions(item, web.Groups.GetByName("CG Debts NAM Admins"), web.RoleDefinitions.GetByType(SPRoleType.Contributor));
                                                AddPermissions(item, web.Groups.GetByName("CG Debts Solutions Admins"), web.RoleDefinitions.GetByType(SPRoleType.Contributor));
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
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

        private void AddPermissions(SPListItem item, SPPrincipal principal, SPRoleDefinition roleDefinition)
        {
            SPRoleAssignment ra = new SPRoleAssignment(principal);
            ra.RoleDefinitionBindings.Add(roleDefinition);
            item.RoleAssignments.Add(ra);
        }
    }
}
