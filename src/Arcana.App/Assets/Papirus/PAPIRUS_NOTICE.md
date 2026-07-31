# Papirus icon theme attribution

The PNG icons in this folder were rasterized (48x48) from the
[Papirus icon theme](https://github.com/PapirusDevelopmentTeam/papirus-icon-theme)
by the Papirus Development Team.

- Source: https://github.com/PapirusDevelopmentTeam/papirus-icon-theme (branch `master`, `Papirus/24x24`)
- License: GNU GPL v3 — https://www.gnu.org/licenses/gpl-3.0.html
- Authors: see https://github.com/PapirusDevelopmentTeam/papirus-icon-theme/blob/master/AUTHORS

Icons used (source path -> local file):

| Local file        | Papirus source (24x24)                    |
|-------------------|-------------------------------------------|
| open.png          | places/folder-open.svg                    |
| add.png           | actions/document-new.svg                  |
| extract.png       | actions/archive-extract.svg               |
| test.png          | actions/dialog-ok.svg                     |
| view.png          | actions/view-preview.svg                  |
| delete.png        | actions/edit-delete.svg                   |
| find.png          | actions/system-search.svg                 |
| info.png          | status/dialog-information.svg             |
| folder.png        | places/folder.svg                         |
| file-generic.png  | mimetypes/text-x-generic.svg              |
| file-archive.png  | mimetypes/application-x-archive.svg       |
| file-image.png    | mimetypes/image-x-generic.svg             |
| file-code.png     | mimetypes/text-x-script.svg               |
| file-media.png    | mimetypes/video-x-generic.svg             |
| file-doc.png      | mimetypes/x-office-document.svg           |
| file-rar.png      | mimetypes/application-x-rar.svg           |
| sort-up.png       | actions/view-sort-ascending.svg           |
| sort-down.png     | actions/view-sort-descending.svg          |

Regenerate with: `pwsh build/update-papirus-icons.ps1`
