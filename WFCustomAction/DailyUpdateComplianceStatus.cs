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
    public class DailyUpdateComplianceStatus
    {
        public Hashtable UpdateStatus(SPUserCodeWorkflowContext context, bool isDev)
        {
            if (isDev)
            {
                return DevMethod(context, isDev);
            }
            else
            {
                return ProductionMethod(context, isDev);
            }
        }

        #region Dev

        private bool hasWarningDev = false;
        private Hashtable DevMethod(SPUserCodeWorkflowContext context, bool isDev)
        {
            Hashtable results = new Hashtable();
            results["result"] = string.Empty;
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        SPList loanList = web.Lists["Loan" + (isDev ? " Dev" : string.Empty)];
                        SPList requirementsList = web.Lists["Requirements" + (isDev ? " Dev" : string.Empty)];

                        if (loanList != null && requirementsList != null)
                        {
                            using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                            {
                                SPListItemCollection items = requirementsList.Items;
                                string oldStatus;
                                string newStatus;

                                foreach (SPListItem item in items)
                                {
                                    oldStatus = item["Due Date Compliance Status"] == null ? "" : item["Due Date Compliance Status"].ToString();
                                    newStatus = GetReqStatusDev(item);
                                    if (oldStatus != newStatus)
                                    {
                                        item["Due Date Compliance Status"] = newStatus;
                                        item.Update();
                                    }

                                    oldStatus = item["Covenant Compliance Status"] == null ? "" : item["Covenant Compliance Status"].ToString();
                                    newStatus = GetReqCovenantStatusDev(item);
                                    if (oldStatus != newStatus)
                                    {
                                        item["Covenant Compliance Status"] = newStatus;
                                        item.Update();
                                    }
                                }

                                SPListItemCollection loanItems = loanList.Items;
                                string oldLoanStatus;
                                string newLoanStatus;

                                foreach (SPListItem item in loanItems)
                                {
                                    bool shouldUpdateLoanStatus = false;
                                    oldLoanStatus = item["Due Date Compliance Status"] == null ? "" : item["Due Date Compliance Status"].ToString();
                                    newLoanStatus = GetLoanStatusDev(item, requirementsList);
                                    if (oldLoanStatus != newLoanStatus)
                                    {
                                        item["Due Date Compliance Status"] = newLoanStatus;
                                        shouldUpdateLoanStatus = true;
                                    }

                                    oldLoanStatus = item["Covenant Compliance Status"] == null ? "" : item["Covenant Compliance Status"].ToString();
                                    newLoanStatus = GetLoanCovenantStatusDev(item, requirementsList);
                                    if (oldLoanStatus != newLoanStatus)
                                    {
                                        item["Covenant Compliance Status"] = newLoanStatus;
                                        shouldUpdateLoanStatus = true;
                                    }

                                    if (shouldUpdateLoanStatus)
                                    {
                                        item.Update();
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

            if (typeOfDD.Contains("Monthly") && item["January Status Covenant"] != null && item["January Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["February Status Covenant"] != null && item["February Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["March Status Covenant"] != null && item["March Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["April Status Covenant"] != null && item["April Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["May Status Covenant"] != null && item["May Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["June Status Covenant"] != null && item["June Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["July Status Covenant"] != null && item["July Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["August Status Covenant"] != null && item["August Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["September Status Covenant"] != null && item["September Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["October Status Covenant"] != null && item["October Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["November Status Covenant"] != null && item["November Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["December Status Covenant"] != null && item["December Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["1st Quarter Status Covenant"] != null && item["1st Quarter Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["2nd Quarter Status Covenant"] != null && item["2nd Quarter Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["3rd Quarter Status Covenant"] != null && item["3rd Quarter Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["4th Quarter Status Covenant"] != null && item["4th Quarter Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["1st Semi-Annual Status Covenant"] != null && item["1st Semi-Annual Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["2nd Semi-Annual Status Covenant"] != null && item["2nd Semi-Annual Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["Annual Status Covenant"] != null && item["Annual Status Covenant"].ToString() == "Non-Compliant")
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

        #endregion

        #region Production

        private bool hasWarningProd = false;
        private Hashtable ProductionMethod(SPUserCodeWorkflowContext context, bool isDev)
        {
            Hashtable results = new Hashtable();
            results["result"] = string.Empty;
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        SPList loanList = web.Lists["Loan" + (isDev ? " Dev" : string.Empty)];
                        SPList requirementsList = web.Lists["Requirements" + (isDev ? " Dev" : string.Empty)];

                        if (loanList != null && requirementsList != null)
                        {
                            using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                            {
                                SPListItemCollection items = requirementsList.Items;
                                string oldStatus;
                                string newStatus;

                                foreach (SPListItem item in items)
                                {
                                    oldStatus = item["Due Date Compliance Status"] == null ? "" : item["Due Date Compliance Status"].ToString();
                                    newStatus = GetReqStatusProd(item);
                                    if (oldStatus != newStatus)
                                    {
                                        item["Due Date Compliance Status"] = newStatus;
                                        item.Update();
                                    }

                                    oldStatus = item["Covenant Compliance Status"] == null ? "" : item["Covenant Compliance Status"].ToString();
                                    newStatus = GetReqCovenantStatusProd(item);
                                    if (oldStatus != newStatus)
                                    {
                                        item["Covenant Compliance Status"] = newStatus;
                                        item.Update();
                                    }
                                }

                                SPListItemCollection loanItems = loanList.Items;
                                string oldLoanStatus;
                                string newLoanStatus;

                                foreach (SPListItem item in loanItems)
                                {
                                    bool shouldUpdateLoanStatus = false;
                                    oldLoanStatus = item["Due Date Compliance Status"] == null ? "" : item["Due Date Compliance Status"].ToString();
                                    newLoanStatus = GetLoanStatusProd(item, requirementsList);
                                    if (oldLoanStatus != newLoanStatus)
                                    {
                                        item["Due Date Compliance Status"] = newLoanStatus;
                                        shouldUpdateLoanStatus = true;
                                    }

                                    oldLoanStatus = item["Covenant Compliance Status"] == null ? "" : item["Covenant Compliance Status"].ToString();
                                    newLoanStatus = GetLoanCovenantStatusProd(item, requirementsList);
                                    if (oldLoanStatus != newLoanStatus)
                                    {
                                        item["Covenant Compliance Status"] = newLoanStatus;
                                        shouldUpdateLoanStatus = true;
                                    }

                                    if (shouldUpdateLoanStatus)
                                    {
                                        item.Update();
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
                if (DateTime.Now < date)
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

            if (typeOfDD.Contains("Monthly") && item["January Status Covenant"] != null && item["January Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["February Status Covenant"] != null && item["February Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["March Status Covenant"] != null && item["March Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["April Status Covenant"] != null && item["April Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["May Status Covenant"] != null && item["May Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["June Status Covenant"] != null && item["June Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["July Status Covenant"] != null && item["July Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["August Status Covenant"] != null && item["August Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["September Status Covenant"] != null && item["September Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["October Status Covenant"] != null && item["October Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["November Status Covenant"] != null && item["November Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["December Status Covenant"] != null && item["December Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["1st Quarter Status Covenant"] != null && item["1st Quarter Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["2nd Quarter Status Covenant"] != null && item["2nd Quarter Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["3rd Quarter Status Covenant"] != null && item["3rd Quarter Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["4th Quarter Status Covenant"] != null && item["4th Quarter Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["1st Semi-Annual Status Covenant"] != null && item["1st Semi-Annual Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["2nd Semi-Annual Status Covenant"] != null && item["2nd Semi-Annual Status Covenant"].ToString() == "Non-Compliant")
            {
                return "Non-Compliant";
            }
            else if (typeOfDD.Contains("Monthly") && item["Annual Status Covenant"] != null && item["Annual Status Covenant"].ToString() == "Non-Compliant")
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

        #endregion
    }
}
