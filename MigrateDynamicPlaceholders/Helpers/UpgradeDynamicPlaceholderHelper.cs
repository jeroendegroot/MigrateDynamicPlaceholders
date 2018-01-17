using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace MigrateDynamicPlaceholders.Helpers
{
    public class UpgradeDynamicPlaceholderHelper
    {
        private const string DatabaseName = "master";
        private const string DefaultDeviceId = "{FE5D7FDF-89C0-4D99-9AA3-B5FBD009C9F3}";
        private const string ItemsWithPresentationDetailsQuery = "/sitecore/content//*[@__Renderings != '' or @__Final Renderings != '']";
        private const string PlaceholderRegex = "_([0-9a-f]{8}[-][0-9a-f]{4}[-][0-9a-f]{4}[-][0-9a-f]{4}[-][0-9a-f]{12})";

        public Dictionary<Item, List<KeyValuePair<string, string>>> Iterate()
        {
            var result = new Dictionary<Item, List<KeyValuePair<string, string>>>();

            var master = Factory.GetDatabase(DatabaseName);
            var items = master.SelectItems(ItemsWithPresentationDetailsQuery);

            var layoutFields = new[] { FieldIDs.LayoutField, FieldIDs.FinalLayoutField };

            foreach (var itemInDefaultLanguage in items)
            {
                foreach (var itemLanguage in itemInDefaultLanguage.Languages)
                {
                    var item = itemInDefaultLanguage.Database.GetItem(itemInDefaultLanguage.ID, itemLanguage);
                    if (item.Versions.Count > 0)
                    {
                        foreach (var layoutField in layoutFields)
                        {
                            var changeResult = ChangeLayoutFieldForItem(item, item.Fields[layoutField]);

                            if (changeResult.Any())
                            {
                                if (!result.ContainsKey(item))
                                {
                                    result.Add(item, changeResult);
                                }
                                else
                                {
                                    result[item].AddRange(changeResult);
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        private List<KeyValuePair<string, string>> ChangeLayoutFieldForItem(Item currentItem, Field field)
        {
            var result = new List<KeyValuePair<string, string>>();

            string xml = LayoutField.GetFieldValue(field);

            if (!string.IsNullOrWhiteSpace(xml))
            {
                var details = LayoutDefinition.Parse(xml);

                var device = details.GetDevice(DefaultDeviceId);
                var deviceItem = currentItem.Database.Resources.Devices["Default"];

                var renderings = currentItem.Visualization.GetRenderings(deviceItem, false);
             
                if (device?.Renderings != null)
                {
                    bool requiresUpdate = false;

                    foreach (RenderingDefinition rendering in device.Renderings)
                    {
                        if (!string.IsNullOrWhiteSpace(rendering.Placeholder))
                        {
                            var newPlaceholder = rendering.Placeholder;
                            foreach (Match match in Regex.Matches(newPlaceholder, PlaceholderRegex, RegexOptions.IgnoreCase))
                            {
                                var renderingId = match.Value;
                                var newRenderingId = "-{" + renderingId.ToUpper().Substring(1) + "}-0";

                                newPlaceholder = newPlaceholder.Replace(match.Value, newRenderingId);

                                requiresUpdate = true;
                            }

                            result.Add(new KeyValuePair<string, string>(rendering.Placeholder, newPlaceholder));
                            rendering.Placeholder = newPlaceholder;
                        }
                    }

                    if (requiresUpdate)
                    {
                        string newXml = details.ToXml();

                        using (new EditContext(currentItem))
                        {
                            LayoutField.SetFieldValue(field, newXml);
                        }
                    }

                }
            }

            return result;
        }
    }
}