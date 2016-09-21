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
    public class GetBOICRAccessRightsMatrixIdAction
    {
        public Hashtable GetBOICRAccessRightsMatrixId(SPUserCodeWorkflowContext context, string fuelType, string component)
        {
            Hashtable results = new Hashtable();
            results["result"] = string.Empty;
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        if (fuelType != string.Empty && component != string.Empty)
                        {
                            results["result"] = GetAccessRightsListId(web, fuelType, component);
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

        private string GetAccessRightsListId(SPWeb web, string fuelType, string component)
        {
            SPList matrixList = web.Lists["Access Rights Matrix"];
            if (matrixList != null)
            {
                SPQuery query = new SPQuery();
                query.Query = "<Where><And><Eq><FieldRef Name='Fuel_x0020_Type'></FieldRef><Value Type='Choice'>" + fuelType + "</Value></Eq>" +
                                "<Eq><FieldRef Name='Component'></FieldRef><Value Type='Choice'>" + component + "</Value></Eq></And></Where>";

                SPListItemCollection items = matrixList.GetItems(query);

                if (items != null && items.Count > 0)
                {
                    return items[0]["ID"].ToString();
                }
            }
            return "";
        }
    }
}
