const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

const workflowPath = path.join(__dirname, '..', 'complete-dev-merge-project-item.yml');
const workflowLines = fs.readFileSync(workflowPath, 'utf8').split(/\r?\n/);
const scriptMarkerIndex = workflowLines.indexOf('          script: |');

assert.notEqual(scriptMarkerIndex, -1, 'workflow must contain the github-script block');

const script = workflowLines
  .slice(scriptMarkerIndex + 1)
  .map(line => line.startsWith('            ') ? line.slice(12) : '')
  .join('\n');
const AsyncFunction = Object.getPrototypeOf(async function () {}).constructor;
const executeWorkflow = new AsyncFunction('github', 'context', 'core', script);

function issue(number, status, id = `item-${number}`) {
  return {
    id,
    content: {
      number,
      repository: { nameWithOwner: 'DarkDhamon/Starforged-Atlas' },
    },
    fieldValueByName: status ? { name: status } : null,
  };
}

function createHarness({
  associatedPullRequests,
  body = '',
  closingIssueNumbers = [],
  projectItems = [],
  projectPages,
  reactions = [],
  reviewDecision = 'APPROVED',
  reviewThreads = [],
} = {}) {
  const mutations = [];
  const queries = [];
  const messages = { failures: [], info: [], warnings: [] };

  const github = {
    rest: {
      repos: {
        listPullRequestsAssociatedWithCommit: async () => ({
          data: associatedPullRequests ?? [{
            base: { ref: 'dev' },
            body,
            merge_commit_sha: 'merge-sha',
            merged_at: '2026-08-21T00:00:00Z',
            number: 132,
          }],
        }),
      },
    },
    graphql: async (query, variables) => {
      queries.push(query);

      if (query.includes('pullRequest(number:')) {
        return {
          repository: {
            pullRequest: {
              reviewDecision,
              reactions: { nodes: reactions },
              reviewThreads: { nodes: reviewThreads },
              closingIssuesReferences: {
                nodes: closingIssueNumbers.map(number => ({
                  number,
                  repository: { nameWithOwner: 'DarkDhamon/Starforged-Atlas' },
                })),
              },
            },
          },
        };
      }

      if (query.includes('projectV2(number:')) {
        const pages = projectPages ?? [projectItems];
        const pageIndex = variables.cursor ? Number(variables.cursor.slice(7)) : 0;
        const hasNextPage = pageIndex < pages.length - 1;
        return {
          user: {
            projectV2: {
              id: 'project-id',
              title: 'Starforged Atlas Task Board',
              fields: {
                nodes: [{
                  id: 'status-field-id',
                  name: 'Status',
                  options: [
                    { id: 'in-review-option-id', name: 'In review' },
                    { id: 'done-option-id', name: 'Done' },
                  ],
                }],
              },
              items: {
                pageInfo: {
                  hasNextPage,
                  endCursor: hasNextPage ? `cursor-${pageIndex + 1}` : null,
                },
                nodes: pages[pageIndex],
              },
            },
          },
        };
      }

      if (query.includes('updateProjectV2ItemFieldValue')) {
        mutations.push(variables);
        return { updateProjectV2ItemFieldValue: { projectV2Item: { id: variables.itemId } } };
      }

      throw new Error(`Unexpected GraphQL operation: ${query}`);
    },
  };

  const context = {
    repo: { owner: 'DarkDhamon', repo: 'Starforged-Atlas' },
    sha: 'merge-sha',
  };
  const core = {
    info: message => messages.info.push(message),
    setFailed: message => messages.failures.push(message),
    warning: message => messages.warnings.push(message),
  };

  return { context, core, github, messages, mutations, queries };
}

test('does not update project items when the merged pull request is unapproved', async () => {
  const harness = createHarness({
    closingIssueNumbers: [119],
    projectItems: [issue(119, 'In review')],
    reviewDecision: null,
  });

  await executeWorkflow(harness.github, harness.context, harness.core);

  assert.equal(harness.queries.length, 1);
  assert.deepEqual(harness.mutations, []);
  assert.match(harness.messages.info[0], /does not have a current approval signal/);
});

test('treats a dev push without an associated merged pull request as a no-op', async () => {
  const harness = createHarness({ associatedPullRequests: [] });

  await executeWorkflow(harness.github, harness.context, harness.core);

  assert.deepEqual(harness.queries, []);
  assert.deepEqual(harness.mutations, []);
  assert.match(harness.messages.info[0], /not the merge commit of a pull request/);
});

test('treats changes requested as a veto even when the connector reacted with approval', async () => {
  const harness = createHarness({
    closingIssueNumbers: [119],
    projectItems: [issue(119, 'In review')],
    reactions: [{ user: { login: 'chatgpt-codex-connector' } }],
    reviewDecision: 'CHANGES_REQUESTED',
  });

  await executeWorkflow(harness.github, harness.context, harness.core);

  assert.equal(harness.queries.length, 1);
  assert.deepEqual(harness.mutations, []);
  assert.match(harness.messages.info[0], /does not have a current approval signal/);
});

test('uses only explicit issue sections when falling back to pull request body references', async () => {
  const harness = createHarness({
    body: '## Affected issues\n- #119\n\n## Notes\nDepends on #123',
    projectItems: [issue(119, 'In review'), issue(123, 'In review')],
  });

  await executeWorkflow(harness.github, harness.context, harness.core);

  assert.deepEqual(harness.mutations.map(mutation => mutation.itemId), ['item-119']);
});

test('reports a linked issue that has no existing project item', async () => {
  const harness = createHarness({ closingIssueNumbers: [119] });

  await executeWorkflow(harness.github, harness.context, harness.core);

  assert.deepEqual(harness.mutations, []);
  assert.deepEqual(harness.messages.failures, [
    'Linked issue #119 has no existing item on Starforged Atlas Task Board.',
  ]);
});

test('does not update a partial match when another linked issue is missing from the board', async () => {
  const harness = createHarness({
    closingIssueNumbers: [119, 120],
    projectItems: [issue(119, 'In review')],
  });

  await executeWorkflow(harness.github, harness.context, harness.core);

  assert.deepEqual(harness.mutations, []);
  assert.deepEqual(harness.messages.failures, [
    'Linked issue #120 has no existing item on Starforged Atlas Task Board.',
  ]);
});

test('finds and updates a linked issue on a later project page', async () => {
  const harness = createHarness({
    closingIssueNumbers: [119],
    projectPages: [
      [issue(118, 'Done')],
      [issue(119, 'In review')],
    ],
  });

  await executeWorkflow(harness.github, harness.context, harness.core);

  assert.deepEqual(harness.mutations.map(mutation => mutation.itemId), ['item-119']);
  assert.equal(harness.queries.filter(query => query.includes('projectV2(number:')).length, 2);
});

test('updates only In review items and preserves Done or unexpected statuses', async () => {
  const harness = createHarness({
    closingIssueNumbers: [119, 120, 121],
    projectItems: [
      issue(119, 'Done'),
      issue(120, 'Backlog'),
      issue(121, 'In review'),
    ],
  });

  await executeWorkflow(harness.github, harness.context, harness.core);

  assert.deepEqual(harness.mutations.map(mutation => mutation.itemId), ['item-121']);
  assert.ok(harness.messages.info.some(message => message.includes('#119 is already Done')));
  assert.ok(harness.messages.warnings.some(message => message.includes('#120 is Backlog')));
});
