# MiddleClickScrolling

MiddleClickScrolling allows you to scroll by click middle mouse button (scroll wheel button) and move the pointer of the direction to be scrolled. This extension method can be used directly in `ScrollViewer` or ancestor of `ScrollViewer`.

## Syntax

```xaml
<!-- Setting MiddleClickScrolling directely for ScrollViewer -->
<ScrollViewer extensions:ScrollViewerExtensions.EnableMiddleClickScrolling="True">
    <!-- ScrollViewer Content -->
</ScrollViewer>

<!-- Setting MiddleClickScrolling fot the ancestor of ScrollViewer -->
<ListView extensions:ScrollViewerExtensions.EnableMiddleClickScrolling="True">
    <!-- ListView Item -->
</ListView>
```

## Sample Output

![Output](Output-Image.gif)

### Changing Cursor Type

> **NOTE:** Resource file must be manually added to change the cursor type when middle click scrolling.

#### Using Existing Resource File

1. Download [CursorTypeResource.res](https://github.com/Vijay-Nirmal/MiddleClickScrollingHelper/tree/master/MiddleClickScrollingHelper/CursorTypeResource.res) file
2. Move this file into your project's folder
2. Open .csproj file of your project in [Visual Studio Code](https://code.visualstudio.com/) or in any other code editor
3. Added `<Win32Resource>CursorTypeResource.res</Win32Resource>` in the first `<PropertyGroup>`

### Using Your Own Resource File

- You need 9 cursor resource in your resource file
- Your cursor number should be 101 to 109
- Cursor number 101 must be the centre cursor
- Cursor number 102, 103, 104, 105, 106, 107, 108, 109 must be the NorthArror, NorthEastArror, EastArror, SouthEastArror, SouthArror, SouthWestArror, WestArror, NorthWestArror respectively
- Every cursor will be automatically attached to the corresponding direction of scrolling

## Attached Properties

| Property | Type | Description |
| -- | -- | -- |
| EnableMiddleClickScrolling | bool | Set `true` to enable middle click scrolling |