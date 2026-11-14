# Design-time item collection editors

The classes in this folder are **not used to edit collections** by the .NET out-of-process Windows
Forms designer, and adding an override here will not change what **Add** does. This note exists so
that nobody spends another round of debugging on them.

## What the designer actually does

1. The property grid asks the client half of the designer
   (`Microsoft.DotNet.DesignTools.Client.Editors.CollectionEditor`) to edit the collection property.
2. That client editor always creates the server-side editor through the factory name
   `DefaultCollectionEditor`, which is the plain `Microsoft.DotNet.DesignTools.Editors.CollectionEditor`.
   The client asks for a different factory only when a matching *client-side* editor type is
   registered for it, and this assembly has no client half.
3. The plain editor decides what Add creates in `CreateCollectionItemType()`: the first public
   property named `Item` or `Items` on the collection type. `RibbonCollectionBase<T>` exposes that
   as `T`, and for every ribbon item collection `T` was the abstract `RibbonItem`, so Add tried to
   instantiate an abstract class and failed.

## Where Add is fixed instead

Each ribbon item collection hides that indexer with a concrete item type, so the static type of the
`Item` property is what Add creates:

| Collection | Item created by Add |
| --- | --- |
| `RibbonItemCollection` (panel items, button / button list / combo box drop-downs, item group, quick access toolbar drop-down button) | `RibbonButton` |
| `RibbonQuickAccessToolbarItemCollection`, `RibbonButtonCollection` (inherit it from the row above) | `RibbonButton` |
| `RibbonOrbMenuItemCollection` | `RibbonOrbMenuItem` |
| `RibbonOrbRecentItemCollection` | `RibbonOrbRecentItem` |
| `RibbonOrbOptionButtonCollection` | `RibbonOrbOptionButton` |

To change what a collection editor adds, change that concrete indexer (or add one when a new
collection type needs it). `CreateNewItemTypes` and `CreateInstance` in this folder have no effect:
the plain editor offers exactly one item type and creates it itself.

## Why the classes stay

The `[Editor("...CollectionEditor, RibbonLib.Design", typeof(UITypeEditor))]` attributes in
`RibbonLib` and the `EditorAttribute`s in `RibbonDesigner.PreFilterProperties` name these types, and
removing them has not been verified against Visual Studio's designer. They are design-time only,
never affect runtime behaviour, and never run while a collection is being edited.

If the designer ever starts supporting custom collection editors without a client-side counterpart,
these are the classes that would come back into play - and then the collection indexer becomes the
fallback rather than the mechanism.
