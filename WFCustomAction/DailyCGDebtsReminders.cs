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
    public class DailyCGDebtsReminders
    {
        private string result = string.Empty;

        public Hashtable SendReminders(SPUserCodeWorkflowContext context, string daysBefore, bool isDev)
        {
            if (isDev)
            {
                return DevMethod(context, daysBefore, isDev);
            }
            else
            {
                return ProductionMethod(context, daysBefore, isDev);
            }
        }

        #region Dev

        private Hashtable DevMethod(SPUserCodeWorkflowContext context, string daysBefore, bool isDev)
        {
            Hashtable results = new Hashtable();
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        int days;
                        if (int.TryParse(daysBefore, out days))
                        {
                            SPList requirementsList = web.Lists["Requirements" + (isDev ? " Dev" : string.Empty)];
                            SPList emailList = web.Lists["Send Email" + (isDev ? " Dev" : string.Empty)];

                            if (requirementsList != null && emailList != null)
                            {
                                //using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                //{
                                SPListItemCollection items = requirementsList.Items;

                                foreach (SPListItem item in items)
                                {
                                    //if (item["ID"].ToString() == "189" || item["ID"].ToString() == "190" || item["ID"].ToString() == "191")     //temporary for tests only
                                    //{
                                    if (item["Type of Due Date"] != null)
                                    {
                                        if (item["Type of Due Date"].ToString().Contains(";#Monthly;#"))
                                        {
                                            CheckDateDev(item, "January", days, emailList, isDev);
                                            CheckDateDev(item, "February", days, emailList, isDev);
                                            CheckDateDev(item, "March", days, emailList, isDev);
                                            CheckDateDev(item, "April", days, emailList, isDev);
                                            CheckDateDev(item, "May", days, emailList, isDev);
                                            CheckDateDev(item, "June", days, emailList, isDev);
                                            CheckDateDev(item, "July", days, emailList, isDev);
                                            CheckDateDev(item, "August", days, emailList, isDev);
                                            CheckDateDev(item, "September", days, emailList, isDev);
                                            CheckDateDev(item, "October", days, emailList, isDev);
                                            CheckDateDev(item, "November", days, emailList, isDev);
                                            CheckDateDev(item, "December", days, emailList, isDev);
                                        }

                                        if (item["Type of Due Date"].ToString().Contains(";#Quarterly;#"))
                                        {
                                            CheckDateDev(item, "1st Quarter", days, emailList, isDev);
                                            CheckDateDev(item, "2nd Quarter", days, emailList, isDev);
                                            CheckDateDev(item, "3rd Quarter", days, emailList, isDev);
                                            CheckDateDev(item, "4th Quarter", days, emailList, isDev);
                                        }

                                        if (item["Type of Due Date"].ToString().Contains(";#Semi-Annual;#"))
                                        {
                                            CheckDateDev(item, "1st Semi-Annual", days, emailList, isDev);
                                            CheckDateDev(item, "2nd Semi-Annual", days, emailList, isDev);
                                        }

                                        if (item["Type of Due Date"].ToString().Contains(";#Annual;#"))
                                        {
                                            CheckDateDev(item, "Annual", days, emailList, isDev);
                                        }
                                    }
                                    //}
                                }
                                //}
                            }
                        }
                    }
                }

                results["result"] = result;
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

        private void CheckDateDev(SPListItem item, string date, int days, SPList emailList, bool isDev)
        {
            DateTime dueDate;
            if (item[date] != null && DateTime.TryParse(item[date].ToString(), out dueDate))
            {
                if ((item[date + " Status"] == null || item[date + " Status"].ToString() != "Final"))
                {
                    if (dueDate.AddDays(-days).ToShortDateString() == DateTime.Today.ToShortDateString())
                    {
                        SendEmailDev(item, emailList, dueDate, isDev);
                    }
                    else if (item["Linked to a reporting period?"] != null && item["Linked to a reporting period?"].ToString() == "Yes" &&
                                dueDate.AddYears(1).AddDays(-days).ToShortDateString() == DateTime.Today.ToShortDateString())
                    {
                        SendEmailDev(item, emailList, dueDate.AddYears(1), isDev);
                    }
                }
            }
        }

        private void SendEmailDev(SPListItem item, SPList emailList, DateTime dueDate, bool isDev)
        {
            SPListItem emailItem = emailList.AddItem();
            emailItem["To"] = GetSPUserObjectDev(item, "Responsible") + GetSPUserObjectDev(item, "Person in charge") + GetSPUserObjectDev(item, "Approver") + GetSecondApproverDev(item);
            //emailItem["CC"] = GetSPUserObject(item, "Legal");
            emailItem["Subject"] = "CG Debts Reminder: requirement to be fulfilled on " + (item["Project"] ?? string.Empty) + " on " + dueDate.ToShortDateString();
            emailItem["Body"] = "The " + (item["Project"] ?? string.Empty) + " requirement <i>" + (item["Name"] ?? string.Empty) + "</i> is to be fulfilled by " + dueDate.ToShortDateString() + " and is accessible under the following link: <br/><br/>" +
                "<a href='https://contourglobal.sharepoint.com/sites/finance/Finance/Lists/Requirements" + (isDev ? "%20Dev" : string.Empty) + "/DispForm.aspx?ID=" + item["ID"] + "'>Requirement</a><br/><br/>" +
                "In parallel of documentation and/or communication supporting the requirement fulfilment, please ensure the approval process through CG Debts is duly completed.<br/>" +
                "You can contact Debt compliance should you need any assistance with the above (Juliette Larapidie).";

            emailItem.Update();
        }

        private string GetSecondApproverDev(SPListItem item)
        {
            if (item["Category"] != null)
            {
                switch (item["Category"].ToString())
                {
                    case "Insurance":
                    case "Legal":
                        if (item["Legal"] != null)
                        {
                            return GetSPUserObjectDev(item, "Legal");
                        }
                        break;
                    case "Environnemental & societal":
                    case "Health and Safety compliance":
                        if (item["HS"] != null)
                        {
                            return GetSPUserObjectDev(item, "HS");
                        }
                        break;
                    case "Tax":
                        if (item["Tax"] != null)
                        {
                            return GetSPUserObjectDev(item, "Tax");
                        }
                        break;
                    case "Construction":
                        if (item["Operational"] != null)
                        {
                            return GetSPUserObjectDev(item, "Operational");
                        }
                        break;
                }
            }
            return string.Empty;
        }

        private string GetSPUserObjectDev(SPListItem sourceItem, String fieldName)
        {
            try
            {
                string emails = string.Empty;

                if (fieldName != string.Empty)
                {
                    SPFieldUser field = sourceItem.Fields[fieldName] as SPFieldUser;
                    if (field != null && sourceItem[fieldName] != null)
                    {
                        SPFieldUserValue fieldValue = field.GetFieldValue(sourceItem[fieldName].ToString()) as SPFieldUserValue;
                        if (fieldValue != null)
                        {
                            emails = fieldValue.User.Email + ";";
                        }
                        else
                        {
                            SPFieldUserValueCollection fieldValues = field.GetFieldValue(sourceItem[fieldName].ToString()) as SPFieldUserValueCollection;
                            foreach (SPFieldUserValue fv in fieldValues)
                            {
                                emails += fv.User.Email + ";";
                            }
                        }
                    }
                    else
                    {
                        if (field == null) throw new Exception("GetSPUserObject: field is null ");
                    }
                }

                return emails;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region Production

        private Hashtable ProductionMethod(SPUserCodeWorkflowContext context, string daysBefore, bool isDev)
        {
            Hashtable results = new Hashtable();
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        int days;
                        if (int.TryParse(daysBefore, out days))
                        {
                            SPList requirementsList = web.Lists["Requirements" + (isDev ? " Dev" : string.Empty)];
                            SPList emailList = web.Lists["Send Email" + (isDev ? " Dev" : string.Empty)];

                            if (requirementsList != null && emailList != null)
                            {
                                //using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                                //{
                                SPListItemCollection items = requirementsList.Items;

                                foreach (SPListItem item in items)
                                {
                                    //if (item["ID"].ToString() == "189" || item["ID"].ToString() == "190" || item["ID"].ToString() == "191")     //temporary for tests only
                                    //{
                                    if (item["Type of Due Date"] != null)
                                    {
                                        if (item["Type of Due Date"].ToString().Contains(";#Monthly;#"))
                                        {
                                            CheckDateProd(item, "January", days, emailList, isDev);
                                            CheckDateProd(item, "February", days, emailList, isDev);
                                            CheckDateProd(item, "March", days, emailList, isDev);
                                            CheckDateProd(item, "April", days, emailList, isDev);
                                            CheckDateProd(item, "May", days, emailList, isDev);
                                            CheckDateProd(item, "June", days, emailList, isDev);
                                            CheckDateProd(item, "July", days, emailList, isDev);
                                            CheckDateProd(item, "August", days, emailList, isDev);
                                            CheckDateProd(item, "September", days, emailList, isDev);
                                            CheckDateProd(item, "October", days, emailList, isDev);
                                            CheckDateProd(item, "November", days, emailList, isDev);
                                            CheckDateProd(item, "December", days, emailList, isDev);
                                        }

                                        if (item["Type of Due Date"].ToString().Contains(";#Quarterly;#"))
                                        {
                                            CheckDateProd(item, "1st Quarter", days, emailList, isDev);
                                            CheckDateProd(item, "2nd Quarter", days, emailList, isDev);
                                            CheckDateProd(item, "3rd Quarter", days, emailList, isDev);
                                            CheckDateProd(item, "4th Quarter", days, emailList, isDev);
                                        }

                                        if (item["Type of Due Date"].ToString().Contains(";#Semi-Annual;#"))
                                        {
                                            CheckDateProd(item, "1st Semi-Annual", days, emailList, isDev);
                                            CheckDateProd(item, "2nd Semi-Annual", days, emailList, isDev);
                                        }

                                        if (item["Type of Due Date"].ToString().Contains(";#Annual;#"))
                                        {
                                            CheckDateProd(item, "Annual", days, emailList, isDev);
                                        }
                                    }
                                    //}
                                }
                                //}
                            }
                        }
                    }
                }

                results["result"] = result;
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

        private void CheckDateProd(SPListItem item, string date, int days, SPList emailList, bool isDev)
        {
            DateTime dueDate;
            if (item[date] != null && DateTime.TryParse(item[date].ToString(), out dueDate))
            {
                if ((item[date + " Status"] == null || item[date + " Status"].ToString() != "Final"))
                {
                    if (dueDate.AddDays(-days).ToShortDateString() == DateTime.Today.ToShortDateString())
                    {
                        SendEmailProd(item, emailList, dueDate, isDev);
                    }
                    else if (item["Linked to a reporting period?"] != null && item["Linked to a reporting period?"].ToString() == "Yes" &&
                                dueDate.AddYears(1).AddDays(-days).ToShortDateString() == DateTime.Today.ToShortDateString())
                    {
                        SendEmailProd(item, emailList, dueDate.AddYears(1), isDev);
                    }
                }
            }
        }

        private void SendEmailProd(SPListItem item, SPList emailList, DateTime dueDate, bool isDev)
        {
            SPListItem emailItem = emailList.AddItem();
            emailItem["To"] = GetSPUserObjectProd(item, "Responsible") + GetSPUserObjectProd(item, "Person in charge") + GetSPUserObjectProd(item, "Approver") + GetSecondApproverProd(item);
            //emailItem["CC"] = GetSPUserObject(item, "Legal");
            emailItem["Subject"] = "CG Debts Reminder: requirement to be fulfilled on " + (item["Project"] ?? string.Empty) + " on " + dueDate.ToShortDateString();
            emailItem["Body"] = "The " + (item["Project"] ?? string.Empty) + " requirement <i>" + (item["Name"] ?? string.Empty) + "</i> is to be fulfilled by " + dueDate.ToShortDateString() + " and is accessible under the following link: <br/><br/>" +
                "<a href='https://contourglobal.sharepoint.com/sites/finance/Finance/Lists/Requirements" + (isDev ? "%20Dev" : string.Empty) + "/DispForm.aspx?ID=" + item["ID"] + "'>Requirement</a><br/><br/>" +
                "In parallel of documentation and/or communication supporting the requirement fulfilment, please ensure the approval process through CG Debts is duly completed.<br/>" +
                "You can contact Debt compliance should you need any assistance with the above (Juliette Larapidie).";

            emailItem.Update();
        }

        private string GetSecondApproverProd(SPListItem item)
        {
            if (item["Category"] != null)
            {
                switch (item["Category"].ToString())
                {
                    case "Insurance":
                    case "Legal":
                        if (item["Legal"] != null)
                        {
                            return GetSPUserObjectProd(item, "Legal");
                        }
                        break;
                    case "Environnemental & societal":
                    case "Health and Safety compliance":
                        if (item["HS"] != null)
                        {
                            return GetSPUserObjectProd(item, "HS");
                        }
                        break;
                    case "Tax":
                        if (item["Tax"] != null)
                        {
                            return GetSPUserObjectProd(item, "Tax");
                        }
                        break;
                    case "Construction":
                        if (item["Operational"] != null)
                        {
                            return GetSPUserObjectProd(item, "Operational");
                        }
                        break;
                }
            }
            return string.Empty;
        }

        private string GetSPUserObjectProd(SPListItem sourceItem, String fieldName)
        {
            try
            {
                string emails = string.Empty;

                if (fieldName != string.Empty)
                {
                    SPFieldUser field = sourceItem.Fields[fieldName] as SPFieldUser;
                    if (field != null && sourceItem[fieldName] != null)
                    {
                        SPFieldUserValue fieldValue = field.GetFieldValue(sourceItem[fieldName].ToString()) as SPFieldUserValue;
                        if (fieldValue != null)
                        {
                            emails = fieldValue.User.Email + ";";
                        }
                        else
                        {
                            SPFieldUserValueCollection fieldValues = field.GetFieldValue(sourceItem[fieldName].ToString()) as SPFieldUserValueCollection;
                            foreach (SPFieldUserValue fv in fieldValues)
                            {
                                emails += fv.User.Email + ";";
                            }
                        }
                    }
                    else
                    {
                        if (field == null) throw new Exception("GetSPUserObject: field is null ");
                    }
                }

                return emails;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

    }
}
