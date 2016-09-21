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
using AppraisalForm.Utils;

namespace WFCustomAction
{
    public class CheckOOBBreakdownAction
    {
        public Hashtable CheckOOBBreakdown(SPUserCodeWorkflowContext context,double breakdownValue, string breakdownType, string breakdownsListName, string annualBreakdownsListName, double annualPercent)
        {
            string res = string.Empty;
            string debugInfo = string.Empty;
            string s = string.Empty;
            Hashtable results = new Hashtable();
            int oobYear = DateTime.Now.Year;
            bool success = true;
            string breakdownColumnName = OOBColumnIdentifier.GetColumnInternalNameByBreakdownType(breakdownType);
            if (breakdownValue != 0)
            {
                try
                {
                    using (SPSite site = new SPSite(context.CurrentWebUrl))
                    {
                        using (SPWeb web = site.OpenWeb())
                        {
                            double sumBefore = 0;
                            double sumAfter = 0;
                            double percent = 0.05;
                            double budget = 0;
                            double percentTotal = annualPercent/100;
                            double budgetTotal = 0;

                            SPList oobList = web.Lists[breakdownsListName];
                            if (oobList != null)
                            {
                                SPQuery oobQuery = new SPQuery();
                                oobQuery.Query = "";
                                /*
                                oobQuery.ViewXml = "<View>" +
                      "<ViewFields>" +
                        "<FieldRef Name='" + breakdownColumnName + "'/>" +
                      "</ViewFields>" +
                      "<Query>" +
                        "<Where><Eq><FieldRef Name='OOBAppro0'/><Value Type='WorkflowStatus'>16</Value></Eq></Where>" +
                      "</Query>" +
                    "</View>";
                                
                                 */

                                oobQuery.ViewXml = "<View>" +
                      "<ViewFields>" +
                        "<FieldRef Name='" + breakdownColumnName + "'/>" +
                      "</ViewFields>" +
                      "<Query>" +
                        "<Where>" +
                          "<And>" +
                             "<Eq>" +
                                "<FieldRef Name='OOBAppro0' />" +
                                "<Value Type='WorkflowStatus'>16</Value>" +
                             "</Eq>" +
                             "<And>" +
                                "<Eq>" +
                                   "<FieldRef Name='Date_x0020_of_x0020_request' />" +
                                   "<Value IncludeTimeValue='FALSE' Type='DateTime'>" + oobYear.ToString() + "-01-01</Value>" +
                                "</Eq>" +
                                "<Eq>" +
                                   "<FieldRef Name='Date_x0020_of_x0020_request' />" +
                                   "<Value IncludeTimeValue='FALSE' Type='DateTime'>" + oobYear.ToString() + "-12-31</Value>" +
                                "</Eq>" +
                             "</And>" +
                          "</And>" +
                       "</Where>" +
                      "</Query>" +
                    "</View>";


                                SPListItemCollection items = oobList.GetItems(oobQuery);
                                if (items != null)
                                {
                                    //debugInfo += string.Format("Items({0}) count: {1}{2}", breakdownColumnName, items.Count, Environment.NewLine);
                                    s += "1";
                                    foreach (SPListItem item in items)
                                    {
                                        sumBefore += item[breakdownColumnName] == null ? 0 : Convert.ToDouble(item[breakdownColumnName]);
                                    }
                                }

                                sumAfter = sumBefore + breakdownValue;
                                //debugInfo += string.Format("Sum before: {0} Sum after: {1}{2}", sumBefore, sumAfter, Environment.NewLine);
                                s += "2";
                            }
                            else
                            {
                                res = "List(" + breakdownsListName + ") not found.";
                                success = false;
                                //debugInfo += "List(" + breakdownsListName + ") not found.";
                                s += "3";
                            }


                            SPList breakdownList = web.Lists[annualBreakdownsListName];
                            var breakdownQuery = new SPQuery();
                            breakdownQuery.ViewXml =
                                "<View>" +
                                  "<Query>" +

                                  "</Query>" +
                                "</View>";
                            SPListItemCollection breakdowns = breakdownList.GetItems(breakdownQuery);
                            if (breakdowns != null)
                            {
                                //debugInfo += string.Format("Items(breakdowns types) count: {0}{1}", breakdowns.Count, Environment.NewLine);
                                s += "4";
                                foreach (SPListItem breakdown in breakdowns)
                                {
                                    s += "5";
                                    string fl = (string)breakdown["Expense_x0020_Estimate_x0020_Bre"];
                                    //debugInfo += fl;
                                    //debugInfo += "!"+new SPFieldLookupValue(fl).LookupValue+"!";
                                    var flv = new SPFieldLookupValue(fl).LookupValue;
                                    s += "6";
                                    if (breakdownType == flv)
                                    {
                                        percent = breakdown["Board_x0020_Major_x0020_Action_x"] == null ? 0 : Convert.ToDouble(breakdown["Board_x0020_Major_x0020_Action_x"]);
                                        budget = breakdown["Budget"] == null ? 0 : Convert.ToDouble(breakdown["Budget"]);

                                        //debugInfo += string.Format("Breakdown({0}) Percent: {1} Budget: {2}{3}", breakdownType, percent, budget, Environment.NewLine);
                                        s += "7";
                                    }
                                    budgetTotal += breakdown["Budget"] == null ? 0 : Convert.ToDouble(breakdown["Budget"]);
                                }
                                //debugInfo += string.Format("Breakdown({0}) BudgetTotal: {1}{2}", breakdownType, budgetTotal, Environment.NewLine);
                                s += "8";
                            }
                            else
                            {
                                res = "List(" + annualBreakdownsListName + ") not found.";
                                success = false;
                                //debugInfo += "List(" + annualBreakdownsListName + ") not found.";
                                s += "9";


                            }
                            if (string.IsNullOrEmpty(res))
                            {
                                if (Math.Round(sumAfter, 2, MidpointRounding.ToEven) > Math.Round(percent * budget, 2, MidpointRounding.ToEven) ||
                                    Math.Round(sumAfter, 2, MidpointRounding.ToEven) > Math.Round(percentTotal * budgetTotal, 2, MidpointRounding.ToEven))
                                {
                                    res = "Exceed";
                                }
                                else
                                {
                                    res = "OK";
                                }
                                //debugInfo += string.Format("Percent/Percent Total: {0}/{1}, {2}", percent, percentTotal, Environment.NewLine);
                                //debugInfo += string.Format("Treshhold1: {0}, {1}", Math.Round(percent * budget, 2, MidpointRounding.ToEven), Environment.NewLine);
                                //debugInfo += string.Format("Treshhold2: {0}, {1}", Math.Round(percentTotal * budgetTotal, 2, MidpointRounding.ToEven), Environment.NewLine);
                                //debugInfo += res;
                                s += "10";
                            }

                        }
                    }
                }
                catch (Exception e)
                {
                    results = new Hashtable();
                    results["result"] = s + e.ToString() + debugInfo;
                    results["success"] = false;
                    //results["debugInfo"] =  string.Format("{0} - {1}",debugInfo,e.Message);

                    return results;
                }
            }
            else
            {
                res = "OK-Zero";//no breakdown requested
            }

            results["result"] = res;
            //results["debugInfo"] = debugInfo;
            results["success"] = success;
            
            return results;
        }

        


        
    }
}
