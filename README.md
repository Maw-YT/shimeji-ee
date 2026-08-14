# Shimeji-ee (C#)

Windows port of [Shimeji-ee](https://github.com/groupfinity/Shimeji-ee) from the Java sources in `ogshimejieesrc`.

## Build

```powershell
dotnet build ShimejiEe.sln -c Release
```

Requires .NET 8 Windows Desktop SDK.

## Run

Place a Shimeji **`img`** folder next to the executable (or inside `ogshimejieesrc` if you run from the repo). The `conf` files from the original project are copied to the build output.

Typical layout:

```
Shimeji-ee.exe
conf\actions.xml
conf\behaviors.xml
conf\language*.properties
img\Shimeji\shime1.png
img\icon.png
```

Then:

```powershell
dotnet run --project ShimejiEe\ShimejiEe.csproj
```

Left-click the tray icon to spawn another mascot. Right-click a mascot or the tray icon for the original-style commands.

Scripts in `actions.xml` / `behaviors.xml` are evaluated with Jint, using the same `${}` / `#{}` syntax as the Java Nashorn engine.
