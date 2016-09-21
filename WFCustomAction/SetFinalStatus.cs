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
    public class SetFinalStatus
    {
        public Hashtable SetReqFinalStatus(SPUserCodeWorkflowContext context, string id, string dueDate, bool isDev)
        {
            if (isDev)
            {
                return DevMethod(context, id, dueDate, isDev);
            }
            else
            {
                return ProductionMethod(context, id, dueDate, isDev);
            }
        }

        #region Dev

        private bool hasWarningDev = false;
        private Hashtable DevMethod(SPUserCodeWorkflowContext context, string id, string dueDate, bool isDev)
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
                            SPList requirementsList = web.Lists["Requirements" + (isDev ? " Dev" : string.Empty)];

                            if (requirementsList != null)
                            {
                                SPListItem item = requirementsList.GetItemById(currentId);

                                if (item != null)
                                {
                                    using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                    {
                                        DateTime oldDate;
                                        if (item[GetStatusFieldDev(dueDate)] != null && DateTime.TryParse((item[GetStatusFieldDev(dueDate)].ToString()), out oldDate))
                                        {
                                            item[GetStatusFieldDev(dueDate)] = oldDate.AddYears(1);
                                        }
                                        item[GetStatusFieldDev(dueDate) + " Status"] = "";

                                        item["Due Date Compliance Status"] = GetReqStatusDev(item);
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
        private Hashtable ProductionMethod(SPUserCodeWorkflowContext context, string id, string dueDate, bool isDev)
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
                            SPList requirementsList = web.Lists["Requirements" + (isDev ? " Dev" : string.Empty)];

                            if (requirementsList != null)
                            {
                                SPListItem item = requirementsList.GetItemById(currentId);

                                if (item != null)
                                {
                                    using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                    {
                                        DateTime oldDate;
                                        if (item[GetStatusFieldProd(dueDate)] != null && DateTime.TryParse((item[GetStatusFieldProd(dueDate)].ToString()), out oldDate))
                                        {
                                            item[GetStatusFieldProd(dueDate)] = oldDate.AddYears(1);
                                        }
                                        item[GetStatusFieldProd(dueDate) + " Status"] = "";

                                        item["Due Date Compliance Status"] = GetReqStatusProd(item);
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
