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
    public class CheckCompletionStatus
    {
        //Not used
        public Hashtable CheckRequirementCompletionStatus(SPUserCodeWorkflowContext context, string id, string sourceList)
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

                            if (source != null)
                            {
                                SPListItem sourceItem = source.GetItemById(currentId);

                                if (sourceItem != null)
                                {
                                    results["result"] = GetCompletionStatus(web, sourceItem);
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

        private string GetCompletionStatus(SPWeb web, SPListItem sourceItem)
        {
            object dueDateObj = GetDueDate(sourceItem);
            if (dueDateObj != null)
            {
                DateTime dueDate = new DateTime();
                if (DateTime.TryParse(dueDateObj.ToString(), out dueDate))
                {
                    SPList historyList = web.Lists["CG Debts History"];
                    SPQuery query = new SPQuery();
                    query.Query = "<Where><And><And><And><Eq><FieldRef Name='Project_x0020_Id' /><Value Type='Text'>" + sourceItem["ID"] + "</Value></Eq>" +
                                "<Eq><FieldRef Name='Reporting_x0020_Due_x0020_Date_x' /><Value Type='DateTime' IncludeTimeValue='FALSE'>" + dueDate.ToString("yyyy-MM-dd") + "</Value></Eq></And>" +
                                "<And><Eq><FieldRef Name='Category' /><Value Type='Text'>" + sourceItem["Category"] + "</Value></Eq>" +
                                "<Eq><FieldRef Name='Type_x0020_of_x0020_Due_x0020_Da' /><Value Type='Text'>" + sourceItem["Completed Due Date"] + "</Value></Eq></And></And>" +
                                "<Eq><FieldRef Name='Completion_x0020_Status' /><Value Type='Text'>Draft</Value></Eq></And></Where>";

                    SPListItemCollection items = historyList.GetItems(query);

                    if (items != null)
                    {
                        return items.Count > 0 ? "Final" : "Draft";
                    }
                }
            }
            return string.Empty;
        }

        private object GetDueDate(SPListItem sourceItem)
        {
            if (sourceItem["Completed Due Date"] != null)
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
                }
            }
            return string.Empty;
        }
    }
}
