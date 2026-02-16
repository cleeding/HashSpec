using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Reflection;

namespace HashSpec.Selenium;

public static class WebDriverExtensions
{
    /// <summary>
    /// Scrapes the UI based on a provided schema (anonymous object) with a built-in wait.
    /// </summary>
    public static Dictionary<string, object> CaptureState(this IWebDriver driver, object schema, int timeoutSeconds = 5)
    {
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
        var state = new Dictionary<string, object>();
        var properties = schema.GetType().GetProperties();

        foreach (var prop in properties)
        {
            if (prop.GetValue(schema) is string selector)
            {
                try
                {
                    // Wait until the element is actually present
                    var element = wait.Until(d => d.FindElement(By.CssSelector(selector)));
                    state.Add(prop.Name, ExtractSemanticValue(element));
                }
                catch (WebDriverTimeoutException)
                {
                    state.Add(prop.Name, "[TIMEOUT/NOT FOUND]");
                }
                catch (NoSuchElementException)
                {
                    state.Add(prop.Name, "[NOT FOUND]");
                }
            }
        }
        return state;
    }

    /// <summary>
    /// Captures a list of repeating elements (e.g., rows in a table or items in a list).
    /// </summary>
    public static List<object> CaptureCollection(this IWebDriver driver, string containerSelector, string itemSelector)
    {
        var container = driver.FindElement(By.CssSelector(containerSelector));
        var items = container.FindElements(By.CssSelector(itemSelector));

        return items.Select(ExtractSemanticValue).ToList();
    }

    private static object ExtractSemanticValue(IWebElement element)
    {
        string tagName = element.TagName.ToLower();

        // 1. Capture common attributes that define the "Identity" of an element
        var metadata = new Dictionary<string, string>();
        string[] attributesToCapture = { "placeholder", "data-testid", "aria-label", "type", "title" };

        foreach (var attr in attributesToCapture)
        {
            var val = element.GetAttribute(attr);
            if (!string.IsNullOrEmpty(val)) metadata.Add(attr, val);
        }

        // 2. Specialized handling for Inputs
        if (tagName == "input" || tagName == "select" || tagName == "textarea")
        {
            return new
            {
                Value = element.GetAttribute("value") ?? "",
                Metadata = metadata,
                IsDisplayed = element.Displayed
            };
        }

        // 3. Specialized handling for Buttons/Interactive
        if (tagName == "button" || (tagName == "input" && element.GetAttribute("type") == "checkbox"))
        {
            return new
            {
                Text = element.Text.Trim(),
                Enabled = element.Enabled,
                Selected = element.Selected,
                Metadata = metadata
            };
        }

        // 4. Default: Return text, metadata, AND visual layout data
        // This detects if an element moves or changes size!
        return new
        {
            Text = element.Text.Trim(),
            Location = new { element.Location.X, element.Location.Y },
            Size = new { element.Size.Width, element.Size.Height },
            Metadata = metadata
        };
    }
}