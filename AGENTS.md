check for Agents.MD and follow rules defined for project

When I start a conversation with an Issue Number, analyse the task and research the current state of the project before doing any coding work.

The first workflow step for an issue-driven conversation is:
1. Switch to `dev`
2. Pull the latest changes from `origin/dev`
3. Create a new feature or bug-fix branch for that issue from `dev`
4. Research the current implementation and what needs to be done at the project's current state

If `dev` does not exist yet, create it from `master`, then create the feature or bug-fix branch from `dev`.

Do not start coding for an issue-driven conversation until I explicitly ask you to begin the coding process.

GitHub project workflow uses the `Starforged Atlas Task Board`.

When an issue is in `Backlog`, move it to `Ready` after research is complete.

GitHub release workflow:
1. Create releases from `master` only.
2. Pull the latest `origin/master` before building release artifacts.
3. Build the portable desktop package with `scripts/New-PortableDesktopPackage.ps1`.
4. Upload `Starforged-Atlas-Portable.zip` as the primary release asset.
5. Do not re-upload the Delcora sector import zip if it has not changed. In release notes, direct users to download `Delcora.Sector.zip` from the previous release instead. Only upload it again when the file changes or when a fully self-contained onboarding release is explicitly requested.
6. Release notes must include a changelog covering what changed since the previous release.
7. Follow the same release note format used by `2026-04-26.2 Developer Preview`, with these sections in this order:
   - `Developer Preview release for {date}.`
   - `Included assets`
   - `Built from`
   - `Changelog since {previous release}`
   - `Highlights`
   - `Issues included`
   - `Commit summary`
   - `Notes`
8. In the `Included assets` section, list the portable desktop package zip and note that `Delcora.Sector.zip` should be downloaded from the previous release when it is not re-uploaded for the current release.
9. When promoting `dev` to `master`, update the desktop application version before the pull request is merged so the version change is included in the `dev` -> `master` pull request.
10. The desktop application version format is `{year}-{two-digit-month}-{two-digit-day}.{release-index}-developer-preview` for the informational release tag, where the first release of a day uses release index `0`, the second uses `1`, and so on.
11. For the corresponding assembly/file/package numeric version, use `{year}.{month}.{day}.{release-index}`.
12. Until explicitly changed, the release name suffix is `Developer Preview`.
13. Include the release name in the `dev` -> `master` pull request title/body when preparing the promotion.
14. After the `dev` -> `master` pull request is fully merged, create the GitHub release from `master` using that version.

When I explicitly tell you to implement the changes, move the issue from `Ready` to `In progress`.

When I tell you to promote the work to `dev`, move the issue from `In progress` to `In review`.

When a pull request that promotes work to `main` is completed, move all issues connected to that pull request from `In review` to `Done`.

Use `master` as the protected release branch, `dev` as the shared integration branch, and short-lived feature branches for task work. Do not commit feature work directly to `master` or `dev`.

Do not use default Visual Studio installation, Use Visual Studio 2026 Insider Preview.

When working on an issue branch, use a port in the `10000` range for local running and debugging. The last three digits of the port should match the issue number being worked on. For example, issue `123` should use port `10123`. If the issue number is longer than three digits, use the last three digits of the issue number.

After every code change, commit and push the changes.

All new code requires unit tests that cover the new behavior before moving on to the next task.

Testing workflow terms:
- Unit test: run the automated unit/integration test suite requested for the relevant scope.
- QA test: direct verification is allowed, including jumping straight to a page or route to check behavior.
- UAT test: perform user acceptance testing by navigating through the website like a normal user would, using mouse and keyboard interactions rather than jumping directly to target pages/routes unless the user explicitly allows that shortcut.

The original StarWin source code in `Legacy/StarWin2OriginalSource` is reference-only. Do not edit this code directly.

The original Slip Map application code is legacy code. Do not edit it except for changes required to support exporting or migrating data into the new application.

Legacy projects are reference-only. Keep `WPF SlipMap` and `SlipMap Code Library` visible in the solution for historical reference, but do not include them in default builds for the new Starforged Atlas apps.

New Starforged Atlas projects must not directly reference old app projects or compile/include files from `Legacy`, `WPF SlipMap`, or `SlipMap Code Library`. Migrate behavior into new domain/application/infrastructure code instead.
