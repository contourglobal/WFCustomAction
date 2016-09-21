using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint;

namespace AppraisalForm.Utils
{
    public class SPListHelper
    {
        #region private methods
        public static SPListItemCollection GetListItems(string listName, string currentWebUrl)
        {
            SPList list;
            using (SPSite site = new SPSite(currentWebUrl))
            {
                using (SPWeb web = site.OpenWeb())
                {
                    list = web.Lists.TryGetList(listName);

                    if (list == null)
                    {
                        return null;
                    }
                }
            }
            return list.Items;
        }

        public static SPListItemCollection GetListItemsByQuery(string listName, SPQuery camlQuery, string currentWebUrl)
        {
            SPList list;
            using (SPSite site = new SPSite(currentWebUrl))
            {
                using (SPWeb web = site.OpenWeb())
                {
                    list = web.Lists.TryGetList(listName);

                    if (list == null)
                    {
                        return null;
                    }

                    return list.GetItems(camlQuery);
                }
            }
        }

        public static SPListItemCollection GetListItemByLookupColumnValue(string listName, string columnName, object columnValue, string currentWebUrl)
        {
            SPList list;
            using (SPSite site = new SPSite(currentWebUrl))
            {
                using (SPWeb web = site.OpenWeb())
                {
                    list = web.Lists.TryGetList(listName);

                    if (list == null)
                    {
                        return null;
                    }
                    SPQuery camlQuery = new SPQuery() { Query = String.Format(@"<Where><Eq><FieldRef Name='{0}' LookupId='TRUE'/><Value Type='Lookup'>{1}</Value></Eq></Where>", columnName, columnValue) };
                    return list.GetItems(camlQuery);
                }
            }
        }

        public static SPListItem GetListItemByID(string listName, int id, string currentWebUrl)
        {
             SPList list;
             using (SPSite site = new SPSite(currentWebUrl))
             {
                 using (SPWeb web = site.OpenWeb())
                 {
                     list = web.Lists.TryGetList(listName);

                     if (list == null)
                     {
                         return null;
                     }
                     
                     SPQuery camlQuery = new SPQuery() { Query = String.Format(@"<Where><Eq><FieldRef Name='ID'/><Value Type='Number'>{0}</Value></Eq></Where>", id) };

                     SPListItemCollection coll = list.GetItems(camlQuery);

                     SPListItem item = null;
                     if (coll.Count > 0) item = coll[0];

                     return item;
                 }
             }
        }

        public static void UpdateItem(int id, string ListName, string fieldName, object fieldValue, string currentWebUrl){
            using (SPSite site = new SPSite(currentWebUrl))
            {
                using (SPWeb web = site.OpenWeb())
                {
                    SPListItem item = web.Lists[ListName].GetItemById(id);
                    item[fieldName] = fieldValue;
                    item.Update();
                }
            }
        }

        public static SPUser GetUser(SPListItem item, string fieldName, string currentWebUrl)
        {
            SPFieldUserValue userValue = new SPFieldUserValue(item.Web, item[fieldName].ToString());
            SPUser user = userValue.User;

            return user;
        }

        public enum MergeType { Or, And };

        /// <summary>
        /// Merge CAML conditions in code
        /// </summary>
        /// <param name="conditions">A list of contitions to merge into alternative or conjunction</param>
        /// <param name="type"><value>MergeType.Or</value> for alternative, MergeType.And for conjunction</param>
        //<returns></returns>
        private static string MergeCAMLConditions(List<string> conditions, MergeType type)
        {
            // No conditions => empty response
            if (conditions.Count == 0) return "";

            // Initialize temporary variables
            string typeStart = (type == MergeType.And ? "<And>" : "<Or>");
            string typeEnd = (type == MergeType.And ? "</And>" : "</Or>");

            // Build hierarchical structure
            while (conditions.Count >= 2)
            {
                List<string> complexConditions = new List<string>();

                for (int i = 0; i < conditions.Count; i += 2)
                {
                    if (conditions.Count == i + 1) // Only one condition left
                        complexConditions.Add(conditions[i]);
                    else // Two condotions - merge
                        complexConditions.Add(typeStart + conditions[i] + conditions[i + 1] + typeEnd);
                }

                conditions = complexConditions;
            }

            return conditions[0];
        }
        #endregion
    }
    public static class CAMLExtensions
    {
        public static string ToCamlIn(this List<int> ids)
        {
            return ToCamlIn(ids, null, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="fieldName">Deafult value is 'ID'</param>
        /// <param name="fieldType">Default value is 'Lookup'</param>
        /// <returns></returns>
        public static string ToCamlIn(this List<int> ids, string fieldName, string fieldType)
        {
            string template = string.Format(@"<In>
                                                <FieldRef Name='{1}' />
                                                <Values>
                                                    {0}
                                                </Values>
                                             </In>", string.Concat(ids.Select(id => string.Format("<Value Type='Number'>{0}</Value>", id))),
                                                   string.IsNullOrEmpty(fieldName) ? "ID" : fieldName,
                                                   string.IsNullOrEmpty(fieldType) ? "Lookup" : fieldType);
            return template;
        }


        public static int LookupToInt(this object id)
        {
            try
            {
                return int.Parse(id.ToString().Split(new string[] { ";#" }, StringSplitOptions.RemoveEmptyEntries)[1]);
                //TODO: return int.Parse(new SPFieldLookupValue(id.ToString()).LookupValue);
            }
            catch (Exception ex)
            {
                throw new Exception("LookupToInt Exception: " + ((id == null) ? "id is null" : id.ToString()));
            }
        }
    }

    public static class OOBColumnIdentifier
    {
        public static string GetColumnInternalNameByBreakdownType(string breakdownType)
        {
            int oobYear = DateTime.Now.Year;
            string internalName = string.Empty;
            switch (breakdownType)
            {
                case "CAPEX":
                    internalName = string.Format("CAPEX_x0020_{0}", oobYear);
                    break;
                case "Professional Fees":
                    internalName = GetProfessionalFeesColumnInternalName();
                    break;
                case "Employee":
                    internalName = string.Format("Employee_x0020_{0}", oobYear);
                    break;
                case "Travel":
                    internalName = string.Format("Travel_x0020_{0}", oobYear);
                    break;
                case "Facility":
                    internalName = string.Format("Facility_x0020_{0}", oobYear);
                    break;
                case "Other":
                    internalName = string.Format("Other_x0020_{0}", oobYear);
                    break;
                default:
                    internalName = "Column not found. Please conract your administrator or developers";
                    break;
            }

            return internalName;
        }

        //TODO: Update this function each year !!!
        public static string GetProfessionalFeesColumnInternalName()
        {
            int oobYear = DateTime.Now.Year;
            string internalName = "";
            switch (oobYear)
            {
                case 2015:
                    internalName = "Professional_x0020_Fees_x0020_20";
                    break;
                case 2016:
                    internalName = "Professional_x0020_Fees_x0020_200";
                    break;
                case 2017:
                    internalName = "Professional_x0020_Fees_x0020_201";
                    break;
                default:
                    internalName = "Column not found. Please conract your administrator or developers.";
                    break;
            }

            return internalName;
        }

        public static string GetImpactColumnInternalName()
        {
            int oobYear = DateTime.Now.Year;
            string internalName = string.Format("{0} Impact", oobYear);

            return internalName;
        }
    }
}
