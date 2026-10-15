# Location heat map assignment

C# / .NET 10 MAUI application that records foreground GPS fixes to SQLite and
extends the native MAUI `Map` with density-colored `Circle` overlays.

## Open and build

Use Visual Studio on Windows with the .NET MAUI development workload, Android SDK
36, and a .NET 10 SDK. Open `LocationHeatmap.sln`, select the app project, and choose
an Android emulator or physical device. Use a short project path such as
`C:\Projects\LocationHeatmap` to avoid Windows Android resource path limits.
The checked application is Android;
the iOS target and location usage description are supplied for a Mac build but
have not been tested. MAUI Maps does not have a built-in Windows implementation,
so this project deliberately does not target Windows.

```powershell
dotnet workload install maui-android
dotnet build src/LocationHeatmap.App -f net10.0-android
dotnet run --project tests/LocationHeatmap.Checks
```

For a standalone APK, add `-p:EmbedAssembliesIntoApk=true -p:AndroidPackageFormats=apk`
to the build command. If SDK discovery fails, also pass
`-p:AndroidSdkDirectory="YOUR_ANDROID_SDK_PATH"`. Emulator-only builds may use
`-p:RuntimeIdentifier=android-x64`; omit this for a device build.

## Live tracking

1. Enable Google Maps SDK for Android in your Google Cloud project.
2. Replace `YOUR_GOOGLE_MAPS_API_KEY` in
   `src/LocationHeatmap.App/Platforms/Android/AndroidManifest.xml` with your key.
   Restrict the key to package `com.shobhit.locationheatmap` and your signing
   certificate SHA-1. Do not commit a personal API key.
3. Turn on the device's location services. Tap **Start tracking** and allow
   location access while using the app. Approximate location may be less accurate.
4. The app requests a fix, writes it to SQLite, then waits 10 seconds before
   requesting the next fix. GPS acquisition can take up to 8 seconds; this is not
   a strict 10-second sampling interval. Fixes with unknown accuracy or accuracy
   worse than 100 meters are ignored.
5. Tap **Stop**, or put the app in the background, to stop tracking. Return and
   tap Start again to resume. The previous history reloads from SQLite.

Location history stays in the app's private storage (`locations.db3`). Android
backup is disabled. The database is not encrypted. **Clear** asks for confirmation
before deleting the displayed history. No server or background service is used.

## Native map and sample route

Tap **Load demo** to store 141 synthetic fixes in a separate `demo.db3`, read
them back, and display the heat layer on the native MAUI Map (`HeatmapMap : Map`).
Both demo data and recorded GPS fixes use the same native map. Google map imagery
requires an API key authorized for Maps SDK for Android. The verified test build
uses an ignored local manifest; the shared manifest keeps its placeholder.
Demo and real histories remain separate. Reloading demo replaces only demo data.

The screenshot and Word report in `submission/` now show the actual MAUI Maps
control with Google map imagery. The heat overlay uses synthetic sample fixes
loaded from SQLite. Native overlay updates are batched to avoid rebuilding the
whole layer for every added cell.

In the Android emulator, the mouse wheel can pan the map. Use the map's plus and
minus buttons to zoom. The minus button was verified on the running app.

To use a private test manifest, create a `tools` folder, copy
`src/LocationHeatmap.App/Platforms/Android/AndroidManifest.xml` into
`tools/AndroidManifest.Local.xml`, and replace its key placeholder locally.
Build with `-p:AndroidManifest=../../tools/AndroidManifest.Local.xml`. The `tools`
folder is ignored by Git and excluded from the source archive.

## Density algorithm and scope

The calculator groups fixes into a local 65-meter grid and spreads their weights
across neighboring cells using `exp(-(dx² + dy²))`. Intensities are divided by
the maximum density, then colored blue through red. Stationary fixes are retained
so repeated visits become hotter. This is a relative sample-density heat map,
not a calibrated measure of elapsed dwell time. Longitude scale is adjusted by
latitude; the local approximation suits city routes and is not intended for
global or polar cartography. Up to the latest 2,000 fixes are rendered; older
history stays in SQLite. The overlay is made from overlapping circles rather
than a native raster heatmap renderer.

## Deliverables

- Full commented C# source, platform files, and solution in `src/`.
- Database and density checks in `tests/`.
- Word report with source listings, Android manifest, screenshot, and repository
  URL in `submission/Location Heat Map Assignment.docx`.
- Actual emulator output in `submission/application-output.png`.

## Repository

[Final Project MSCS 533 A01](https://github.com/sdhamania/Final_Project_MSCS-533-A01)

The solution and README are at the repository root, application source is in
`src/`, and the report and screenshots are in `submission/`. Build outputs,
local SQLite databases, and credentials are excluded by `.gitignore`.

## References

- [MAUI Map and map elements](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/map?view=net-maui-10.0)
- [MAUI Geolocation](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/device/geolocation)
- [C# code conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [sqlite-net](https://github.com/praeclarum/sqlite-net)

Only C# is used in this assignment; JavaScript and Dart conventions do not apply.
