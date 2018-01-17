using MigrateDynamicPlaceholders.Helpers;
using Sitecore.Data.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace MigrateDynamicPlaceholders.sitecore.admin.custom
{
    public partial class MigrateDP : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var fixRenderings = new UpgradeDynamicPlaceholderHelper();
            var result = fixRenderings.Iterate();

            OutputResult(result);
        }

        private void OutputResult(Dictionary<Item, List<KeyValuePair<string, string>>> result)
        {
            Response.ContentType = "text/html";

            Response.Write($"<h1>{result.Count} items processed</h1>");
            foreach (var pair in result)
            {
                Response.Write($"<h3>{pair.Key.Paths.FullPath}</h3>");

                foreach (var kvp in pair.Value)
                {
                    if (kvp.Key != kvp.Value)
                    {
                        Response.Write($"<div>{kvp.Key} ==> {kvp.Value}</div>");
                    }
                }
            }
        }
    }
}