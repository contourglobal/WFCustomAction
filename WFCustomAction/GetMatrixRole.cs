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
    public class GetMatrixRole
    {
        private string resultM = string.Empty;
        public Hashtable GetRole(SPUserCodeWorkflowContext context, string managers, string mailData, string managerName)
        {
            Hashtable results = new Hashtable();
            results["result"] = string.Empty;
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        if (mailData == null || !mailData.Contains(managerName))
                        {
                            string[] managersRows = managers.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                            resultM += GetRoles(web, managersRows, managerName);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                results = new Hashtable();
                resultM += e.ToString();
                results["success"] = false;
            }

            results["result"] = resultM;
            return results;
        }

        private string GetRoles(SPWeb web, string[] managersRows, string managerName)
        {
            string result = string.Empty;

            foreach (string row in managersRows)
            {
                if (row.Contains(managerName))
                {
                    string role = row.Substring(9, row.IndexOf(" - ") - 9);
                    result += result == string.Empty ? role : ", " + role;
                }
            }

            return result;
        }
    }
}
