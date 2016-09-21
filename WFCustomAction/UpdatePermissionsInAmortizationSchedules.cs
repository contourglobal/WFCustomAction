using Microsoft.SharePoint;
using Microsoft.SharePoint.UserCode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;

namespace WFCustomAction
{
    public class UpdatePermissionsInAmortizationSchedules
    {
        private Dictionary<string, UserVal> allManagers = new Dictionary<string, UserVal>();
        private string res = string.Empty;
        private string corporateGroup = "CG Debts Corporate Level";

        private bool hasFinanceGroupAdded;
        private bool hasCorporateGroupAdded;

        public Hashtable UpdateChildrenPermissionsInAmortizationSchedules(SPUserCodeWorkflowContext context, string id, string sourceList, string targetList)
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
                                SPListItem sourceListItem = source.GetItemById(currentId);
                                if (sourceListItem != null)
                                {
                                    string finance = string.Empty;
                                    string scheduleField = string.Empty;
                                    string fnFinance = "Finance Organization";
                                    string fnSchedule = "Amortization Schedule";

                                    if (sourceListItem[fnFinance] != null)
                                    {
                                        finance = sourceListItem[fnFinance].ToString();
                                    }

                                    if (sourceListItem[fnSchedule] != null)
                                    {
                                        scheduleField = sourceListItem[fnSchedule].ToString();
                                        string scheduleWebAddress = GetWebAddressFromUrlField(scheduleField);

                                        string documentName = GetDocumentName(scheduleWebAddress);
                                        if (!string.IsNullOrEmpty(documentName))
                                        {
                                            SPListItemCollection relatedListItems = GetChildren(target, documentName);

                                            res += "Document name is: " + documentName + ". ";
                                            res += "The count of found files is: " + relatedListItems.Count + ". ";

                                            if (relatedListItems.Count > 0)
                                            {
                                                LoadManagers(sourceListItem);

                                                using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                                {
                                                    foreach (SPListItem relatedListItem in relatedListItems)
                                                    {
                                                        BreakInheritenceAndUpdateItemPermissions(web, finance, relatedListItem);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                res += "Lists or one of them are null. ";
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                results = new Hashtable();
                res += e.ToString();
                results["success"] = false;
            }
            results["result"] = res;
            return results;
        }

        private string GetDocumentName(string scheduleWebAddress)
        {
            NameValueCollection nameValuesCollection = ParseQueryString(scheduleWebAddress ?? "");
            string fileParamValue = nameValuesCollection.Get("file");
            string documentName = string.Empty;

            if (!string.IsNullOrEmpty(fileParamValue))
            {
                documentName = System.Net.WebUtility.UrlDecode(fileParamValue);
            }
            else
            {
                string fileName = System.IO.Path.GetFileName(scheduleWebAddress);
                documentName = System.Net.WebUtility.UrlDecode(fileName);
            }
            return documentName;
        }

        private static string GetWebAddressFromUrlField(string scheduleField)
        {
            string[] webAddressScheduleArr = scheduleField.Split(new string[] { ", " }, StringSplitOptions.None);
            string scheduleWebAddress = webAddressScheduleArr.First();
            return scheduleWebAddress;
        }

        private void BreakInheritenceAndUpdateItemPermissions(SPWeb web, string finance, SPListItem currentListItem)
        {
            hasFinanceGroupAdded = false;
            hasCorporateGroupAdded = false;

            if (!currentListItem.HasUniqueRoleAssignments)
            {
                currentListItem.BreakRoleInheritance(true);
            }
            foreach (SPRoleAssignment assignment in currentListItem.RoleAssignments)
            {
                bool toRemove = true;

                if (allManagers.ContainsKey(assignment.Member.LoginName) && assignment.RoleDefinitionBindings.Contains(web.RoleDefinitions.GetByType(SPRoleType.Contributor)))
                {
                    allManagers[assignment.Member.LoginName].IsAdded = true;
                    toRemove = false;
                }

                if (assignment.Member.LoginName == "CG Debts " + finance + " Admins" && assignment.RoleDefinitionBindings.Contains(web.RoleDefinitions.GetByType(SPRoleType.Contributor)))
                {
                    hasFinanceGroupAdded = true;
                    toRemove = false;
                }

                if (assignment.Member.LoginName == corporateGroup && assignment.RoleDefinitionBindings.Contains(web.RoleDefinitions.GetByType(SPRoleType.Contributor)))
                {
                    hasCorporateGroupAdded = true;
                    toRemove = false;
                }

                if (toRemove)
                {
                    assignment.RoleDefinitionBindings.RemoveAll();
                    assignment.Update();
                }

            }
            UpdateItemPermissions(web, currentListItem, finance);
        }

        private void UpdateItemPermissions(SPWeb web, SPListItem item, string finance)
        {
            List<string> keys = new List<string>(allManagers.Keys);
            foreach (string key in keys)
            {
                if (allManagers[key].IsAdded)
                {
                    allManagers[key].IsAdded = false;
                }
                else
                {
                    AddPermissions(item, allManagers[key].User, web.RoleDefinitions.GetByType(SPRoleType.Contributor));
                }
            }

            if (finance != string.Empty && !hasFinanceGroupAdded)
            {
                AddPermissions(item, web.Groups.GetByName("CG Debts " + finance + " Admins"), web.RoleDefinitions.GetByType(SPRoleType.Contributor));
            }

            if (!hasCorporateGroupAdded)
            {
                AddPermissions(item, web.Groups.GetByName(corporateGroup), web.RoleDefinitions.GetByType(SPRoleType.Contributor));
            }
        }

        private SPListItemCollection GetChildren(SPList targetList, string documentTitle)
        {
            SPQuery query = new SPQuery();
            query.Query = "<Where><Eq><FieldRef Name='FileLeafRef' /><Value Type='File'>" + documentTitle + "</Value></Eq></Where>";
            return targetList.GetItems(query);
        }

        //private SPListItemCollection GetChildren2(SPList targetList, string s)
        //{
        //    SPQuery query = new SPQuery();
        //    //query.Query = "<Where><Eq><FieldRef Name='FileLeafRef' /><Value Type='File'>" + documentTitle + "</Value></Eq></Where>";
        //    //query.Query = "<Where><Eq><FieldRef Name='Title' /><Value Type='Text'>" + documentTitle + "</Value></Eq></Where>";
        //   // query.Query = "<Where><Eq><FieldRef Name='FileRef'/><Value Type='URL'>" + s + "</Value></Eq></Where>";
        //   // query.Query = "<Where><Eq><FieldRef Name='URL'/><Value Type='URL'>" + s + "</Value></Eq></Where>";
        //    return targetList.GetItems(query);
        //}

        private void AddPermissions(SPListItem item, SPPrincipal principal, SPRoleDefinition roleDefinition)
        {
            SPRoleAssignment ra = new SPRoleAssignment(principal);
            ra.RoleDefinitionBindings.Add(roleDefinition);
            item.RoleAssignments.Add(ra);
        }

        private void LoadManagers(SPListItem item)
        {
            allManagers.Clear();
            string responsible = "Responsible";
            string responsible2 = "Responsible 2";
            string personInCharge = "Person in charge";

            if (item[responsible] != null)
            {
                List<SPUser> responsiblesList = GetManager(item, responsible);
                AddManager(responsiblesList);
            }
            if (item[responsible2] != null)
            {
                List<SPUser> responsible2List = GetManager(item, responsible2);
                AddManager(responsible2List);
            }

            if (item[personInCharge] != null)
            {
                List<SPUser> personsInChargeList = GetManager(item, personInCharge);
                AddManager(personsInChargeList);
            }
        }

        private void AddManager(List<SPUser> users)
        {
            foreach (SPUser user in users)
            {
                if (!allManagers.ContainsKey(user.LoginName))
                {
                    UserVal uv = new UserVal();
                    uv.User = user;
                    uv.IsAdded = false;
                    allManagers.Add(user.LoginName, uv);
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


        public NameValueCollection ParseQueryString(string queryString)
        {
            NameValueCollection nameValueCollection = new NameValueCollection();
            if (queryString.Contains("?"))
            {
                queryString = queryString.Substring(queryString.IndexOf('?') + 1);
            }
            foreach (string vp in Regex.Split(queryString, "&"))
            {
                string[] singlePair = Regex.Split(vp, "=");
                if (singlePair.Length == 2)
                {
                    nameValueCollection.Add(singlePair[0], singlePair[1]);
                }
                else
                {
                    nameValueCollection.Add(singlePair[0], string.Empty);
                }
            }
            return nameValueCollection;
        }
    }

    public class UserVal
    {
        public SPUser User { get; set; }
        public bool IsAdded { get; set; }
    }
}
