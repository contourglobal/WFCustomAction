using Microsoft.SharePoint;
using Microsoft.SharePoint.UserCode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace WFCustomAction
{
    public class GetBOICRDeadlineCategoryDatesIdAction
    {
        public Hashtable GetBOICRDeadlineCategoryDatesId(SPUserCodeWorkflowContext context, DateTime createdDate)
        {
            Hashtable results = new Hashtable();
            results["result"] = string.Empty;
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        string month = createdDate.ToString("MMMM", CultureInfo.CreateSpecificCulture("en"));
                        string year = createdDate.Year.ToString();
                        if (month != string.Empty && year != string.Empty)
                        {
                            results["result"] = GetListId(web, month, year);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                results = new Hashtable();
                results["result"] = e.ToString();
                results["success"] = false;
            }

            return results;
        }

        private string GetListId(SPWeb web, string month, string year)
        {
            SPList list = web.Lists["Deadline category dates"];
            if (list != null)
            {
                SPQuery query = new SPQuery();
                query.Query = "<Where><And><Eq><FieldRef Name='Month'></FieldRef><Value Type='Choice'>" + month + "</Value></Eq>" +
                                "<Eq><FieldRef Name='Year'></FieldRef><Value Type='Number'>" + year + "</Value></Eq></And></Where>";

                SPListItemCollection items = list.GetItems(query);

                if (items != null && items.Count > 0)
                {
                    return items[0]["ID"].ToString();
                }
            }
            return "";
        }
    }
}
