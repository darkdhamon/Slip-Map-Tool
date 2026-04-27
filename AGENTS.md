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
