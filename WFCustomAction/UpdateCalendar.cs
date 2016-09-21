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
    public class UpdateCalendar
    {
        public Hashtable UpdateDueDates(SPUserCodeWorkflowContext context, string calendarName)
        {
            Hashtable results = new Hashtable();
            results["result"] = string.Empty;
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        SPList requirementsList = web.Lists["Requirements"];
                        SPList calendarList = web.Lists[calendarName];

                        if (requirementsList != null && calendarList != null)
                        {
                            using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                            {
                                SPListItemCollection items = requirementsList.Items;

                                foreach (SPListItem item in items)
                                {
                                    int id = int.Parse(item["ID"].ToString());
                                    if (id > 170 && id < 191)     //temporary for tests only
                                    {
                                        if (item["Type of Due Date"] != null)
                                        {
                                            if (item["Type of Due Date"].ToString().Contains("Monthly"))
                                            {
                                                UpdateDate(item, "January", calendarList);
                                                UpdateDate(item, "February", calendarList);
                                                UpdateDate(item, "March", calendarList);
                                                UpdateDate(item, "April", calendarList);
                                                UpdateDate(item, "May", calendarList);
                                                UpdateDate(item, "June", calendarList);
                                                UpdateDate(item, "July", calendarList);
                                                UpdateDate(item, "August", calendarList);
                                                UpdateDate(item, "September", calendarList);
                                                UpdateDate(item, "October", calendarList);
                                                UpdateDate(item, "November", calendarList);
                                                UpdateDate(item, "December", calendarList);
                                            }
                                            else if (item["Type of Due Date"].ToString().Contains("Quarterly"))
                                            {
                                                UpdateDate(item, "1st Quarter", calendarList);
                                                UpdateDate(item, "2nd Quarter", calendarList);
                                                UpdateDate(item, "3rd Quarter", calendarList);
                                                UpdateDate(item, "4th Quarter", calendarList);
                                            }
                                            else if (item["Type of Due Date"].ToString().Contains("Semi-Annual"))
                                            {
                                                UpdateDate(item, "1st Semi-Annual", calendarList);
                                                UpdateDate(item, "2nd Semi-Annual", calendarList);
                                            }
                                            else if (item["Type of Due Date"].ToString().Contains("Annual"))
                                            {
                                                UpdateDate(item, "Annual", calendarList);
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
                results["result"] = e.ToString();
                results["success"] = false;
            }
            return results;
        }

        private void UpdateDate(SPListItem item, string date, SPList calendarList)
        {
            DateTime dueDate;
            if (item[date] != null && DateTime.TryParse(item[date].ToString(), out dueDate))
            {
                SPListItem calendarItem = calendarList.AddItem();
                calendarItem["Title"] = (item["Project"] ?? string.Empty) + " - " + date;
                calendarItem["Start Time"] = dueDate;
                calendarItem["End Time"] = dueDate;
                calendarItem["Managers"] = GetManagers(item);

                calendarItem.Update();
            }
        }

        private SPFieldUserValueCollection GetManagers(SPListItem item)
        {
            SPFieldUserValueCollection fieldValues = new SPFieldUserValueCollection();

            if (item["Responsible"] != null)
            {
                SPFieldUserValue userValue = new SPFieldUserValue(item.Web, item["Responsible"].ToString());
                fieldValues.Add(userValue);
            }

            SPFieldUser field = item.Fields["Person in charge"] as SPFieldUser;
            if (field != null && item["Person in charge"] != null)
            {
                SPFieldUserValueCollection picFieldValues = field.GetFieldValue(item["Person in charge"].ToString()) as SPFieldUserValueCollection;
                fieldValues.AddRange(picFieldValues);
            }

            if (item["Approver"] != null)
            {
                SPFieldUserValue userValue = new SPFieldUserValue(item.Web, item["Approver"].ToString());
                fieldValues.Add(userValue);
            }

            string secondApprover = GetSecondApprover(item);

            if (secondApprover != string.Empty && item[secondApprover] != null)
            {
                SPFieldUserValue userValue = new SPFieldUserValue(item.Web, item[secondApprover].ToString());
                fieldValues.Add(userValue);
            }

            return fieldValues;
        }

        private string GetSecondApprover(SPListItem item)
        {
            if (item["Category"] != null)
            {
                switch (item["Category"].ToString())
                {
                    case "Insurance":
                    case "Legal":
                        return "Legal";
                    case "Environnemental & societal":
                    case "Health and Safety compliance":
                        return "HS";
                    case "Tax":
                        return "Tax";
                    case "Construction":
                        return "Operational";
                }
            }
            return string.Empty;
        }
    }
}
