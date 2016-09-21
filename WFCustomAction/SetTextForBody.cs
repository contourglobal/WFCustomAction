using Microsoft.SharePoint;
using Microsoft.SharePoint.UserCode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WFCustomAction
{
    public class SetTextForBody
    {
        public Hashtable SetTextForEmailBody(SPUserCodeWorkflowContext context, string text)
        {
            Hashtable results = new Hashtable();
            try
            {
                text = text.Replace("</p>", "%0D").Replace('"', '\'').Replace("\r\n", "%0D");
                results["result"] = Regex.Replace(text, "<.*?>", string.Empty);
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

    }
}
