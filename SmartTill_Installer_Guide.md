# Step-by-Step: Building & Publishing SmartTill POS

Follow these steps in **Visual Studio 2022** to create a working installer for your application.

## 1. Prepare for a Fresh Build
Before publishing, it is important to clear out any old files that might cause errors.
1.  **Open your solution** (`SmartTill.V2.slnx`) in Visual Studio.
2.  **Clean Solution**: Go to the **Build** menu and select **Clean Solution**.
3.  **Restore Packages**: Right-click the **Solution** in Solution Explorer and select **Restore NuGet Packages**.

## 2. Build the Application
1.  **Rebuild**: Go to the **Build** menu and select **Rebuild Solution**.
    - *Note: Ensure you see "Build: 1 succeeded" in the Output window at the bottom.*
    - *Warning: Never use the `dotnet build` command in the terminal for this project!*

## 3. Publish to Create the Installer
1.  **Right-click** on the `SmartTill.V2` project in Solution Explorer.
2.  Select **Publish...**.
3.  In the Publish tab that opens, click the **Publish** button.
    - *Visual Studio will generate the installer files in `C:\Users\choto\Desktop\WORK\POS\PublishedPOS\`.*

## 4. Install and Run
1.  Go to the folder: **`C:\Users\choto\Desktop\WORK\POS\PublishedPOS\`**.
2.  Run the file **`setup.exe`**.
3.  Follow the prompts to install. The app will launch automatically once finished.

## 5. Finding the App Later
Once installed, the app lives in your **Windows Start Menu**:
1.  Press the Windows key and type "**SmartTill POS**".
2.  Right-click the icon and select **"Pin to Taskbar"** for easy access.

---

### Troubleshooting "Manifest Identity" Error
If you see an error saying "Reference in manifest does not match identity":
1.  **Delete** the `PublishedPOS` folder on your desktop.
2.  **Delete** the `bin` and `obj` folders in your project directory.
3.  Repeat steps **1, 2, and 3** above.
