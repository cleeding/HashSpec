# 🛡️ HashSpec

### The Semantic State Safety Net for .NET & Selenium

HashSpec is a high-fidelity verification library that bridges the gap between specific assertions and broad regression testing. It captures the **entire logical state** of an object or UI component and verifies it against a deterministic fingerprint.

---

## 🚀 Why HashSpec?

Traditional tests often check for specific values (e.g., `Assert.Equal(200, status)`). While necessary, these tests miss the "everything else" that might have changed in your system state.

**HashSpec acts as a safety net that:**

- ✅ **Captures the Whole Truth** — Hashes every property of an object or UI state at once  
- ✅ **Semantic Equality** — Recognizes objects as equal when their data matches (regardless of property order)  
- ✅ **Visual Diffs** — Prints a color-coded diff when mismatches occur  

---

## 🛠️ Usage

### 1️⃣ Semantic Object Verification

Instead of writing dozens of assertions for complex nested objects, capture the entire "truth" in one line.

On first run, HashSpec automatically creates a baseline `.hash` and `.json` file inside your `Specs/` folder.

```csharp
var complexOrder = new {
    OrderId = 101,
    Customer = new { Name = "Jane Doe", Email = "jane@example.com" },
    Items = new[] {
        new { SKU = "ABC", Qty = 1, Price = 50.00 },
        new { SKU = "XYZ", Qty = 2, Price = 25.00 }
    }
};

// Automatically creates 'Specs/MyTestName.hash'
HashSpec.Verify(complexOrder);
```

---

### 2️⃣ Selenium UI State Discovery

Map live UI elements into a logical **State Object**. This detects:

- Layout shifts  
- Missing elements  
- Attribute changes  
- Structural regressions  

```csharp
driver.Navigate().GoToUrl("https://the-internet.herokuapp.com/login");

var uiState = driver.CaptureState(new {
    PageTitle = "h2",
    LoginButton = "button[type='submit']",
    UsernameField = "#username"
});

// Ensures the login page structure hasn't regressed
HashSpec.Verify("Login_Page_UI_State", uiState);
```

---

## 🩹 Updating Baselines ("Healer" Mode)

When intentional changes are made, you don't need to manually delete hash files.

Run your tests with the `HASHSPEC_UPDATE` environment variable set to `true` to overwrite existing baselines.

### PowerShell

```powershell
$env:HASHSPEC_UPDATE="true"; dotnet test
```

### Windows CMD

```cmd
set HASHSPEC_UPDATE=true && dotnet test
```

---

## 🔬 Testing the Safety Net

Try these to see HashSpec detect regressions:

### 🔄 Change a Value

Modify a property (e.g., change `Price` from `10.00` to `9.99`).  
The test will fail and show a detailed diff.

### 🎯 Change the UI

Update a CSS selector in your Selenium mapping.  
HashSpec will immediately detect structural differences.

### 🖥️ Check the Console Output

When a test fails, you'll see:

- Expected JSON state  
- Actual JSON state  
- A clear, color-coded diff  

---

## 🗺️ Roadmap

HashSpec is evolving. Here are the features currently in development:

- 🔓 **Sensitive Data Masking**  
  Support for filtering out volatile data (GUIDs, timestamps, tokens) during the hashing process.

- 📦 **Bulk Extraction**  
  Advanced state capture to pull massive datasets from tables, lists, or APIs for total-state verification in one call.

- 🎭 **Multi-Engine Support**  
  Dedicated adapters for Playwright and Cypress.

- 📂 **Failure Artifact Management**  
  Improved local reporting that saves a side-by-side comparison of the Spec vs. Actual JSON on failure.

---

## 📂 Project Structure

```
src/
├── HashSpec.Core        # Deterministic hashing engine & snapshot logic
├── HashSpec.Selenium    # IWebDriver extensions for semantic DOM state mapping
tests/
└── HashSpec.Tests       # Example & integration tests
```

---

## 🤝 Contributing

1. Fork the repository  
2. Create your feature branch  

```bash
git checkout -b feature/AmazingFeature
```

3. Commit your changes  
4. Push to the branch  
5. Open a Pull Request  

---

## 📜 License

Distributed under the MIT License. See `LICENSE` for details.
