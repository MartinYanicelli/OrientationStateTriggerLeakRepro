# OrientationStateTrigger leak repro (.NET MAUI, Android)

On Android, when the Activity is recreated without killing the process (for example, after a font scale change), every previous page stays alive for the life of the process if one of its views uses `OrientationStateTrigger` with a setter whose value is a `DataTemplate` that references the view (here, a template that uses a `StaticResource` from the view's resources). One extra page (and its `Window`) leaks per Activity recreation. The same XAML without state triggers is collected normally.

## Versions

- .NET MAUI 10.0.110 (pinned with `MauiVersion` in the project file)
- Android (`net10.0-android`)
- `MauiXamlInflator` = `SourceGen`, as in the template. With XamlC it leaks even with a template that only uses a `Binding` (see Results).

## The app

- `App.CreateWindow` returns `new Window(new MainPage())` (no Shell, no DI), so each Activity recreation creates a new window and a new page.
- Each `MainPage` registers itself in a static tracker with a `WeakReference<Page>`.
- `ItemsView.xaml` declares two `DataTemplate`s in its resources (one of them uses a `StaticResource` color) and a `VisualStateGroup` whose `Portrait` and `Landscape` states, driven by `OrientationStateTrigger`, set `BindableLayout.ItemTemplate` to one or the other.
- With the **Use OrientationStateTrigger** switch on (default), the view keeps its state triggers. With it off (control), the view clears its visual state groups before it is attached to a window. The switch is persisted in `Preferences` and takes effect on the next window.
- **Collect and count** runs `GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();` three times and shows `Alive pages: N (created: M)`.
- Page creation and the counts are also written to logcat with the tag `OSTLeak`:

  ```sh
  adb logcat -s OSTLeak
  ```

`MainActivity` is exactly as the template generates it: its `ConfigurationChanges` do not include `FontScale`, so a font scale change recreates the Activity.

## Steps

1. Build and install the Release build:

   ```sh
   dotnet build -f net10.0-android -c Release -p:RuntimeIdentifiers=android-arm64 -p:AndroidPackageFormat=apk -t:SignAndroidPackage
   adb install -r bin/Release/net10.0-android/com.repro.orientationstatetriggerleak-Signed.apk
   ```

2. Open the app and tap **Collect and count**: `Alive pages: 1 (created: 1)`.
3. Recreate the Activity 3 times, waiting for the app to come back after each command:

   ```sh
   adb shell settings put system font_scale 1.15
   adb shell settings put system font_scale 1.0
   adb shell settings put system font_scale 1.15
   ```

4. Tap **Collect and count**.
   - **Expected:** `Alive pages: 1`
   - **Actual (switch on):** `Alive pages: 4`. Every old page is still alive.
5. Turn the switch off, force-stop and reopen the app, and repeat steps 2 to 4: `Alive pages: 1` (control).

## Results

Physical Android 16 device, Release, 3 Activity recreations:

| Variant | Pages alive / created |
| --- | --- |
| `OrientationStateTrigger`, setters with plain values (text, color) | 1 / 4 |
| Setters with resource `DataTemplate`s that only use a `Binding`, `SourceGen` | 1 / 4 |
| Setters with resource `DataTemplate`s that only use a `Binding`, XamlC | 4 / 4 |
| **Setters with resource `DataTemplate`s that use a `StaticResource`, `SourceGen` (this project)** | **4 / 4** |
| This project with the switch off (control) | 1 / 4 |

## Why

- `Window.Destroying` disconnects only the window handler and does not set `Page` to `null`, so `Window` is never set to `null` on the old elements, and their state triggers are never detached.
- [`OrientationStateTrigger`](https://github.com/dotnet/maui/blob/10.0.110/src/Controls/src/Core/OrientationStateTrigger.cs) subscribes to the static `DeviceDisplay.MainDisplayInfoChanged` event in `OnAttached` and unsubscribes only in `OnDetached`, so every old trigger, its `VisualState` and its setters stay alive.
- `VisualStateGroup` and `VisualState` hold their element through a `WeakReference`, so plain setter values do not keep the page alive. A compiled `DataTemplate` that needs the root object of its XAML file does: XamlC always generates a class that keeps it, and `SourceGen` keeps it in a closure when the template uses something resolved from the root, such as a `StaticResource`. That closes the chain from the static event to the old page and its `Window`.
- `AdaptiveTrigger` listens to `SizeChanged` of its own `Window` (an instance event) and, since [#36240](https://github.com/dotnet/maui/pull/36240), holds its element through a `WeakReference`.
