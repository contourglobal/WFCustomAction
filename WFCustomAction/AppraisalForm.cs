using AppraisalForm.Utils;
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
    public class AppraisalFormAction
    {
        public void SetStatuesWhenHelperDelay(SPUserCodeWorkflowContext context, int id)
        {
            try
            {
                AppraisalForm form = GetAppraisalFormByID(id, context.CurrentWebUrl, true);

                foreach(Objective obj in form.Objectives){
                    if (obj.Helpers.Where(h => h.State == StateType.Submited || h.State == StateType.TimeExpired).Count()
                        == obj.Helpers.Count())
                    {
                        if (obj.IsCompleted == false) 
                        {
                            SPListHelper.UpdateItem(obj.ID, Configuration.ObjectivesListName, "IsCompleted", true, context.CurrentWebUrl);
                        }
                    }
                }

                if (form.Objectives.Where(o => o.IsCompleted == true).Count() == form.Objectives.Count()) 
                {
                    if (form.IsCompleted == false)
                    {
                        SPListHelper.UpdateItem(form.ID, Configuration.AppraisalFormsListName, "IsCompleted", true, context.CurrentWebUrl);
                    }
                }
            }
            catch (Exception e)
            {

            }
        }


        public static AppraisalForm GetAppraisalFormByID(int id, string currentWebUrl, bool skipLoadingObjectives = false)
        {
            AppraisalForm form = new AppraisalForm();

            SPQuery appraisalQuery = new SPQuery() { Query = String.Format(@"<Where><Eq><FieldRef Name='ID'/><Value Type='Lookup'>{0}</Value></Eq></Where>", id) };
            SPListItemCollection queryAppraisalItems = SPListHelper.GetListItemsByQuery(Configuration.AppraisalFormsListName, appraisalQuery, currentWebUrl);

            if (queryAppraisalItems != null && queryAppraisalItems.Count > 0)
            {
                SPListItem appraisalItem = queryAppraisalItems[0];
                form.ID = appraisalItem.ID;
                
                form.IsCompleted = appraisalItem["IsCompleted"] != null ? (bool)appraisalItem["IsCompleted"] : false;
                form.AppraisalFormTemplateID = appraisalItem["AppraisalFormTemplateID"].LookupToInt();
                form.State = (StateType)Enum.Parse(typeof(StateType), appraisalItem["State"].ToString());
            }

            if (!skipLoadingObjectives)
            {
                form.Objectives = GetAppraisalFormObjectives(form.ID, currentWebUrl);
            }

            return form;
        }


        public  static List<Objective> GetAppraisalFormObjectives(int appraisalFormID, string currentWebUrl)
        {
            List<Objective> result = new List<Objective>();
            SPQuery objectivesQuery = new SPQuery() { Query = String.Format(@"<Where><Eq><FieldRef Name='AppraisalFormID'/><Value Type='Lookup'>{0}</Value></Eq></Where>", appraisalFormID) };
            SPListItemCollection queryObjectivesItems = SPListHelper.GetListItemsByQuery(Configuration.ObjectivesListName, objectivesQuery, currentWebUrl);

            if (queryObjectivesItems != null && queryObjectivesItems.Count > 0)
            {
                List<int> objectiveIds = queryObjectivesItems.OfType<SPListItem>().Select(i => i.ID).ToList();
                SPQuery helpersQuery = new SPQuery() { Query = String.Format(@"<Where>{0}</Where>", objectiveIds.ToCamlIn("ObjectiveID", null)) };
                SPListItemCollection queryHelpersItems = SPListHelper.GetListItemsByQuery(Configuration.HelpersListName, helpersQuery, currentWebUrl);
                List<Helper> helpers = ExtractHelpersFromSPList(queryHelpersItems);

                foreach (SPListItem objective in queryObjectivesItems)
                {
                    result.Add(new Objective()
                    {
                        ID = objective.ID,
                        AppraisalFormID = appraisalFormID,
                        IsCompleted = objective["IsCompleted"] != null ? bool.Parse(objective["IsCompleted"].ToString()) : false,
                        Helpers = helpers.Where(helper => helper.ObjectiveID == objective.ID).Select(helper => helper).ToList()
                    });
                }
            }

            return result;
        }

        public static List<Helper> ExtractHelpersFromSPList(SPListItemCollection queryHelperItems)
        {
            List<Helper> result = new List<Helper>();
            if (queryHelperItems != null)
            {
                foreach (SPListItem helper in queryHelperItems)
                {
                    result.Add(new Helper()
                    {
                        ID = helper.ID,
                        ObjectiveID = helper["ObjectiveID"].LookupToInt(),
                        State = (StateType)Enum.Parse(typeof(StateType), helper["State"].ToString())
                    });
                }
            }

            return result;
        }
    }

    public class AppraisalForm
    {
        public AppraisalForm()
        {
            Objectives = new List<Objective>();
        }

        public int ID { get; set; }
        public int AppraisalFormTemplateID { get; set; }
        public bool IsCompleted { get; set; }
        public List<Objective> Objectives { get; set; }
        public StateType State { get; set; }
    }
    public class Objective
    {
        public Objective()
        {
            Helpers = new List<Helper>();
        }

        public int ID { get; set; }
        public int AppraisalFormID { get; set; }
        public bool IsCompleted { get; set; }
        public List<Helper> Helpers { get; set; }
    }

    public class Helper
    {
        public int ID { get; set; }
        public int ObjectiveID { get; set; }
        public StateType State { get; set; }
    }

    public enum StateType
    {
        New = 1,
        InProgress = 2,
        TimeExpired = 3,
        Submited = 4
    }

    public class Configuration
    {
        #region List Names
        
        public const string AppraisalFormsListName = "AppraisalForms";
        
        public const string AppraisalFormTemplatesListName = "AppraisalFormTemplates";
        public const string HelpersListName = "Helpers";
        public const string ObjectivesListName = "Objectives";

        #endregion List Names
    }
}
