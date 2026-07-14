# License Transition Implementation Notes

The migration is implemented through repository files and automated gates rather than a destructive rewrite of Git history.

- `archive/apache-2.0-final` preserves the final historical Apache revision.
- `main` after the transition uses `GPL-3.0-or-later` for current community source and releases.
- Commercial rights require a separate executed agreement.
- Current packages contain only the current GPL license plus notices and historical-boundary documentation.
- CI, Pages, build, installer, and release paths verify the model before publication.

Historical commit messages and earlier releases may continue to describe the license that applied at that time.