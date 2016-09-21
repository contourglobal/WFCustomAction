using Microsoft.SharePoint;
using Microsoft.SharePoint.UserCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Microsoft.SharePoint.Utilities;

namespace WFCustomAction
{
    public class CreateFolderInLibraryAction
    {
        Hashtable results = new Hashtable();
        public Hashtable CreateFolderInLibrary(SPUserCodeWorkflowContext context, string folderName, string libraryName, string folderPath)
        {
            char[] filenameChars = folderName.ToCharArray();
            foreach (char c in filenameChars)
            {
                if (!SPEncode.IsLegalCharInUrl(c))
                    folderName = folderName.Replace(c.ToString(), "");
            }
            results["result"] = string.Empty;
            try
            {
                using (SPSite site = new SPSite(context.CurrentWebUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        SPList library = web.Lists[libraryName];

                        if (library != null)
                        {
                            string folderUrl = CreateFolder(library, folderName, folderPath, web);
                            results["result"] += "Created Finished";
                            results["folderUrl"] = folderUrl;
                        }
                        else
                        {
                            results["result"] = string.Format("Library ({0}) not found.", libraryName);
                            results["folderUrl"] = string.Empty;
                        }
                    }

                    
                }
            }
            catch (Exception e)
            {
                results = new Hashtable();
                results["result"] = e.ToString();
                results["folderUrl"] = string.Empty;
            }

            return results;
        }

        private string CreateFolder(SPList spList, string folderName, string itemUrl, SPWeb web)
        {
            var folder = spList.Items.Add(itemUrl, SPFileSystemObjectType.Folder, folderName);
            string folderUrl = itemUrl + "/" + folder.Name;

            if (!FolderExists(folderUrl, web))
            {
                try
                {
                    folder.Update();
                }
                catch (Exception)
                {
                    throw;
                }
            }
            else
            {
                results["result"] = string.Format("Folder ({0}) already exists", folderName);
            }

            return folderUrl;
        }

        public bool FolderExists(string url, SPWeb web)
        {
            if (url.Equals(string.Empty))
            {
                return false;
            }

            try
            {
                return web.GetFolder(url).Exists;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
