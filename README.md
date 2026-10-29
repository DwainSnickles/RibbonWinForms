# RibbonWinForms for .NET 10

[![Build](https://github.com/DwainSnickles/RibbonWinForms/actions/workflows/build.yml/badge.svg?branch=net10)](https://github.com/DwainSnickles/RibbonWinForms/actions/workflows/build.yml)
[![Download v1.0.0](https://img.shields.io/badge/download-v1.0.0-blue)](https://github.com/DwainSnickles/RibbonWinForms/releases/tag/v1.0.0)

**[Download RibbonLib.dll (v1.0.0)](https://github.com/DwainSnickles/RibbonWinForms/releases/tag/v1.0.0)** — no NuGet package yet, so this is the built DLL, or add the project to your solution.

An Office-style ribbon control for Windows Forms, ported to **.NET 10** and repaired so it works
both at runtime and inside the **out-of-process WinForms designer** that current Visual Studio uses.

This is a fork of [RibbonWinForms](https://github.com/RibbonWinForms/RibbonWinForms) (MS-PL, originally
by Jose Menendez Poo). Upstream targets .NET Framework 4.8; this fork moves the library to
`net10.0-windows` and fixes the things that port broke.

![The ribbon rendered by this library: orb, tabs, three panels with items, and panel footers](docs/ribbon.png)

## What this fork changes

**It runs.** The ribbon paints its tabs, panels, items, quick access toolbar, contexts and orb, and it
handles mouse input — the same as on .NET Framework.

**It designs.** The old fixed-in-process designer is gone. This fork implements a designer against the
out-of-process design server:

- Click a **tab**, a **panel** or an **item** on the design surface to select it and edit it in the
  Properties window.
- Three pills on the surface add structure without hunting through verbs: **Add Tab**, **Add Panel**,
  and **Add Control** (which opens a menu of Button, Combo Box, Text Box, Check Box, Up Down, Label,
  Button List, Item Group, Color Chooser, Host and Separator).
- The item collections have real **collection editors** in the Properties grid, so panels, items, orb
  entries, button lists and the quick access toolbar can be edited visually instead of by hand.
- Everything written into `Form1.Designer.cs` is ordinary, readable `new RibbonTab()` / `Panels.Add(...)`
  code — no custom serialization to unpick later.

**It keeps design-time code out of your app.** A deployed app must never load
`Microsoft.DotNet.DesignTools.Server`, which only exists inside Visual Studio, so the designers,
glyphs and collection editors live in a separate assembly, `RibbonLib.Design.dll`. `RibbonLib.dll`
itself references **no** design-tools assembly: it names its designers by *string* in attributes and
reaches them through a plain `object` handshake (`Classes/RibbonDesignTime.cs`). The design assembly
is copied next to your application — that is where the design server looks for it — but it is not a
compile-time reference and nothing loads it at runtime.

That split also fixes an inspection problem: enumerating the shipping assembly used to throw
`FileNotFoundException: Could not load file or assembly 'Microsoft.DotNet.DesignTools.Server,
Version=1.6.0.0'` in any process without design tools (Visual Studio's toolbox discovery, for one),
because the designers were public types inside it.

## Requirements

| | |
|---|---|
| Runtime | Windows, .NET 10 (`net10.0-windows`) — nothing else |
| Design-time | Visual Studio with the WinForms designer (the out-of-process design server) |
| Build | .NET 10 SDK |

`RibbonLib.Design` compiles against `Microsoft.WinForms.Designer.SDK` 1.6.0, which supplies the
design-time types at compile time only. At design time the code runs against whichever design-tools
build your Visual Studio hosts; nothing from that package is shipped to your application.

## Quick start

```csharp
using RibbonLib;

var ribbon = new Ribbon { Dock = DockStyle.Top };

var tab = new RibbonTab { Text = "Home" };
var panel = new RibbonPanel { Text = "Clipboard" };

var paste = new RibbonButton
{
    Text = "Paste",
    Image = Properties.Resources.Paste,
    LargeImage = Properties.Resources.PasteLarge
};
paste.Click += (s, e) => MessageBox.Show("Paste");

panel.Items.Add(paste);
tab.Panels.Add(panel);
ribbon.Tabs.Add(tab);

Controls.Add(ribbon);
```

Use `RibbonForm` instead of `Form` if you want the ribbon merged into a glass caption bar.

## Working in the designer

1. Drop a `Ribbon` on the form (it docks to the top).
2. Click **Add Tab**, then **Add Panel** on the tab you just made.
3. Click **Add Control** on a panel and pick the item type from the menu.
4. Select any tab, panel or item to edit it in the Properties window.
5. For collections (`Tabs`, `Panels`, `Items`, orb collections, `Buttons`, quick access items), use the
   collection editor from the Properties window — add, remove and reorder there.

## Spacing and padding

The ribbon's spacing is editable. Select the ribbon and every value appears under the **Padding**
category in the Properties window; each one takes effect as you type it and is written into
`Form1.Designer.cs` like any other property.

| Property | What it pads |
|---|---|
| `Padding` | The content area below the tabs — the ribbon's own padding |
| `TabsPadding` | Inside each tab, around its text |
| `OrbsPadding` | Inside the orb, around its text |
| `TabTextMargin` | Between a tab's text and the tab edge |
| `PanelPadding` | Around each panel, between it and its neighbours |
| `PanelMargin` | Inside each panel; the bottom value is the caption height |
| `ItemPadding` | Around each control (button, combo box…) inside a panel |
| `ItemMargin` | Inside each control, between its content and its edge |

```csharp
ribbon.Padding = new Padding(12, 4, 12, 6);   // breathing room around the panel area
ribbon.ItemPadding = new Padding(6);          // space around every control in every panel
```

Padding you set by hand survives a change of `OrbStyle`: the style change still updates colours,
roundness and everything else, but leaves your spacing alone. A ribbon you have not touched keeps
taking the style's own spacing defaults.

### Per-panel spacing

A panel can use its own spacing instead of the ribbon's. Select the panel and set any of these. A
blank value means *use the ribbon's*, and clearing the cell goes back to inheriting.

| Panel property | Overrides | Notes |
|---|---|---|
| `ItemPadding` | the ribbon's `ItemPadding` | Space around the controls of this panel |
| `ItemMargin` | the ribbon's `ItemMargin` | Space inside the controls of this panel |
| `PanelMargin` | the ribbon's `PanelMargin` | Space inside the panel; the bottom value is this panel's caption height |

```csharp
roomyPanel.ItemPadding = new Padding(14, 6, 14, 6);   // only this panel gets air
busyPanel.ItemPadding = new Padding(2, 0, 2, 0);     // and this one stays tight
busyPanel.ItemPadding = null;                        // back to the ribbon's value
```

`PanelPadding` — the gap *between* panels inside a tab — stays ribbon-wide, because it belongs to
the space between panels rather than to any one of them.

## The toolbox

`Ribbon` carries `[ToolboxItem(true)]`; the tabs, panels, items, contexts and popups that only make
sense inside a ribbon are marked `DesignTimeVisible(false)`, so the toolbox offers the ribbon and
nothing that would be wrong to drop on a form.

A control appears in the toolbox, with no registration, when the project you are designing
references its assembly. With **Tools → Options → Windows Forms Designer → General → Automatically
populate the toolbox** enabled (the default), a solution containing a project that references
`RibbonLib` — by project reference or by the NuGet package — shows a **RibbonLib Components** tab
while that solution is open. The tab follows the reference: no reference, no tab.

For an entry that does not depend on the open solution, register the assembly once with
**Choose Toolbox Items → Browse** and point it at `RibbonLib.dll`.

## Building

```
dotnet build RibbonLib.slnx
```

Open `RibbonLib.slnx` in Visual Studio to work on the designer itself. Design-time behaviour can only be
exercised inside Visual Studio; the runtime behaviour can be tested from any app that references the
project.

## Releasing

Push a `v*` tag and the **Publish NuGet package** workflow packs `RibbonLib.Net10` and pushes it to
nuget.org, along with its symbol package:

```
git tag -a v1.0.1 -m "1.0.1" && git push origin v1.0.1
```

- `v1.2.3` publishes package version `1.2.3`; `v2.0.0-beta.1` publishes `2.0.0-beta.1`. A tag that is
  not `v<major>.<minor>.<patch>[-prerelease]` fails the run instead of publishing a guessed version.
- The key comes from the repository secret `NUGET_API_KEY` (scope *Push new packages and package
  versions*, glob `RibbonLib.Net10`). If it is missing the job stops before building anything it would
  have to throw away.
- Run it by hand from **Actions → Publish NuGet package → Run workflow** with `dry_run: true` to pack
  and verify a package without publishing it. The `.nupkg` and `.snupkg` are attached to the run either
  way.

## How the design-time support is put together

Two assemblies, so the boundary is a compile-time fact rather than a convention:

| Assembly | Contents |
|---|---|
| `RibbonLib.dll` | The control itself. No reference to `Microsoft.DotNet.DesignTools.*` |
| `RibbonLib.Design.dll` | Everything that only runs inside the designer |

| Piece | Role |
|---|---|
| `RibbonLib.Design/Designers/RibbonDesigner.cs` | The ribbon designer: selection, the Add pills, the Add Control menu, the Properties verbs |
| `RibbonLib.Design/Editors/` | Collection editors for the item collections |
| `RibbonLib.Design/Glyphs/` | Design-surface decorations (orb, quick access toolbar, Add pills) |
| `Classes/RibbonDesignToolsResolver.cs` | Answers design-tools assembly requests with the copy the IDE has loaded, whatever its version. It sits in the runtime assembly so it is registered before the design assembly's types are resolved; it names no design-tools type itself |
| `Classes/RibbonDesignTime.cs` | The runtime-safe bridge, in the runtime assembly. Runtime code talks to the designer as a plain `object`, so no design-time type is ever named on a runtime path |

The pieces talk to each other through `InternalsVisibleTo("RibbonLib.Design")`, and every designer or
editor is referenced from an attribute as `"…, RibbonLib.Design"`.

A project that references the library gets `RibbonLib.Design.dll` in its output through a small
targets file (`RibbonLib.targets`); the NuGet package ships the assembly under `tools/` and copies it
the same way, so it never becomes a reference of the consuming code.

## Known limitations

- **Design-time only features.** Selection, pills, the Add Control menu and collection editors exist
  only in the Visual Studio designer. Applications use the plain runtime API.
- **Design-tools versions are independent of this package.** The package that provides the compile-time
  designer types (1.6.0) and the design server inside Visual Studio carry different assembly versions;
  the resolver in this fork bridges that. If a future Visual Studio changes the designer API, the
  designer code may need updating — the runtime will be unaffected.
- **No NuGet package.** Build the project or reference it directly.
- Binding a runtime `PropertyGrid` to a `Ribbon` can throw `ArgumentNullException` while it walks the
  ribbon's collections (observed during this port). The designer's own property grid is not affected.

## Credits and license

This library is the work of **Jose Menendez Poo** and the [RibbonWinForms](https://github.com/RibbonWinForms/RibbonWinForms)
contributors, with later maintenance by the OfficeRibbon project. This fork adds the .NET 10 port and the
out-of-process designer support described above.

Licensed under the **Microsoft Public License (Ms-PL)** — see [LICENSE](LICENSE).
