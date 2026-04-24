check for Agents.MD and follow rules defined for project

When I start a conversation with an Issue Number, analyse the task. Create a new feature branch from `dev`. If `dev` is not available, create it from `master` and branch from `dev`.

Use `master` as the protected release branch, `dev` as the shared integration branch, and short-lived feature branches for task work. Do not commit feature work directly to `master` or `dev`.

Do not use default Visual Studio installation, Use Visual Studio 2026 Insider Preview.

After every code change, commit and push the changes.

The original StarWin source code in `Legacy/StarWin2OriginalSource` is reference-only. Do not edit this code directly.

The original Slip Map application code is legacy code. Do not edit it except for changes required to support exporting or migrating data into the new application.

Legacy projects are reference-only. Keep `WPF SlipMap` and `SlipMap Code Library` visible in the solution for historical reference, but do not include them in default builds for the new Starforged Atlas apps.

New Starforged Atlas projects must not directly reference old app projects or compile/include files from `Legacy`, `WPF SlipMap`, or `SlipMap Code Library`. Migrate behavior into new domain/application/infrastructure code instead.
