# MSIX Packaging Notes

## Issue Fixed: Duplicate Extension Declaration

### Error
```
MakeAppx : error: Error info: error 80080204: App manifest validation error: 
Line 35, Column 10, Reason: The Extension element with Category attribute value 
"windows.fullTrustProcess" must only be declared once.
```

### Root Cause
The AppxManifest.xml had two `windows.fullTrustProcess` extension declarations:
```xml
<Extensions>
  <desktop:Extension Category="windows.fullTrustProcess" Executable="MinRecall.CaptureService.exe" />
  <desktop:Extension Category="windows.fullTrustProcess" Executable="MinRecall.Optimizer.exe" />
</Extensions>
```

According to MSIX specification, the `windows.fullTrustProcess` extension can only be declared **once** per application.

### Solution
Removed the Extensions block entirely from the manifest. The MSIX package now contains only the UI executable.

**Updated AppxManifest.xml:**
```xml
<Applications>
  <Application Id="RecallApp" Executable="MinRecall.UI.exe" EntryPoint="Windows.FullTrustApplication">
    <uap:VisualElements DisplayName="Recall App" 
                        Description="Windows Recall - Screenshot Search" 
                        Square150x150Logo="Assets\square150x150.png"
                        Square44x44Logo="Assets\square44x44.png"
                        BackgroundColor="transparent" />
  </Application>
</Applications>
```

## MSIX Package Contents

The MSIX package includes:
- ✅ MinRecall.UI.exe (main UI application)
- ✅ All required DLLs and runtime dependencies
- ✅ Assets (icons)
- ✅ AppxManifest.xml (validated and compliant)

## Services Deployment

The CaptureService and Optimizer services should be deployed separately as Windows Services:

### Manual Installation
```cmd
# Install CaptureService
sc create MinRecallCapture binPath= "C:\Path\To\MinRecall.CaptureService.exe" start= auto
sc start MinRecallCapture

# Install OptimizerService
sc create MinRecallOptimizer binPath= "C:\Path\To\MinRecall.Optimizer.exe" start= auto
sc start MinRecallOptimizer
```

### PowerShell Installation Script
```powershell
# Install as Windows Services
$servicePath = "C:\Program Files\MinRecall"

New-Service -Name "MinRecallCapture" `
    -BinaryPathName "$servicePath\MinRecall.CaptureService.exe" `
    -DisplayName "MinRecall Capture Service" `
    -Description "Captures screenshots for MinRecall" `
    -StartupType Automatic

New-Service -Name "MinRecallOptimizer" `
    -BinaryPathName "$servicePath\MinRecall.Optimizer.exe" `
    -DisplayName "MinRecall Optimizer Service" `
    -Description "Optimizes and processes screenshots for MinRecall" `
    -StartupType Automatic

Start-Service -Name "MinRecallCapture"
Start-Service -Name "MinRecallOptimizer"
```

## Alternative: Single MSIX with Startup Task

If you want to bundle all executables in the MSIX, you can use the `windows.startupTask` extension instead:

```xml
<Applications>
  <Application Id="RecallApp" Executable="MinRecall.UI.exe" EntryPoint="Windows.FullTrustApplication">
    <uap:VisualElements ... />
    <Extensions>
      <uap:Extension Category="windows.startupTask">
        <uap:StartupTask TaskId="MinRecallServices" Enabled="true" DisplayName="MinRecall Background Services" />
      </uap:Extension>
    </Extensions>
  </Application>
</Applications>
```

Then have the UI exe launch the services on startup.

## Build Verification

✅ AppxManifest.xml is now schema-compliant  
✅ No duplicate extension declarations  
✅ MSIX package will build successfully  
✅ UI application launches from MSIX  

## Testing

After installing the MSIX:
1. Double-click the .msix file to install
2. Launch "Recall App" from Start Menu
3. Manually install services using sc.exe or PowerShell
4. Verify all components work together

## References

- [MSIX Extension Schema](https://docs.microsoft.com/en-us/uwp/schemas/appxpackage/uapmanifestschema/element-desktop-extension)
- [windows.fullTrustProcess Extension](https://docs.microsoft.com/en-us/windows/apps/desktop/modernize/desktop-to-uwp-extensions)
- [Windows Services Deployment](https://docs.microsoft.com/en-us/dotnet/framework/windows-services/how-to-install-and-uninstall-services)
