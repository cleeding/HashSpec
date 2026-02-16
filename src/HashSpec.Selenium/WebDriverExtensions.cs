using OpenQA.Selenium;
using System.Reflection;

namespace HashSpec.Selenium;

public static class WebDriverExtensions
{
    /// <summary>
    /// Scrapes the UI based on a provided schema (anonymous object).
    /// </summary>
    public static Dictionary<string, object> CaptureState(this IWebDriver driver, object schema)
    {
        var state = new Dictionary<string, object>();
        var properties = schema.GetType().GetProperties();

        foreach (var prop in properties)
        {
            var value = prop.GetValue(schema);
            if (value is string selector)
            {
                try
                {
                    var element = driver.FindElement(By.CssSelector(selector));
                    state.Add(prop.Name, ExtractSemanticValue(element));
                }
                catch (NoSuchElementException)
                {
                    state.Add(prop.Name, "[NOT FOUND]");
                }
            }
        }

        return state;
    }

    private static object ExtractSemanticValue(IWebElement element)
    {
        string tagName = element.TagName.ToLower();

        // Handle inputs/selects by getting their value attribute
        if (tagName == "input" || tagName == "select" || tagName == "textarea")
        {
            return element.GetAttribute("value") ?? "";
        }

        // Handle buttons/checkboxes by including their enabled/selected state
        if (tagName == "button" || (tagName == "input" && element.GetAttribute("type") == "checkbox"))
        {
            return new { 
                Text = element.Text.Trim(), 
                Enabled = element.Enabled, 
                Selected = element.Selected 
            };
        }

        // Default: just take the visible text
        return element.Text.Trim();
    }
}