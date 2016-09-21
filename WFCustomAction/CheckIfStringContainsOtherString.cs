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
    public class CheckIfStringContainsOtherStringAction
    {
        public Hashtable CheckIfStringContainsOtherString(SPUserCodeWorkflowContext context, string source, string text)
        {
            int index = -1;
            Hashtable results = new Hashtable();
            if (!string.IsNullOrEmpty(source))
            {
                index = source.IndexOf(text);
            }

            results["result"] = index;
            results["success"] = true;
            
            return results;
        }

        
    }
}
