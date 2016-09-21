using Microsoft.SharePoint;
using Microsoft.SharePoint.UserCode;
using Microsoft.SharePoint.Workflow;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WFCustomAction
{
    public class CheckDoubledTasks
    {
        //Not finished, don't use in that way!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        public Hashtable CheckTasks(SPUserCodeWorkflowContext context, string id, string loginName)
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
                            SPList source = web.Lists["Requirements"];

                            if (source != null)
                            {
                                SPListItem sourceItem = source.GetItemById(currentId);

                                if (sourceItem != null)
                                {
                                    CheckManager(sourceItem, loginName);

                                    SPWorkflowTask taskedit = null;


                                    //SPWorkflowTask task = sourceItem.Tasks[new Guid(taskId)];
                                    //taskedit = task;

                                    //if (taskedit != null)
                                    //{
                                    //    // alter the task
                                    //    Hashtable ht = new Hashtable();
                                    //    ht["TaskStatus"] = "#";    // Mark the entry as approved

                                    //    SPWorkflowTask.AlterTask((taskedit as SPListItem), ht, true);
                                    //}
                                    //results["result"] = GetCompletionStatus(web, sourceItem);
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

        private void CheckManager(SPListItem item, string loginName)
        {
            List<string> doubledUsers = new List<string>();
            
            if (item["Person in charge"] is SPFieldUserValueCollection)
            {
                SPFieldUserValueCollection fieldValues = item["Person in charge"] as SPFieldUserValueCollection;
                if (fieldValues.Where(fv => fv.User.LoginName == loginName).Count() > 0)
                {
                    List<SPFieldUserValue> otherFieldValues = fieldValues.Where(fv => fv.User.LoginName != loginName).ToList();
                    foreach (SPFieldUserValue uv in otherFieldValues)
                    {
                        doubledUsers.Add(uv.LookupId + ";#" + uv.LookupValue);
                    }
                    CompleteTasks(item, doubledUsers);
                    return;
                }
            }


            //CheckManager(sourceItem, "Tax");
            //CheckManager(sourceItem, "Finance");
            //CheckManager(sourceItem, "Compliance");
            //CheckManager(sourceItem, "Internal Control");
        }

        private void CompleteTasks(SPListItem item, List<string> doubledUsers)
        {
            foreach (SPWorkflowTask task in item.Tasks)
            {
                XElement xml = XElement.Parse(task.Xml);
                if (doubledUsers.Contains(xml.Attribute("ows_AssignedTo").Value) && task["Outcome"] == null)
                {
                    string taskStatus = SPResource.GetString(new CultureInfo((int)task.Web.Language, false), "WorkflowTaskStatusComplete", new object[0]);
                    task[SPBuiltInFieldId.TaskStatus] = taskStatus;
                    task["Outcome"] = "Canceled";
                    task[SPBuiltInFieldId.Completed] = true;
                    task[SPBuiltInFieldId.PercentComplete] = 1;
                    task[SPBuiltInFieldId.ExtendedProperties] = "ows_FieldName_DelegateTo='' ows_FieldName_RequestTo='' ows_FieldName_NewDescription='' ows_FieldName_ConsolidatedComments='Approval (2) started by Peter Dimov on 12/17/2014 4:24 PM Comment: aa' ows_TaskStatus='Approved' ows_FieldName_NewDurationUnits='Day' ows_FieldName_NewSerialTaskDuration='' ows_FieldName_Comments='' ";
                    //task[TaskStatus] = "Approved"
                    task.Update();


                    //Hashtable ht = new Hashtable();
                    //ht["Status"] = "Completed";
                    //ht["Outcome"] = "Canceled";
                    //ht["PercentComplete"] = 1.0f;
                    ////ht["TaskStatus"] = "#";     // Mark the entry as approved

                    //SPWorkflowTask.AlterTask((task as SPListItem), ht, true);
                }
            }
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
