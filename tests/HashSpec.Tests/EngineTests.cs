using System;
using System.IO;
using System.Collections.Generic;
using Xunit;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using HashSpec.Core;
using HashSpec.Selenium;

namespace HashSpec.Tests;

/// <summary>
/// Showcase tests for HashSpec: Semantic State Verification for QA.
/// These tests demonstrate how HashSpec acts as an additional safety net 
/// by providing deterministic state "fingerprinting" alongside traditional tests.
/// </summary>
public class EngineTests : IDisposable
{
    private readonly IWebDriver _driver;

    public EngineTests()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless"); 
        _driver = new ChromeDriver(options);
    }

    // --- SECTION 1: CORE DETERMINISTIC HASHING ---

    [Fact]
    public void Hasher_ShouldBe_Deterministic_Regardless_Of_Order()
    {
        // HashSpec ensures that objects with the same logical data produce 
        // the same hash, even if property initialization order differs.
        var state1 = new { Name = "Product A", Price = 100.00, InStock = true };
        var state2 = new { InStock = true, Price = 100.00, Name = "Product A" };

        var hash1 = DeterministicHasher.CreateFingerprint(state1);
        var hash2 = DeterministicHasher.CreateFingerprint(state2);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Verify_Should_Create_Baseline_On_First_Run()
    {
        // Demonstrates the automatic creation of the 'Specs' baseline directory.
        var testState = new { Status = "Success", Code = 200 };
        string specName = "Initial_Run_Test";

        HashSpec.Core.HashSpec.Verify(specName, testState);

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var projectDir = Directory.GetParent(baseDir)!.Parent!.Parent!.Parent!;
        string expectedPath = Path.Combine(projectDir.FullName, "Specs", $"{specName}.hash");

        Assert.True(File.Exists(expectedPath), $"Expected hash file at {expectedPath} was not found.");
    }

    [Fact]
    public void Verify_Complex_Nested_Domain_Model()
    {
        // Provides a safety net for deep object graphs, capturing the 
        // entire state "truth" in a single baseline.
        var complexOrder = new {
            OrderId = 101,
            Customer = new { Name = "Jane Doe", Email = "jane@example.com" },
            Items = new[] {
                new { SKU = "ABC", Qty = 1, Price = 50.00 },
                new { SKU = "XYZ", Qty = 2, Price = 25.00 }
            },
            Metadata = new Dictionary<string, string> { { "Source", "Web" } }
        };

        HashSpec.Core.HashSpec.Verify(complexOrder);
    }

    // --- SECTION 2: SELENIUM COMPANION (HashSpec.Selenium) ---

    [Fact]
    public void Test_Live_Website_State_Discovery()
    {
        // Captures a snapshot of the UI structure to detect unexpected 
        // layout or ID changes that specific assertions might overlook.
        _driver.Navigate().GoToUrl("https://the-internet.herokuapp.com/login");

        var uiState = _driver.CaptureState(new
        {
            PageTitle = "h2",
            LoginButton = "button[type='submit']",
            UsernameField = "#username"
        });

        HashSpec.Core.HashSpec.Verify(uiState);
    }

    [Fact]
    public void Test_Login_Failure_Semantic_State()
    {
        _driver.Navigate().GoToUrl("https://the-internet.herokuapp.com/login");

        _driver.FindElement(By.Id("username")).SendKeys("tomsmith");
        _driver.FindElement(By.Id("password")).SendKeys("wrong_password");
        _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        // Snapshot the failure state to ensure the error messaging 
        // and UI context remain consistent over time.
        var uiState = _driver.CaptureState(new
        {
            ErrorMessage = "#flash",
            PageHeader = "h2",
            SubmitButton = "button.radius"
        });

        HashSpec.Core.HashSpec.Verify(uiState);
    }

    [Fact]
    public void Selenium_Should_Ignore_Volatile_UI_Data()
    {
        // Shows how to filter out "noise" so the safety net 
        // only triggers on meaningful state changes.
        var uiState = new {
            Header = "Dashboard",
            User = "Authenticated_User",
            GeneratedAt = DateTime.Now.ToString(), 
            Token = Guid.NewGuid().ToString()      
        };

        /* var spec = new SemanticSpec()
            .IgnoreProperty("GeneratedAt")
            .IgnoreProperty("Token");

        HashSpec.Core.HashSpec.Verify("Volatile_Dashboard_Test", uiState, spec);
        */
    }

    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
    }
}