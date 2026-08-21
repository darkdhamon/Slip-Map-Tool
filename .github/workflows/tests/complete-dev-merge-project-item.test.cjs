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
  body = '',
  closingIssueNumbers = [],
  projectItems = [],
  reactions = [],
  reviewDecision = 'APPROVED',
  reviewThreads = [],
} = {}) {
  const mutations = [];
  const queries = [];
  const messages = { failures: [], info: [], warnings: [] };

  const github = {
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
                pageInfo: { hasNextPage: false, endCursor: null },
                nodes: projectItems,
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
    payload: { pull_request: { body, number: 132 } },
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
    'None of the linked issues has an existing item on Starforged Atlas Task Board.',
  ]);
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
