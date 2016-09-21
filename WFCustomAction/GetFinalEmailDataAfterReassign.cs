using Microsoft.SharePoint.UserCode;
using System;
using System.Collections;
using System.Linq;

namespace WFCustomAction
{
    public class GetFinalEmailDataAfterReassign
    {
        private string res = string.Empty;
        public Hashtable GetEmailDataWhenReassign(SPUserCodeWorkflowContext context, string currentUser, string matrixRole, string managers)
        {
            Hashtable results = new Hashtable();
            results["result"] = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(currentUser) && !string.IsNullOrEmpty(matrixRole) && !string.IsNullOrEmpty(managers))
                {
                    string[] managersRows = managers.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    res += GetEmailData(currentUser, matrixRole, managersRows);
                }
            }
            catch (Exception e)
            {
                results = new Hashtable();
                res += e.ToString();
                results["success"] = false;
            }

            results["result"] = res;
            return results;
        }

        private string GetEmailData(string currentUser, string matrixRole, string[] managersRows)
        {
            string updatedManagersRows = string.Empty;

            for (int i = 0; i < managersRows.Count(); i++)
            {
                if (managersRows[i].ToLower().Contains(matrixRole.ToLower()))
                {
                    string username = managersRows[i].Split(new string[] { " - " }, StringSplitOptions.None).Last();
                    string updatedRole = managersRows[i].Replace(username, currentUser);
                    managersRows[i] = updatedRole;
                }
            }

            updatedManagersRows = string.Join("\n", managersRows);
            return updatedManagersRows;
        }
    }
}
