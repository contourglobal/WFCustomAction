using Microsoft.SharePoint.UserCode;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Microsoft.SharePoint;
using AppraisalForm.Utils;
using System.Globalization;
using Microsoft.SharePoint.Utilities;

namespace WFCustomAction
{
    public class GetAssistantEmailsOfSignatories
    {
        string res = string.Empty;
        string debug = string.Empty;
        bool isUserAdded = false;

        public Hashtable GetAssistantEmails(SPUserCodeWorkflowContext context, string signatories, string signatoriesEmails, string listName, string id, string sourceList)
        {
            Hashtable results = new Hashtable();
            results["result"] = string.Empty;
            //debug += string.Format("Signatories: {0} SignatoriesEmails: {1} ListName: {2} Id: {3} SourceList: {4}", signatories, signatoriesEmails, listName, id, sourceList);

            List<string> processedUsers = new List<string>();
            try
            {
                if (!string.IsNullOrEmpty(signatories) && !string.IsNullOrEmpty(signatoriesEmails))
                {
                    string[] arrSignatories = signatories.Split(new char[] { ';' });
                    string[] arrSignatoriesEmails = signatoriesEmails.Split(new char[] { ';' });
                    SPQuery camlQuery = new SPQuery();                    
                    camlQuery.Query = "<Where>" + ToCamlIn(arrSignatories, "Signatory", "User") + "</Where>";

                    SPListItemCollection items = SPListHelper.GetListItemsByQuery(listName, camlQuery, context.CurrentWebUrl);
                    /*
                    if (items == null)
                    {
                        debug += string.Format("List with name {0} not found", listName);
                    }
                    */ 
                    foreach (SPListItem item in items)
                    {
                        List<SPUser> users = GetSPUserObject(item, "Assistant");
                        foreach (SPUser user in users)
                        {
                            res += user.Email + ";";
                            SetPermissionsForAssistant(context, user.LoginName, id, sourceList);
                        }

                        List<SPUser> procUsers = GetSPUserObject(item, "Signatory");
                        foreach (SPUser processedUser in procUsers)
                        {
                            processedUsers.Add(processedUser.Email);
                        }
                    }

                    foreach (string signatoryEmail in arrSignatoriesEmails)
                    {
                        if (!processedUsers.Contains(signatoryEmail))
                        {
                            res += signatoryEmail + ";";
                        }
                    }

                    res = res.TrimEnd(new char[] { ';' });
                }
                results["success"] = true;
            }
            catch (Exception e)
            {
                results = new Hashtable();
                res += e.ToString();
                //debug += string.Format("Error: {0}", e.ToString());
                results["success"] = false;
            }

            //results["debug"] = debug;
            results["result"] = res;
            return results;
        }

        private void SetPermissionsForAssistant(SPUserCodeWorkflowContext context, string assignedTo, string id, string sourceList)
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

                                    foreach (SPRoleAssignment assignment in item.RoleAssignments)
                                    {
                                        if (assignment.Member.LoginName == assignedTo && assignment.RoleDefinitionBindings.Contains(web.RoleDefinitions.GetByType(SPRoleType.Contributor)))
                                        {
                                            isUserAdded = true;
                                            //res += "isUserAdded = true";
                                            //debug += "isUserAdded = true";
                                        }
                                    }
                                    SPPrincipal spPrincipal = GetPrincipal(site, assignedTo);
                                    //res += "SPPrincipal: " + spPrincipal.Name;
                                    //debug += "SPPrincipal: " + spPrincipal.Name;
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
                //res += "AddPermissions";
                //debug += "AddPermissions";
                AddPermissions(item, spPrincipal, web.RoleDefinitions.GetByType(SPRoleType.Contributor));
            }
        }

        private void AddPermissions(SPListItem item, SPPrincipal principal, SPRoleDefinition roleDefinition)
        {
            SPRoleAssignment ra = new SPRoleAssignment(principal);
            ra.RoleDefinitionBindings.Add(roleDefinition);
            item.RoleAssignments.Add(ra);
        }

        private List<SPUser> GetSPUserObject(SPListItem spListItem, String fieldName)
        {
            List<SPUser> spUser = new List<SPUser>();
            try
            {
                if (fieldName != string.Empty)
                {
                    SPFieldUser field = spListItem.Fields[fieldName] as SPFieldUser;
                    if (field != null && spListItem[fieldName] != null)
                    {
                        SPFieldUserValue fieldValue = field.GetFieldValue(spListItem[fieldName].ToString()) as SPFieldUserValue;
                        if (fieldValue != null)
                        {
                            spUser.Add(fieldValue.User);
                        }
                        else
                        {
                            SPFieldUserValueCollection fieldValues = field.GetFieldValue(spListItem[fieldName].ToString()) as SPFieldUserValueCollection;
                            foreach (SPFieldUserValue fv in fieldValues)
                            {
                                spUser.Add(fv.User);
                            }
                        }
                    }
                    else
                    {
                        if (field == null) throw new Exception("GetSPUserObject: field is null ");
                        if (spListItem[fieldName] == null) throw new Exception("GetSPUserObject: spListItem[fieldName] is null ");
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return spUser;
        }
        /* Not used
        private static string ToCamlIn(List<int> ids, string fieldName, string fieldType)
        {
            string template = string.Format(@"<In>
                                                <FieldRef Name='{1}' />
                                                <Values>
                                                    {0}
                                                </Values>
                                             </In>", string.Concat(ids.Select(id => string.Format("<Value Type='Number'>{0}</Value>", id))),
                                                   string.IsNullOrEmpty(fieldName) ? "ID" : fieldName,
                                                   string.IsNullOrEmpty(fieldType) ? "Lookup" : fieldType);
            return template;
        }*/

        public static string ToCamlIn(string[] ids, string fieldName, string valueType) 
        {
            string values = string.Empty;
            for (int i = 0; i < ids.Length; i++) 
            {
                values += "<Value Type='" + valueType + "'>" + ids[i].Trim() + "</Value>";
            }

            return "<In><FieldRef Name='" + fieldName + "' /><Values>" + values + "</Values></In>";
        }


    }


}
