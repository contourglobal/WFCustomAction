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
    public class MakeHistory
    {
        public Hashtable MakeHistoryRecord(SPUserCodeWorkflowContext context, string id, string sourceList, string targetList, bool isDev)
        {
            if (isDev)
            {
                return DevMethod(context, id, sourceList, targetList, isDev);
            }
            else
            {
                return ProductionMethod(context, id, sourceList, targetList, isDev);
            }
        }

        #region Dev

        private Hashtable DevMethod(SPUserCodeWorkflowContext context, string id, string sourceList, string targetList, bool isDev)
        {
            Hashtable results = new Hashtable();
            results["result"] = string.Empty;
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
                                    targetItem["Requirement Id"] = sourceItem["ID"];
                                    targetItem["Project"] = sourceItem["Project"];
                                    targetItem["Category"] = sourceItem["Category"];
                                    //targetItem["Requirement fulfilled?"] = sourceItem["Requirement fulfilled?"];
                                    targetItem["Requirement Compliance Status"] = sourceItem["Requirement Compliance Status"];

                                    if (sourceItem["Name"] != null)
                                    {
                                        targetItem["Requirement Name"] = sourceItem["Name"];
                                    }

                                    if (sourceItem["Completed Due Date"] != null && sourceItem["Completed Due Date"].ToString() != string.Empty)
                                    {

                                        targetItem["Notes on Reporting"] = sourceItem["Notes on Reporting"];
                                        targetItem["Type of Due Date completed"] = sourceItem["Completed Due Date"];
                                        targetItem["Reporting Due Date Closed"] = GetDueDateDev(sourceItem);

                                        string completionStatus = GetCompletionStatusDev(web, sourceItem, isDev);
                                        targetItem["Completion Status"] = completionStatus;
                                        //results["result"] = completionStatus;

                                        if (completionStatus == "Final" && sourceItem["Requirement Compliance Status"] != null &&
                                            (sourceItem["Requirement Compliance Status"].ToString() == "I checked the requirement, the actions described below shall be undertaken" ||
                                            sourceItem["Requirement Compliance Status"].ToString() == "I confirm the project complies with the requirement" ||
                                            sourceItem["Requirement Compliance Status"].ToString() == "I confirm the project obtained from the lender appropriate waivers, and now complies with the requirement" ||
                                            sourceItem["Requirement Compliance Status"].ToString() == "The project does not comply with the requirement, and actions described below should be undertaken"))
                                        {
                                            AddDraftAttachmentsDev(web, sourceItem, targetItem, isDev);
                                        }

                                        sourceItem[GetStatusFieldDev(sourceItem["Completed Due Date"].ToString()) + " Status"] = "InProgress";
                                        sourceItem[GetStatusFieldDev(sourceItem["Completed Due Date"].ToString()) + " Status Covenant"] = GetCovenantStatusDev(sourceItem["Requirement Compliance Status"]);
                                        sourceItem["Covenant Compliance Status"] = GetReqCovenantStatusDev(sourceItem);
                                        sourceItem["Completed Due Date"] = string.Empty;
                                        sourceItem["Notes on Reporting"] = string.Empty;
                                        sourceItem["Requirement Compliance Status"] = string.Empty;
                                        sourceItem.Update();
                                        GetLoanCovenantStatusDev(web, sourceItem, source, isDev);
                                    }

                                    targetItem.Update();
                                    results["result"] = targetItem["ID"];

                                    using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                    {
                                        //CheckStatus(sourceItem, target);
                                        //CheckLoanStatus(web, sourceItem);

                                        for (int i = sourceItem.Attachments.Count; i > 0; i--)
                                        {
                                            sourceItem.Attachments.Delete(sourceItem.Attachments[i - 1]);
                                        }
                                        sourceItem.Update();
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
                results["result"] = e.ToString();
                results["success"] = false;
            }

            return results;
        }

        private void AddDraftAttachmentsDev(SPWeb web, SPListItem sourceItem, SPListItem targetItem, bool isDev)
        {
            object dueDateObj = GetDueDateDev(sourceItem);
            if (dueDateObj != null)
            {
                DateTime dueDate = new DateTime();
                if (DateTime.TryParse(dueDateObj.ToString(), out dueDate))
                {
                    SPList historyList = web.Lists["CG Debts History" + (isDev ? " Dev" : string.Empty)];
                    SPQuery query = new SPQuery();
                    query.Query = "<Where><And><And><And><Eq><FieldRef Name='Project_x0020_Id' /><Value Type='Text'>" + sourceItem["ID"] + "</Value></Eq>" +
                                "<Eq><FieldRef Name='Reporting_x0020_Due_x0020_Date_x' /><Value Type='DateTime' IncludeTimeValue='FALSE'>" + dueDate.ToString("yyyy-MM-dd") + "</Value></Eq></And>" +
                                "<And><Eq><FieldRef Name='Category' /><Value Type='Text'>" + sourceItem["Category"] + "</Value></Eq>" +
                                "<Eq><FieldRef Name='Type_x0020_of_x0020_Due_x0020_Da' /><Value Type='Text'>" + sourceItem["Completed Due Date"] + "</Value></Eq></And></And>" +
                                "<Eq><FieldRef Name='Completion_x0020_Status' /><Value Type='Text'>Draft</Value></Eq></And></Where>";

                    SPListItemCollection items = historyList.GetItems(query);

                    if (items != null && items.Count > 0)
                    {
                        foreach (string fileName in items[0].Attachments)
                        {
                            SPFile file = items[0].ParentList.ParentWeb.GetFile(items[0].Attachments.UrlPrefix + fileName);
                            byte[] imageData = file.OpenBinary();
                            targetItem.Attachments.Add(fileName, imageData);
                        }
                    }
                }
            }
        }

        private object GetCovenantStatusDev(object status)
        {
            if (status != null && status.ToString() == "I checked the requirement, and confirm no action is needed")
            {
                return "Compliant";
            }
            return "Non-Compliant";
        }

        private string GetReqCovenantStatusDev(SPListItem item)
        {
            string typeOfDD = string.Empty;
            if (item["Type of Due Date"] != null)
            {
                typeOfDD = item["Type of Due Date"].ToString();
            }

            if (typeOfDD.Contains(";#Monthly;#") && item["January Status Covenant"] != null && item["January Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["February Status Covenant"] != null && item["February Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["March Status Covenant"] != null && item["March Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["April Status Covenant"] != null && item["April Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["May Status Covenant"] != null && item["May Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["June Status Covenant"] != null && item["June Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["July Status Covenant"] != null && item["July Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["August Status Covenant"] != null && item["August Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["September Status Covenant"] != null && item["September Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["October Status Covenant"] != null && item["October Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["November Status Covenant"] != null && item["November Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["December Status Covenant"] != null && item["December Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Quarterly;#") && item["1st Quarter Status Covenant"] != null && item["1st Quarter Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Quarterly;#") && item["2nd Quarter Status Covenant"] != null && item["2nd Quarter Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Quarterly;#") && item["3rd Quarter Status Covenant"] != null && item["3rd Quarter Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Quarterly;#") && item["4th Quarter Status Covenant"] != null && item["4th Quarter Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Semi-Annual;#") && item["1st Semi-Annual Status Covenant"] != null && item["1st Semi-Annual Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Semi-Annual;#") && item["2nd Semi-Annual Status Covenant"] != null && item["2nd Semi-Annual Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Annual;#") && item["Annual Status Covenant"] != null && item["Annual Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }

            return "Compliant";
        }

        private void GetLoanCovenantStatusDev(SPWeb web, SPListItem sourceItem, SPList requirementsList, bool isDev)
        {
            if (sourceItem["Project Id"] != null)
            {
                int projectId;
                if (int.TryParse(sourceItem["Project Id"].ToString(), out projectId))
                {
                    SPList source = web.Lists["Loan" + (isDev ? " Dev" : string.Empty)];
                    SPListItem loan = source.GetItemById(projectId);
                    if (loan != null)
                    {
                        string oldLoanStatus = loan["Covenant Compliance Status"] == null ? "" : loan["Covenant Compliance Status"].ToString();
                        string newLoanStatus = "Compliant";

                        SPQuery query = new SPQuery();
                        query.Query = "<Where><And><Eq><FieldRef Name='Project_x0020_Id' /><Value Type='Text'>" + projectId + "</Value></Eq>" +
                                    "<Eq><FieldRef Name='Covenant_x0020_Compliance_x0020_' /><Value Type='Text'>Non-Compliant</Value></Eq></And></Where>";

                        SPListItemCollection items = requirementsList.GetItems(query);

                        if (items != null && items.Count > 0)
                        {
                            newLoanStatus = "Non-Compliant";
                        }

                        if (oldLoanStatus != newLoanStatus)
                        {
                            loan["Covenant Compliance Status"] = newLoanStatus;
                            loan.Update();
                        }
                    }
                }
            }
        }

        private string GetStatusFieldDev(string type)
        {
            switch (type)
            {
                case "Quarterly 1":
                    return "1st Quarter";
                case "Quarterly 2":
                    return "2nd Quarter";
                case "Quarterly 3":
                    return "3rd Quarter";
                case "Quarterly 4":
                    return "4th Quarter";
                case "Semi-Annual 1":
                    return "1st Semi-Annual";
                case "Semi-Annual 2":
                    return "2nd Semi-Annual";
                default:
                    return type;
            }
        }

        private string GetCompletionStatusDev(SPWeb web, SPListItem sourceItem, bool isDev)
        {
            if (sourceItem["Requirement Compliance Status"] != null && (sourceItem["Requirement Compliance Status"].ToString() == "I checked the requirement, and confirm no action is needed" ||
                sourceItem["Requirement Compliance Status"].ToString() == "I checked the requirement, and confirm appropriate actions have been undertaken"))
            {
                return "Final";
            }

            object dueDateObj = GetDueDateDev(sourceItem);
            if (dueDateObj != null)
            {
                DateTime dueDate = new DateTime();
                if (DateTime.TryParse(dueDateObj.ToString(), out dueDate))
                {
                    SPList historyList = web.Lists["CG Debts History" + (isDev ? " Dev" : string.Empty)];
                    SPQuery query = new SPQuery();
                    query.Query = "<Where><And><And><And><Eq><FieldRef Name='Project_x0020_Id' /><Value Type='Text'>" + sourceItem["ID"] + "</Value></Eq>" +
                                "<Eq><FieldRef Name='Reporting_x0020_Due_x0020_Date_x' /><Value Type='DateTime' IncludeTimeValue='FALSE'>" + dueDate.ToString("yyyy-MM-dd") + "</Value></Eq></And>" +
                                "<And><Eq><FieldRef Name='Category' /><Value Type='Text'>" + sourceItem["Category"] + "</Value></Eq>" +
                                "<Eq><FieldRef Name='Type_x0020_of_x0020_Due_x0020_Da' /><Value Type='Text'>" + sourceItem["Completed Due Date"] + "</Value></Eq></And></And>" +
                                "<And><Eq><FieldRef Name='Completion_x0020_Status' /><Value Type='Text'>Draft</Value></Eq>" +
                                "<Neq><FieldRef Name='IsRejected' /><Value Type='Text'>True</Value></Neq></And></And></Where>";

                    SPListItemCollection items = historyList.GetItems(query);

                    if (items != null)
                    {
                        return items.Count > 0 ? "Final" : "Draft";
                    }
                }
            }
            return string.Empty;
        }

        private object GetDueDateDev(SPListItem sourceItem)
        {
            switch (sourceItem["Completed Due Date"].ToString())
            {
                case "Quarterly 1":
                    return sourceItem["1st Quarter"] ?? string.Empty;
                case "Quarterly 2":
                    return sourceItem["2nd Quarter"] ?? string.Empty;
                case "Quarterly 3":
                    return sourceItem["3rd Quarter"] ?? string.Empty;
                case "Quarterly 4":
                    return sourceItem["4th Quarter"] ?? string.Empty;
                case "Semi-Annual 1":
                    return sourceItem["1st Semi-Annual"] ?? string.Empty;
                case "Semi-Annual 2":
                    return sourceItem["2nd Semi-Annual"] ?? string.Empty;
                case "Annual":
                    return sourceItem["Annual"] ?? string.Empty;
                default:
                    return sourceItem[sourceItem["Completed Due Date"].ToString()] ?? string.Empty;
            }
        }

        #endregion

        #region Production

        private Hashtable ProductionMethod(SPUserCodeWorkflowContext context, string id, string sourceList, string targetList, bool isDev)
        {
            Hashtable results = new Hashtable();
            results["result"] = string.Empty;
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
                                    targetItem["Requirement Id"] = sourceItem["ID"];
                                    targetItem["Project"] = sourceItem["Project"];
                                    targetItem["Category"] = sourceItem["Category"];
                                    //targetItem["Requirement fulfilled?"] = sourceItem["Requirement fulfilled?"];
                                    targetItem["Requirement Compliance Status"] = sourceItem["Requirement Compliance Status"];

                                    if (sourceItem["Name"] != null)
                                    {
                                        targetItem["Requirement Name"] = sourceItem["Name"];
                                    }

                                    if (sourceItem["Completed Due Date"] != null && sourceItem["Completed Due Date"].ToString() != string.Empty)
                                    {
                                        targetItem["Notes on Reporting"] = sourceItem["Notes on Reporting"];
                                        targetItem["Type of Due Date completed"] = sourceItem["Completed Due Date"];
                                        targetItem["Reporting Due Date Closed"] = GetDueDateProd(sourceItem);

                                        string completionStatus = GetCompletionStatusProd(web, sourceItem, isDev);
                                        targetItem["Completion Status"] = completionStatus;
                                        //results["result"] = completionStatus;

                                        if (completionStatus == "Final" && sourceItem["Requirement Compliance Status"] != null &&
                                            (sourceItem["Requirement Compliance Status"].ToString() == "I checked the requirement, the actions described below shall be undertaken" ||
                                            sourceItem["Requirement Compliance Status"].ToString() == "I confirm the project complies with the requirement" ||
                                            sourceItem["Requirement Compliance Status"].ToString() == "I confirm the project obtained from the lender appropriate waivers, and now complies with the requirement" ||
                                            sourceItem["Requirement Compliance Status"].ToString() == "The project does not comply with the requirement, and actions described below should be undertaken"))
                                        {
                                            AddDraftAttachmentsProd(web, sourceItem, targetItem, isDev);
                                        }

                                        sourceItem[GetStatusFieldProd(sourceItem["Completed Due Date"].ToString()) + " Status"] = "InProgress";
                                        string covStatus = GetCovenantStatusProd(sourceItem["Requirement Compliance Status"]);
                                        sourceItem[GetStatusFieldProd(sourceItem["Completed Due Date"].ToString()) + " Status Covenant"] = covStatus;
                                        if (covStatus == "Non-Compliant")
                                        {
                                            sourceItem["Covenant Compliance Status"] = GetReqCovenantStatusProd(sourceItem);
                                        }
                                        sourceItem["Completed Due Date"] = string.Empty;
                                        sourceItem["Notes on Reporting"] = string.Empty;
                                        sourceItem["Requirement Compliance Status"] = string.Empty;
                                        sourceItem.Update();
                                        if (covStatus == "Non-Compliant")
                                        {
                                            GetLoanCovenantStatusProd(web, sourceItem, source, isDev);
                                        }
                                    }

                                    targetItem.Update();
                                    results["result"] = targetItem["ID"];

                                    using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                    {
                                        //CheckStatus(sourceItem, target);
                                        //CheckLoanStatus(web, sourceItem);

                                        for (int i = sourceItem.Attachments.Count; i > 0; i--)
                                        {
                                            sourceItem.Attachments.Delete(sourceItem.Attachments[i - 1]);
                                        }
                                        sourceItem.Update();
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
                results["result"] = e.ToString();
                results["success"] = false;
            }

            return results;
        }

        private void AddDraftAttachmentsProd(SPWeb web, SPListItem sourceItem, SPListItem targetItem, bool isDev)
        {
            object dueDateObj = GetDueDateProd(sourceItem);
            if (dueDateObj != null)
            {
                DateTime dueDate = new DateTime();
                if (DateTime.TryParse(dueDateObj.ToString(), out dueDate))
                {
                    SPList historyList = web.Lists["CG Debts History" + (isDev ? " Dev" : string.Empty)];
                    SPQuery query = new SPQuery();
                    query.Query = "<Where><And><And><And><Eq><FieldRef Name='Project_x0020_Id' /><Value Type='Text'>" + sourceItem["ID"] + "</Value></Eq>" +
                                "<Eq><FieldRef Name='Reporting_x0020_Due_x0020_Date_x' /><Value Type='DateTime' IncludeTimeValue='FALSE'>" + dueDate.ToString("yyyy-MM-dd") + "</Value></Eq></And>" +
                                "<And><Eq><FieldRef Name='Category' /><Value Type='Text'>" + sourceItem["Category"] + "</Value></Eq>" +
                                "<Eq><FieldRef Name='Type_x0020_of_x0020_Due_x0020_Da' /><Value Type='Text'>" + sourceItem["Completed Due Date"] + "</Value></Eq></And></And>" +
                                "<Eq><FieldRef Name='Completion_x0020_Status' /><Value Type='Text'>Draft</Value></Eq></And></Where>";

                    SPListItemCollection items = historyList.GetItems(query);

                    if (items != null && items.Count > 0)
                    {
                        foreach (string fileName in items[0].Attachments)
                        {
                            SPFile file = items[0].ParentList.ParentWeb.GetFile(items[0].Attachments.UrlPrefix + fileName);
                            byte[] imageData = file.OpenBinary();
                            targetItem.Attachments.Add(fileName, imageData);
                        }
                    }
                }
            }
        }

        private string GetCovenantStatusProd(object status)
        {
            if (status != null && (status.ToString() == "I checked the requirement, and confirm no action is needed" || status.ToString() == "I checked the requirement, and confirm appropriate actions have been undertaken"
                     || status.ToString() == "I confirm the project complies with the requirement" || status.ToString() == "I confirm the project obtained from the lender appropriate waivers, and now complies with the requirement"))
            {
                return "Compliant";
            }
            return "Non-Compliant";
        }

        private string GetReqCovenantStatusProd(SPListItem item)
        {
            string typeOfDD = string.Empty;
            if (item["Type of Due Date"] != null)
            {
                typeOfDD = item["Type of Due Date"].ToString();
            }

            if (typeOfDD.Contains(";#Monthly;#") && item["January Status Covenant"] != null && item["January Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["February Status Covenant"] != null && item["February Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["March Status Covenant"] != null && item["March Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["April Status Covenant"] != null && item["April Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["May Status Covenant"] != null && item["May Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["June Status Covenant"] != null && item["June Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["July Status Covenant"] != null && item["July Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["August Status Covenant"] != null && item["August Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["September Status Covenant"] != null && item["September Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["October Status Covenant"] != null && item["October Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["November Status Covenant"] != null && item["November Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Monthly;#") && item["December Status Covenant"] != null && item["December Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Quarterly;#") && item["1st Quarter Status Covenant"] != null && item["1st Quarter Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Quarterly;#") && item["2nd Quarter Status Covenant"] != null && item["2nd Quarter Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Quarterly;#") && item["3rd Quarter Status Covenant"] != null && item["3rd Quarter Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Quarterly;#") && item["4th Quarter Status Covenant"] != null && item["4th Quarter Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Semi-Annual;#") && item["1st Semi-Annual Status Covenant"] != null && item["1st Semi-Annual Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Semi-Annual;#") && item["2nd Semi-Annual Status Covenant"] != null && item["2nd Semi-Annual Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains(";#Annual;#") && item["Annual Status Covenant"] != null && item["Annual Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }

            return "Compliant";
        }

        private void GetLoanCovenantStatusProd(SPWeb web, SPListItem sourceItem, SPList requirementsList, bool isDev)
        {
            if (sourceItem["Project Id"] != null)
            {
                int projectId;
                if (int.TryParse(sourceItem["Project Id"].ToString(), out projectId))
                {
                    SPList source = web.Lists["Loan" + (isDev ? " Dev" : string.Empty)];
                    SPListItem loan = source.GetItemById(projectId);
                    if (loan != null)
                    {
                        string oldLoanStatus = loan["Covenant Compliance Status"] == null ? "" : loan["Covenant Compliance Status"].ToString();
                        string newLoanStatus = "Compliant";

                        SPQuery query = new SPQuery();
                        query.Query = "<Where><And><Eq><FieldRef Name='Project_x0020_Id' /><Value Type='Text'>" + projectId + "</Value></Eq>" +
                                    "<Eq><FieldRef Name='Covenant_x0020_Compliance_x0020_' /><Value Type='Text'>Non-Compliant</Value></Eq></And></Where>";

                        SPListItemCollection items = requirementsList.GetItems(query);

                        if (items != null && items.Count > 0)
                        {
                            newLoanStatus = "Non-Compliant";
                        }

                        if (oldLoanStatus != newLoanStatus)
                        {
                            loan["Covenant Compliance Status"] = newLoanStatus;
                            loan.Update();
                        }
                    }
                }
            }
        }

        private string GetStatusFieldProd(string type)
        {
            switch (type)
            {
                case "Quarterly 1":
                    return "1st Quarter";
                case "Quarterly 2":
                    return "2nd Quarter";
                case "Quarterly 3":
                    return "3rd Quarter";
                case "Quarterly 4":
                    return "4th Quarter";
                case "Semi-Annual 1":
                    return "1st Semi-Annual";
                case "Semi-Annual 2":
                    return "2nd Semi-Annual";
                default:
                    return type;
            }
        }

        private string GetCompletionStatusProd(SPWeb web, SPListItem sourceItem, bool isDev)
        {
            if (sourceItem["Requirement Compliance Status"] != null && (sourceItem["Requirement Compliance Status"].ToString() == "I checked the requirement, and confirm no action is needed" ||
                sourceItem["Requirement Compliance Status"].ToString() == "I checked the requirement, and confirm appropriate actions have been undertaken"))
            {
                return "Final";
            }

            object dueDateObj = GetDueDateProd(sourceItem);
            if (dueDateObj != null)
            {
                DateTime dueDate = new DateTime();
                if (DateTime.TryParse(dueDateObj.ToString(), out dueDate))
                {
                    SPList historyList = web.Lists["CG Debts History" + (isDev ? " Dev" : string.Empty)];
                    SPQuery query = new SPQuery();
                    query.Query = "<Where><And><And><And><Eq><FieldRef Name='Project_x0020_Id' /><Value Type='Text'>" + sourceItem["ID"] + "</Value></Eq>" +
                                "<Eq><FieldRef Name='Reporting_x0020_Due_x0020_Date_x' /><Value Type='DateTime' IncludeTimeValue='FALSE'>" + dueDate.ToString("yyyy-MM-dd") + "</Value></Eq></And>" +
                                "<And><Eq><FieldRef Name='Category' /><Value Type='Text'>" + sourceItem["Category"] + "</Value></Eq>" +
                                "<Eq><FieldRef Name='Type_x0020_of_x0020_Due_x0020_Da' /><Value Type='Text'>" + sourceItem["Completed Due Date"] + "</Value></Eq></And></And>" +
                                "<And><Eq><FieldRef Name='Completion_x0020_Status' /><Value Type='Text'>Draft</Value></Eq>" +
                                "<Neq><FieldRef Name='IsRejected' /><Value Type='Text'>True</Value></Neq></And></And></Where>";

                    SPListItemCollection items = historyList.GetItems(query);

                    if (items != null)
                    {
                        return items.Count > 0 ? "Final" : "Draft";
                    }
                }
            }
            return string.Empty;
        }

        private object GetDueDateProd(SPListItem sourceItem)
        {
            switch (sourceItem["Completed Due Date"].ToString())
            {
                case "Quarterly 1":
                    return sourceItem["1st Quarter"] ?? string.Empty;
                case "Quarterly 2":
                    return sourceItem["2nd Quarter"] ?? string.Empty;
                case "Quarterly 3":
                    return sourceItem["3rd Quarter"] ?? string.Empty;
                case "Quarterly 4":
                    return sourceItem["4th Quarter"] ?? string.Empty;
                case "Semi-Annual 1":
                    return sourceItem["1st Semi-Annual"] ?? string.Empty;
                case "Semi-Annual 2":
                    return sourceItem["2nd Semi-Annual"] ?? string.Empty;
                case "Annual":
                    return sourceItem["Annual"] ?? string.Empty;
                default:
                    return sourceItem[sourceItem["Completed Due Date"].ToString()] ?? string.Empty;
            }
        }

        #endregion
    }
}
