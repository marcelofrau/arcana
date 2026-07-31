# Tango icon theme attribution

The PNG icons in this folder were rasterized (48x48) from the
[Tango icon theme](https://tango.freedesktop.org/Tango_Icon_Theme_Guidelines)
as mirrored at [stephenc/tango-icon-theme](https://github.com/stephenc/tango-icon-theme).

- Source: https://github.com/stephenc/tango-icon-theme (branch `master`, `scalable/`)
- License: Public domain (Tango base icons) / GNU GPL v3 (theme), CC-BY-SA where noted — see COPYING in the source repo
- Only the icons whose slot the built-in "Tango" theme covers are included;
  the rest fall back to the Material set.

Icons used (source path -> local file):

| Local file      | Tango source (scalable)               |
|-----------------|---------------------------------------|
| add.png         | actions/list-add.svg                  |
| delete.png      | actions/edit-delete.svg               |
| find.png        | actions/system-search.svg             |
| info.png        | status/dialog-information.svg         |
| save.png        | actions/document-save.svg             |

Regenerate with: `pwsh build/update-tango-icons.ps1`
