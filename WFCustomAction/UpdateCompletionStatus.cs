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
    public class UpdateCompletionStatus
    {
        public Hashtable UpdateStatus(SPUserCodeWorkflowContext context, string id, string historyId, bool isDev)
        {
            if (isDev)
            {
                return DevMethod(context, id, historyId, isDev);
            }
            else
            {
                return ProductionMethod(context, id, historyId, isDev);
            }
        }

        #region Dev
        private bool hasWarningDev = false;
        private Hashtable DevMethod(SPUserCodeWorkflowContext context, string id, string historyId, bool isDev)
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
                        int currentHistoryId;
                        if (int.TryParse(id, out currentId) && int.TryParse(historyId, out currentHistoryId))
                        {
                            SPList loanList = web.Lists["Loan" + (isDev ? " Dev" : string.Empty)];
                            SPList requirementsList = web.Lists["Requirements" + (isDev ? " Dev" : string.Empty)];
                            SPList historyList = web.Lists["CG Debts History" + (isDev ? " Dev" : string.Empty)];

                            if (loanList != null && requirementsList != null && historyList != null)
                            {
                                SPListItem sourceItem = requirementsList.GetItemById(currentId);
                                SPListItem historyItem = historyList.GetItemById(currentHistoryId);

                                if (sourceItem != null && historyItem != null && historyItem["Type of Due Date completed"] != null)
                                {
                                    using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                    {
                                        string typeDueDate = historyItem["Type of Due Date completed"].ToString();
                                        sourceItem[GetStatusFieldDev(typeDueDate) + " Status"] = historyItem["Completion Status"];
                                        bool shouldUpdateLoanStatus = false;

                                        if (historyItem["Completion Status"] != null && historyItem["Completion Status"].ToString() == "Final")
                                        {
                                            DateTime oldDate;
                                            if (sourceItem[GetStatusFieldDev(typeDueDate)] != null && DateTime.TryParse((sourceItem[GetStatusFieldDev(typeDueDate)].ToString()), out oldDate))
                                            {
                                                sourceItem[GetStatusFieldDev(typeDueDate)] = oldDate.AddYears(1);
                                            }
                                            sourceItem[GetStatusFieldDev(typeDueDate) + " Status"] = string.Empty;

                                            sourceItem["Due Date Compliance Status"] = GetReqStatusDev(sourceItem);
                                            shouldUpdateLoanStatus = true;

                                            sourceItem[GetStatusFieldDev(typeDueDate) + " Status Covenant"] = "Compliant";
                                            historyItem["CovStatus"] = "Approved";
                                            historyItem.Update();
                                            UpdateDraftHistoryStatusDev(web, historyItem, isDev);
                                        }

                                        sourceItem["Covenant Compliance Status"] = GetReqCovenantStatusDev(sourceItem);
                                        sourceItem.Update();

                                        int projId;
                                        if (shouldUpdateLoanStatus && sourceItem["Project Id"] != null && int.TryParse(sourceItem["Project Id"].ToString(), out projId))
                                        {
                                            SPListItem loanItem = loanList.GetItemById(projId);
                                            if (loanItem != null)
                                            {
                                                shouldUpdateLoanStatus = false;
                                                string oldLoanStatus = loanItem["Due Date Compliance Status"] == null ? "" : loanItem["Due Date Compliance Status"].ToString();
                                                string newLoanStatus = GetLoanStatusDev(loanItem, requirementsList);
                                                if (oldLoanStatus != newLoanStatus)
                                                {
                                                    loanItem["Due Date Compliance Status"] = newLoanStatus;
                                                    shouldUpdateLoanStatus = true;
                                                }

                                                oldLoanStatus = loanItem["Covenant Compliance Status"] == null ? "" : loanItem["Covenant Compliance Status"].ToString();
                                                newLoanStatus = GetLoanCovenantStatusDev(loanItem, requirementsList);
                                                if (oldLoanStatus != newLoanStatus)
                                                {
                                                    loanItem["Covenant Compliance Status"] = newLoanStatus;
                                                    shouldUpdateLoanStatus = true;
                                                }

                                                if (shouldUpdateLoanStatus)
                                                {
                                                    loanItem.Update();
                                                }
                                            }
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
                results["result"] += e.ToString();
                results["success"] = false;
            }

            return results;
        }

        private void UpdateDraftHistoryStatusDev(SPWeb web, SPListItem historyItem, bool isDev)
        {
            DateTime dueDate = new DateTime();
            if (historyItem["Reporting Due Date Closed"] != null && DateTime.TryParse(historyItem["Reporting Due Date Closed"].ToString(), out dueDate))
            {
                SPList historyList = web.Lists["CG Debts History" + (isDev ? " Dev" : string.Empty)];
                SPQuery query = new SPQuery();
                query.Query = "<Where><And><And><And><Eq><FieldRef Name='Project_x0020_Id' /><Value Type='Text'>" + historyItem["Requirement Id"] + "</Value></Eq>" +
                            "<Eq><FieldRef Name='Reporting_x0020_Due_x0020_Date_x' /><Value Type='DateTime' IncludeTimeValue='FALSE'>" + dueDate.ToString("yyyy-MM-dd") + "</Value></Eq></And>" +
                            "<And><Eq><FieldRef Name='Category' /><Value Type='Text'>" + historyItem["Category"] + "</Value></Eq>" +
                            "<Eq><FieldRef Name='Type_x0020_of_x0020_Due_x0020_Da' /><Value Type='Text'>" + historyItem["Type of Due Date completed"] + "</Value></Eq></And></And>" +
                            "<Eq><FieldRef Name='Completion_x0020_Status' /><Value Type='Text'>Draft</Value></Eq></And></Where>";

                SPListItemCollection items = historyList.GetItems(query);

                if (items != null && items.Count > 0)
                {
                    SPListItem item = historyList.GetItemById(int.Parse(items[0]["ID"].ToString()));
                    item["CovStatus"] = "Approved";
                    item.Update();
                }
            }
        }

        private string GetReqStatusDev(SPListItem item)
        {
            hasWarningDev = false;

            string typeOfDD = string.Empty;
            if (item["Type of Due Date"] != null)
            {
                typeOfDD = item["Type of Due Date"].ToString();
            }

            if (typeOfDD.Contains("Monthly") && IsNonCompliantDev(item["January"], item["January Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantDev(item["February"], item["February Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantDev(item["March"], item["March Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantDev(item["April"], item["April Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantDev(item["May"], item["May Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantDev(item["June"], item["June Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantDev(item["July"], item["July Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantDev(item["August"], item["August Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantDev(item["September"], item["September Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantDev(item["October"], item["October Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantDev(item["November"], item["November Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantDev(item["December"], item["December Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Quarterly") && IsNonCompliantDev(item["1st Quarter"], item["1st Quarter Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Quarterly") && IsNonCompliantDev(item["2nd Quarter"], item["2nd Quarter Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Quarterly") && IsNonCompliantDev(item["3rd Quarter"], item["3rd Quarter Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Quarterly") && IsNonCompliantDev(item["4th Quarter"], item["4th Quarter Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Semi-Annual") && IsNonCompliantDev(item["1st Semi-Annual"], item["1st Semi-Annual Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Semi-Annual") && IsNonCompliantDev(item["2nd Semi-Annual"], item["2nd Semi-Annual Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Annual") && IsNonCompliantDev(item["Annual"], item["Annual Status"]))
            {
                return "Non-Compliant";
            }

            if (hasWarningDev)
            {
                return "Warning";
            }
            return "Compliant";
        }

        private bool IsNonCompliantDev(object dateObj, object status)
        {
            if (status != null && status.ToString() == "Final")
            {
                return false;
            }

            DateTime date;
            if (dateObj != null && DateTime.TryParse(dateObj.ToString(), out date))
            {
                if (DateTime.Now < date)
                {
                    if (!hasWarningDev && DateTime.Now.AddDays(14) > date)
                    {
                        hasWarningDev = true;
                    }
                    return false;
                }
                return true;
            }
            return false;
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

        private string GetLoanStatusDev(SPListItem item, SPList requirementsList)
        {
            SPQuery query = new SPQuery();
            query.Query = "<Where><And><Eq><FieldRef Name='Project_x0020_Id' /><Value Type='Text'>" + item["ID"] + "</Value></Eq>" +
                        "<Or><Eq><FieldRef Name='Requirements_x0020_Compliance_x0' /><Value Type='Text'>Non-Compliant</Value></Eq>" +
                        "<Eq><FieldRef Name='Requirements_x0020_Compliance_x0' /><Value Type='Text'>Warning</Value></Eq></Or></And></Where>";

            SPListItemCollection items = requirementsList.GetItems(query);

            if (items != null && items.Count > 0)
            {
                foreach (SPListItem reqItem in items)
                {
                    if (reqItem["Due Date Compliance Status"] != null && reqItem["Due Date Compliance Status"].ToString() == "Non-Compliant")
                    {
                        return "Non-Compliant";
                    }
                }
                return "Warning";
            }

            return "Compliant";
        }

        private string GetLoanCovenantStatusDev(SPListItem item, SPList requirementsList)
        {
            SPQuery query = new SPQuery();
            query.Query = "<Where><And><Eq><FieldRef Name='Project_x0020_Id' /><Value Type='Text'>" + item["ID"] + "</Value></Eq>" +
                        "<Eq><FieldRef Name='Covenant_x0020_Compliance_x0020_' /><Value Type='Text'>Non-Compliant</Value></Eq></And></Where>";

            SPListItemCollection items = requirementsList.GetItems(query);

            if (items != null && items.Count > 0)
            {
                return "Non-Compliant";
            }

            return "Compliant";
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

        #endregion

        #region Production

        private bool hasWarningProd = false;
        private bool isInCurePeriodProd = false;        
        private Hashtable ProductionMethod(SPUserCodeWorkflowContext context, string id, string historyId, bool isDev)
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
                        int currentHistoryId;
                        if (int.TryParse(id, out currentId) && int.TryParse(historyId, out currentHistoryId))
                        {
                            SPList loanList = web.Lists["Loan" + (isDev ? " Dev" : string.Empty)];
                            SPList requirementsList = web.Lists["Requirements" + (isDev ? " Dev" : string.Empty)];
                            SPList historyList = web.Lists["CG Debts History" + (isDev ? " Dev" : string.Empty)];

                            if (loanList != null && requirementsList != null && historyList != null)
                            {
                                SPListItem sourceItem = requirementsList.GetItemById(currentId);
                                SPListItem historyItem = historyList.GetItemById(currentHistoryId);

                                if (sourceItem != null && historyItem != null && historyItem["Type of Due Date completed"] != null)
                                {
                                    //using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                    //{
                                        string typeDueDate = historyItem["Type of Due Date completed"].ToString();
                                        sourceItem[GetStatusFieldProd(typeDueDate) + " Status"] = historyItem["Completion Status"];
                                        bool shouldUpdateLoanStatus = false;

                                        if (historyItem["Completion Status"] != null && historyItem["Completion Status"].ToString() == "Final")
                                        {
                                            DateTime oldDate;
                                            if (sourceItem[GetStatusFieldProd(typeDueDate)] != null && DateTime.TryParse((sourceItem[GetStatusFieldProd(typeDueDate)].ToString()), out oldDate))
                                            {
                                                sourceItem[GetStatusFieldProd(typeDueDate)] = oldDate.AddYears(1);
                                            }
                                            sourceItem[GetStatusFieldProd(typeDueDate) + " Status"] = string.Empty;

                                            sourceItem["Due Date Compliance Status"] = GetReqStatusProd(sourceItem);
                                            shouldUpdateLoanStatus = true;

                                            sourceItem[GetStatusFieldProd(typeDueDate) + " Status Covenant"] = "Compliant";
                                            historyItem["CovStatus"] = "Approved";
                                            historyItem.Update();
                                            UpdateDraftHistoryStatusProd(web, historyItem, isDev);
                                        }

                                        if (shouldUpdateLoanStatus)
                                        {
                                            GetIsCureProd(sourceItem);
                                        }
                                        sourceItem["Covenant Compliance Status"] = GetReqCovenantStatusProd(sourceItem);
                                        sourceItem.Update();
                                        results["result"] = "ReqUpd - " + GetStatusFieldProd(typeDueDate) + " Status - " + historyItem["Completion Status"];

                                        int projId;
                                        if (shouldUpdateLoanStatus && sourceItem["Project Id"] != null && int.TryParse(sourceItem["Project Id"].ToString(), out projId))
                                        {
                                            SPListItem loanItem = loanList.GetItemById(projId);
                                            if (loanItem != null)
                                            {
                                                shouldUpdateLoanStatus = false;
                                                string oldLoanStatus = loanItem["Due Date Compliance Status"] == null ? "" : loanItem["Due Date Compliance Status"].ToString();
                                                string newLoanStatus = GetLoanStatusProd(loanItem, requirementsList);
                                                if (oldLoanStatus != newLoanStatus)
                                                {
                                                    loanItem["Due Date Compliance Status"] = newLoanStatus;
                                                    shouldUpdateLoanStatus = true;
                                                }

                                                oldLoanStatus = loanItem["Covenant Compliance Status"] == null ? "" : loanItem["Covenant Compliance Status"].ToString();
                                                newLoanStatus = GetLoanCovenantStatusProd(loanItem, requirementsList);
                                                if (oldLoanStatus != newLoanStatus)
                                                {
                                                    loanItem["Covenant Compliance Status"] = newLoanStatus;
                                                    shouldUpdateLoanStatus = true;
                                                }

                                                oldLoanStatus = loanItem["IsInCurePeriod"] == null ? "" : loanItem["IsInCurePeriod"].ToString();
                                                newLoanStatus = GetLoanCureStatusProd(loanItem, requirementsList);
                                                if (oldLoanStatus != newLoanStatus)
                                                {
                                                    loanItem["IsInCurePeriod"] = newLoanStatus;
                                                    shouldUpdateLoanStatus = true;
                                                }

                                                if (shouldUpdateLoanStatus)
                                                {
                                                    loanItem.Update();
                                                }
                                            }
                                        }
                                    //}
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
                results["result"] += e.ToString();
                results["success"] = false;
            }

            return results;
        }

        private void UpdateDraftHistoryStatusProd(SPWeb web, SPListItem historyItem, bool isDev)
        {
            DateTime dueDate = new DateTime();
            if (historyItem["Reporting Due Date Closed"] != null && DateTime.TryParse(historyItem["Reporting Due Date Closed"].ToString(), out dueDate))
            {
                SPList historyList = web.Lists["CG Debts History" + (isDev ? " Dev" : string.Empty)];
                SPQuery query = new SPQuery();
                query.Query = "<Where><And><And><And><Eq><FieldRef Name='Project_x0020_Id' /><Value Type='Text'>" + historyItem["Requirement Id"] + "</Value></Eq>" +
                            "<Eq><FieldRef Name='Reporting_x0020_Due_x0020_Date_x' /><Value Type='DateTime' IncludeTimeValue='FALSE'>" + dueDate.ToString("yyyy-MM-dd") + "</Value></Eq></And>" +
                            "<And><Eq><FieldRef Name='Category' /><Value Type='Text'>" + historyItem["Category"] + "</Value></Eq>" +
                            "<Eq><FieldRef Name='Type_x0020_of_x0020_Due_x0020_Da' /><Value Type='Text'>" + historyItem["Type of Due Date completed"] + "</Value></Eq></And></And>" +
                            "<Eq><FieldRef Name='Completion_x0020_Status' /><Value Type='Text'>Draft</Value></Eq></And></Where>";

                SPListItemCollection items = historyList.GetItems(query);

                if (items != null && items.Count > 0)
                {
                    SPListItem item = historyList.GetItemById(int.Parse(items[0]["ID"].ToString()));
                    item["CovStatus"] = "Approved";
                    item.Update();
                }
            }
        }

        private void GetIsCureProd(SPListItem item)
        {
            item["IsInCurePeriod"] = "";
            int curePeriod = 0;

            if (item["Cure period"] != null)
            {
                if (!int.TryParse(item["Cure period"].ToString(), out curePeriod))
                {
                    curePeriod = 0;
                }
            }

            if (item["Cure period?"] != null && item["Cure period?"].ToString() == "Yes")
            {
                isInCurePeriodProd = false;

                string typeOfDD = string.Empty;
                if (item["Type of Due Date"] != null)
                {
                    typeOfDD = item["Type of Due Date"].ToString();
                }

                if (typeOfDD.Contains("Monthly") && IsAfterCureProd(item["January"], curePeriod, item["January Status"]))
                {
                    item["IsInCurePeriod"] = "After";
                    item["January Status Covenant"] = "Non-Compliant";
                    return;
                }
                else if (typeOfDD.Contains("Monthly") && IsAfterCureProd(item["February"], curePeriod, item["February Status"]))
                {
                    item["IsInCurePeriod"] = "After";
                    item["February Status Covenant"] = "Non-Compliant";
                    return;
                }
                else if (typeOfDD.Contains("Monthly") && IsAfterCureProd(item["March"], curePeriod, item["March Status"]))
                {
                    item["IsInCurePeriod"] = "After";
                    item["March Status Covenant"] = "Non-Compliant";
                    return;
                }
                else if (typeOfDD.Contains("Monthly") && IsAfterCureProd(item["April"], curePeriod, item["April Status"]))
                {
                    item["IsInCurePeriod"] = "After";
                    item["April Status Covenant"] = "Non-Compliant";
                    return;
                }
                else if (typeOfDD.Contains("Monthly") && IsAfterCureProd(item["May"], curePeriod, item["May Status"]))
                {
                    item["IsInCurePeriod"] = "After";
                    item["May Status Covenant"] = "Non-Compliant";
                    return;
                }
                else if (typeOfDD.Contains("Monthly") && IsAfterCureProd(item["June"], curePeriod, item["June Status"]))
                {
                    item["IsInCurePeriod"] = "After";
                    item["June Status Covenant"] = "Non-Compliant";
                    return;
                }
                else if (typeOfDD.Contains("Monthly") && IsAfterCureProd(item["July"], curePeriod, item["July Status"]))
                {
                    item["IsInCurePeriod"] = "After";
                    item["July Status Covenant"] = "Non-Compliant";
                    return;
                }
                else if (typeOfDD.Contains("Monthly") && IsAfterCureProd(item["August"], curePeriod, item["August Status"]))
                {
                    item["IsInCurePeriod"] = "After";
                    item["August Status Covenant"] = "Non-Compliant";
                    return;
                }
                else if (typeOfDD.Contains("Monthly") && IsAfterCureProd(item["September"], curePeriod, item["September Status"]))
                {
                    item["IsInCurePeriod"] = "After";
                    item["September Status Covenant"] = "Non-Compliant";
                    return;
                }
                else if (typeOfDD.Contains("Monthly") && IsAfterCureProd(item["October"], curePeriod, item["October Status"]))
                {
                    item["IsInCurePeriod"] = "After";
                    item["October Status Covenant"] = "Non-Compliant";
                    return;
                }
                else if (typeOfDD.Contains("Monthly") && IsAfterCureProd(item["November"], curePeriod, item["November Status"]))
                {
                    item["IsInCurePeriod"] = "After";
                    item["November Status Covenant"] = "Non-Compliant";
                    return;
                }
                else if (typeOfDD.Contains("Monthly") && IsAfterCureProd(item["December"], curePeriod, item["December Status"]))
                {
                    item["IsInCurePeriod"] = "After";
                    item["December Status Covenant"] = "Non-Compliant";
                    return;
                }
                else if (typeOfDD.Contains("Quarterly") && IsAfterCureProd(item["1st Quarter"], curePeriod, item["1st Quarter Status"]))
                {
                    item["IsInCurePeriod"] = "After";
                    item["1st Quarter Status Covenant"] = "Non-Compliant";
                    return;
                }
                else if (typeOfDD.Contains("Quarterly") && IsAfterCureProd(item["2nd Quarter"], curePeriod, item["2nd Quarter Status"]))
                {
                    item["IsInCurePeriod"] = "After";
                    item["2nd Quarter Status Covenant"] = "Non-Compliant";
                    return;
                }
                else if (typeOfDD.Contains("Quarterly") && IsAfterCureProd(item["3rd Quarter"], curePeriod, item["3rd Quarter Status"]))
                {
                    item["IsInCurePeriod"] = "After";
                    item["3rd Quarter Status Covenant"] = "Non-Compliant";
                    return;
                }
                else if (typeOfDD.Contains("Quarterly") && IsAfterCureProd(item["4th Quarter"], curePeriod, item["4th Quarter Status"]))
                {
                    item["IsInCurePeriod"] = "After";
                    item["4th Quarter Status Covenant"] = "Non-Compliant";
                    return;
                }
                else if (typeOfDD.Contains("Semi-Annual") && IsAfterCureProd(item["1st Semi-Annual"], curePeriod, item["1st Semi-Annual Status"]))
                {
                    item["IsInCurePeriod"] = "After";
                    item["1st Semi-Annual Status Covenant"] = "Non-Compliant";
                    return;
                }
                else if (typeOfDD.Contains("Semi-Annual") && IsAfterCureProd(item["2nd Semi-Annual"], curePeriod, item["2nd Semi-Annual Status"]))
                {
                    item["IsInCurePeriod"] = "After";
                    item["2nd Semi-Annual Status Covenant"] = "Non-Compliant";
                    return;
                }
                else if (typeOfDD.Contains("Annual") && IsAfterCureProd(item["Annual"], curePeriod, item["Annual Status"]))
                {
                    item["IsInCurePeriod"] = "After";
                    item["Annual Status Covenant"] = "Non-Compliant";
                    return;
                }

                if (isInCurePeriodProd)
                {
                    item["IsInCurePeriod"] = "In";
                }
            }
        }

        private bool IsAfterCureProd(object dateObj, int curePeriod, object status)
        {
            if (status != null && status.ToString() == "Final")
            {
                return false;
            }

            DateTime dueDate;
            if (dateObj != null && DateTime.TryParse(dateObj.ToString(), out dueDate))
            {
                DateTime cureDate = dueDate.AddDays(curePeriod);

                if (cureDate.AddDays(1) < DateTime.Now)
                {
                    return true;
                }

                if (dueDate.AddDays(1) < DateTime.Now)
                {
                    isInCurePeriodProd = true;
                }
            }
            return false;
        }

        private string GetReqStatusProd(SPListItem item)
        {
            hasWarningProd = false;

            string typeOfDD = string.Empty;
            if (item["Type of Due Date"] != null)
            {
                typeOfDD = item["Type of Due Date"].ToString();
            }

            if (typeOfDD.Contains("Monthly") && IsNonCompliantProd(item["January"], item["January Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantProd(item["February"], item["February Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantProd(item["March"], item["March Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantProd(item["April"], item["April Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantProd(item["May"], item["May Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantProd(item["June"], item["June Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantProd(item["July"], item["July Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantProd(item["August"], item["August Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantProd(item["September"], item["September Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantProd(item["October"], item["October Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantProd(item["November"], item["November Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && IsNonCompliantProd(item["December"], item["December Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Quarterly") && IsNonCompliantProd(item["1st Quarter"], item["1st Quarter Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Quarterly") && IsNonCompliantProd(item["2nd Quarter"], item["2nd Quarter Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Quarterly") && IsNonCompliantProd(item["3rd Quarter"], item["3rd Quarter Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Quarterly") && IsNonCompliantProd(item["4th Quarter"], item["4th Quarter Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Semi-Annual") && IsNonCompliantProd(item["1st Semi-Annual"], item["1st Semi-Annual Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Semi-Annual") && IsNonCompliantProd(item["2nd Semi-Annual"], item["2nd Semi-Annual Status"]))
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Annual") && IsNonCompliantProd(item["Annual"], item["Annual Status"]))
            {
                return "Non-Compliant";
            }

            if (hasWarningProd)
            {
                return "Warning";
            }
            return "Compliant";
        }

        private bool IsNonCompliantProd(object dateObj, object status)
        {
            if (status != null && status.ToString() == "Final")
            {
                return false;
            }

            DateTime date;
            if (dateObj != null && DateTime.TryParse(dateObj.ToString(), out date))
            {
                if (DateTime.Now < date.AddDays(1))
                {
                    if (!hasWarningProd && DateTime.Now.AddDays(14) > date)
                    {
                        hasWarningProd = true;
                    }
                    return false;
                }
                return true;
            }
            return false;
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

        private string GetLoanStatusProd(SPListItem item, SPList requirementsList)
        {
            SPQuery query = new SPQuery();
            query.Query = "<Where><And><Eq><FieldRef Name='Project_x0020_Id' /><Value Type='Text'>" + item["ID"] + "</Value></Eq>" +
                        "<Or><Eq><FieldRef Name='Requirements_x0020_Compliance_x0' /><Value Type='Text'>Non-Compliant</Value></Eq>" +
                        "<Eq><FieldRef Name='Requirements_x0020_Compliance_x0' /><Value Type='Text'>Warning</Value></Eq></Or></And></Where>";

            SPListItemCollection items = requirementsList.GetItems(query);

            if (items != null && items.Count > 0)
            {
                foreach (SPListItem reqItem in items)
                {
                    if (reqItem["Due Date Compliance Status"] != null && reqItem["Due Date Compliance Status"].ToString() == "Non-Compliant")
                    {
                        return "Non-Compliant";
                    }
                }
                return "Warning";
            }

            return "Compliant";
        }

        private string GetLoanCovenantStatusProd(SPListItem item, SPList requirementsList)
        {
            SPQuery query = new SPQuery();
            query.Query = "<Where><And><Eq><FieldRef Name='Project_x0020_Id' /><Value Type='Text'>" + item["ID"] + "</Value></Eq>" +
                        "<Eq><FieldRef Name='Covenant_x0020_Compliance_x0020_' /><Value Type='Text'>Non-Compliant</Value></Eq></And></Where>";

            SPListItemCollection items = requirementsList.GetItems(query);

            if (items != null && items.Count > 0)
            {
                return "Non-Compliant";
            }

            return "Compliant";
        }

        private string GetLoanCureStatusProd(SPListItem item, SPList requirementsList)
        {
            SPQuery query = new SPQuery();
            query.Query = "<Where><And><Eq><FieldRef Name='Project_x0020_Id' /><Value Type='Text'>" + item["ID"] + "</Value></Eq>" +
                        "<Or><Eq><FieldRef Name='IsInCurePeriod' /><Value Type='Text'>After</Value></Eq>" +
                        "<Eq><FieldRef Name='IsInCurePeriod' /><Value Type='Text'>In</Value></Eq></Or></And></Where>";

            SPListItemCollection items = requirementsList.GetItems(query);

            if (items != null && items.Count > 0)
            {
                foreach (SPListItem reqItem in items)
                {
                    if (reqItem["IsInCurePeriod"] != null && reqItem["IsInCurePeriod"].ToString() == "After")
                    {
                        return "After";
                    }
                }
                return "In";
            }

            return string.Empty;
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

        #endregion
    }
}
