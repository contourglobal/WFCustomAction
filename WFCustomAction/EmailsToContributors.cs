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
    public class EmailsToContributors
    {
        private string result = string.Empty;

        public Hashtable SendEmailsToContributors(SPUserCodeWorkflowContext context, string id, string type)
        {
            Hashtable results = new Hashtable();
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        int reqId;
                        if (int.TryParse(id, out reqId))
                        {
                            SPList contributorsList = web.Lists["Contributors"];
                            SPList emailList = web.Lists["Send Email"];

                            if (contributorsList != null && emailList != null)
                            {
                                SendEmailsToContributors(contributorsList, reqId, emailList, GetEncodedAbsoluteURL(web, reqId), type);
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

        private void SendEmailsToContributors(SPList contributorsList, int id, SPList emailList, string encodedAbsUrl, string type)
        {
            SPQuery query = new SPQuery();
            query.Query = "<Where><Eq><FieldRef Name='IRR_x0020_ID' /><Value Type='Text'>" + id + "</Value></Eq></Where>";

            SPListItemCollection items = contributorsList.GetItems(query);

            if (items != null && items.Count > 0)
            {
                foreach (SPListItem contributor in items)
                {
                    if (type == "Early")
                    {
                        SendEarlyEmail(contributor, emailList, id, encodedAbsUrl);
                    }
                    else if (type == "Late")
                    {
                        SendLateEmail(contributor, emailList, id, encodedAbsUrl);
                    }
                    else if (type == "Released")
                    {
                        SendReleasedEmail(contributor, emailList, id, encodedAbsUrl);
                    }
                }
            }
        }

        private void SendEarlyEmail(SPListItem item, SPList emailList, int id, string encodedAbsUrl)
        {
            SPListItem emailItem = emailList.AddItem();
            emailItem["To"] = GetSPUserObject(item, "Contributor");
            emailItem["Subject"] = "New Investor Relation Requirement";
            emailItem["Body"] = "There is a new Investor Relation Requirement for which you are selected as Contributor.<br/><br/>" +
                "<a href='https://contourglobal.sharepoint.com/irr/_layouts/15/WopiFrame.aspx?sourcedoc=" + encodedAbsUrl + "&action=default'>Edit Presentation</a><br/><br/>" +
                "When you are ready with your changes to in, please complete it here:<br/><br/>" +
                "<a href='https://contourglobal.sharepoint.com/irr/Lists/Contributors/EditForm.aspx?ID=" + item["ID"] + "&Source=https://contourglobal.sharepoint.com/irr/Investor%20relations%20requirements/Forms/AllItems.aspx'>Complete Presentation Changes</a>";

            emailItem.Update();
        }

        private void SendLateEmail(SPListItem item, SPList emailList, int id, string encodedAbsUrl)
        {
            SPListItem emailItem = emailList.AddItem();
            emailItem["To"] = GetSPUserObject(item, "Contributor");
            emailItem["Subject"] = "New Investor Relation Requirement";
            emailItem["Body"] = "Brand Compliance check<br/><br/>" +
                "<a href='https://contourglobal.sharepoint.com/irr/_layouts/15/WopiFrame.aspx?sourcedoc=" + encodedAbsUrl + "&action=default'>View Presentation</a>";

            emailItem.Update();
        }

        private void SendReleasedEmail(SPListItem item, SPList emailList, int id, string encodedAbsUrl)
        {
            SPListItem emailItem = emailList.AddItem();
            emailItem["To"] = GetSPUserObject(item, "Contributor");
            emailItem["Subject"] = "New Investor Relation Requirement";
            emailItem["Body"] = "Released<br/><br/>" +
                "<a href='https://contourglobal.sharepoint.com/irr/_layouts/15/WopiFrame.aspx?sourcedoc=" + encodedAbsUrl + "&action=default'>View Presentation</a>";

            emailItem.Update();
        }

        private string GetEncodedAbsoluteURL(SPWeb web, int id)
        {
            SPList irrList = web.Lists["Investor relations requirements"];

            SPQuery query = new SPQuery();
            query.Query = "<Where><Eq><FieldRef Name='ID' /><Value Type='Text'>" + id + "</Value></Eq></Where>";

            SPListItemCollection items = irrList.GetItems(query);
            
            if (items != null && items.Count > 0)
            {
                if (items[0][SPBuiltInFieldId.EncodedAbsUrl] != null)
                {
                    return items[0][SPBuiltInFieldId.EncodedAbsUrl].ToString();
                }
            }

            return string.Empty;
        }

        private string GetSPUserObject(SPListItem sourceItem, String fieldName)
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
    }
}
