# Numix icon theme attribution

The PNG icons in this folder were rasterized (48x48) from the
[Numix icon theme](https://github.com/numixproject/numix-icon-theme).

- Source: local clone `F:\workspace\numix-icon-theme` (branch `master`, `Numix/24`)
- License: GNU GPL v3 — https://www.gnu.org/licenses/gpl-3.0.html

Icons used (source path -> local file):

| Local file      | Numix source (Numix/24)              |
|-----------------|---------------------------------------|
| open.png        | places/folder-open.svg                |
| add.png         | actions/document-new.svg              |
| extract.png     | actions/archive-extract.svg           |
| test.png        | actions/dialog-ok.svg                 |
| view.png        | actions/view-preview.svg              |
| delete.png      | actions/edit-delete.svg               |
| find.png        | actions/system-search.svg             |
| info.png        | status/dialog-information.svg         |
| save.png        | actions/document-save.svg             |
| settings.png    | actions/configure.svg                 |
| help.png        | actions/help-about.svg                |
| sort-up.png     | actions/view-sort-ascending.svg       |
| sort-down.png   | actions/view-sort-descending.svg      |

Regenerate with: `pwsh build/update-numix-icons.ps1`
