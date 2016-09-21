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
    public class GetSMApprovalMatrixId
    {
        public Hashtable GetApprovalMatrixId(SPUserCodeWorkflowContext context, string region, string documentGroup)
        {
            Hashtable results = new Hashtable();
            results["result"] = string.Empty;
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        if (region != string.Empty && documentGroup != string.Empty)
                        {
                            results["result"] = GetMatrixId(web, region, documentGroup);
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

        private string GetMatrixId(SPWeb web, string region, string documentGroup)
        {
            SPList matrixList = web.Lists["SM Approval Matrix"];
            if (matrixList != null)
            {
                SPQuery query = new SPQuery();
                query.Query = "<Where><And><Eq><FieldRef Name='Region'></FieldRef><Value Type='Text'>" + region + "</Value></Eq>" +
                                "<Eq><FieldRef Name='Document_x0020_Group'></FieldRef><Value Type='Text'>" + documentGroup + "</Value></Eq></And></Where>";

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
