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
  approvedReviewCommitOids,
  approvedReviewPages,
  body = '',
  closingIssuePages,
  closingIssueNumbers = [],
  connectorReviewRequests = [],
  connectorReactionPages,
  connectorRequestPages,
  projectItems = [],
  projectPages,
  reviewDecision = 'APPROVED',
  reviewThreads = [],
  reviewThreadPages,
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
      reactions: {
        listForIssueComment: async () => {
          throw new Error('listForIssueComment should be called through paginate');
        },
      },
    },
    paginate: async (_method, { comment_id }) => {
      const normalizeReactions = reactions => reactions.map(reaction => ({
        created_at: '2026-08-21T01:00:00Z',
        ...reaction,
      }));
      if (connectorReactionPages) {
        return normalizeReactions(connectorReactionPages.flat());
      }
      const comments = (connectorRequestPages ?? [connectorReviewRequests])
        .flatMap((page, pageIndex) => page.map((node, index) => ({
          ...node,
          databaseId: node.databaseId ?? pageIndex * 100 + index + 1,
        })));
      const comment = comments.find(node => node.databaseId === comment_id);
      return normalizeReactions(comment?.reactions?.nodes ?? []);
    },
    graphql: async (query, variables) => {
      queries.push(query);

      if (query.includes('query ReviewApproval')) {
        return {
          repository: {
            pullRequest: {
              headRefOid: 'head-sha',
              reviewDecision,
            },
          },
        };
      }

      if (query.includes('query ApprovalReviewsPage')) {
        const pages = approvedReviewPages ?? [approvedReviewCommitOids ?? (reviewDecision === 'APPROVED' ? ['head-sha'] : [])];
        const pageIndex = variables.cursor ? Number(variables.cursor.slice(7)) : 0;
        const hasNextPage = pageIndex < pages.length - 1;
        return { repository: { pullRequest: { reviews: {
          pageInfo: { hasNextPage, endCursor: hasNextPage ? `cursor-${pageIndex + 1}` : null },
          nodes: pages[pageIndex].map(review => typeof review === 'string'
            ? { authorCanPushToRepository: true, commit: { oid: review } }
            : review),
        } } } };
      }

      if (query.includes('query ConnectorRequestsPage')) {
        const pages = connectorRequestPages ?? [connectorReviewRequests];
        const pageIndex = variables.cursor ? Number(variables.cursor.slice(7)) : 0;
        const hasNextPage = pageIndex < pages.length - 1;
        return { repository: { pullRequest: { comments: {
          pageInfo: { hasNextPage, endCursor: hasNextPage ? `cursor-${pageIndex + 1}` : null },
          nodes: pages[pageIndex].map((node, index) => ({
            ...node,
            databaseId: node.databaseId ?? pageIndex * 100 + index + 1,
            updatedAt: node.updatedAt ?? '2026-08-21T00:00:00Z',
          })),
        } } } };
      }

      if (query.includes('query ReviewThreadsPage')) {
        const pages = reviewThreadPages ?? [reviewThreads];
        const pageIndex = variables.cursor ? Number(variables.cursor.slice(7)) : 0;
        const hasNextPage = pageIndex < pages.length - 1;
        return {
          repository: {
            pullRequest: {
              reviewThreads: {
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

      if (query.includes('query ClosingIssuesPage')) {
        const pages = closingIssuePages ?? [closingIssueNumbers];
        const pageIndex = variables.cursor ? Number(variables.cursor.slice(7)) : 0;
        const hasNextPage = pageIndex < pages.length - 1;
        return {
          repository: {
            pullRequest: {
              closingIssuesReferences: {
                pageInfo: {
                  hasNextPage,
                  endCursor: hasNextPage ? `cursor-${pageIndex + 1}` : null,
                },
                nodes: pages[pageIndex].map(number => ({
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

  assert.equal(harness.queries.length, 4);
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
    connectorReviewRequests: [{
      body: '@codex review head-sha',
      reactions: { nodes: [{ user: { login: 'chatgpt-codex-connector' } }] },
    }],
    projectItems: [issue(119, 'In review')],
    reviewDecision: 'CHANGES_REQUESTED',
  });

  await executeWorkflow(harness.github, harness.context, harness.core);

  assert.equal(harness.queries.length, 4);
  assert.deepEqual(harness.mutations, []);
  assert.match(harness.messages.info[0], /does not have a current approval signal/);
});

test('treats changes requested as a veto even with an authoritative current-head human approval', async () => {
  const harness = createHarness({
    approvedReviewCommitOids: ['head-sha'],
    closingIssueNumbers: [119],
    projectItems: [issue(119, 'In review')],
    reviewDecision: 'CHANGES_REQUESTED',
  });

  await executeWorkflow(harness.github, harness.context, harness.core);

  assert.deepEqual(harness.mutations, []);
  assert.match(harness.messages.info[0], /does not have a current approval signal/);
});

test('does not accept connector approval bound to a stale head', async () => {
  const harness = createHarness({
    closingIssueNumbers: [119],
    connectorReviewRequests: [{
      body: '@codex review previous-head-sha',
      reactions: { nodes: [{ user: { login: 'chatgpt-codex-connector' } }] },
    }],
    projectItems: [issue(119, 'In review')],
    reviewDecision: null,
  });

  await executeWorkflow(harness.github, harness.context, harness.core);

  assert.deepEqual(harness.mutations, []);
  assert.match(harness.messages.info[0], /does not have a current approval signal/);
});

test('does not accept a stale review command that mentions the current head elsewhere', async () => {
  const harness = createHarness({
    closingIssueNumbers: [119],
    connectorReviewRequests: [{
      body: '@codex review previous-head-sha\nCurrent head: head-sha',
      reactions: { nodes: [{ user: { login: 'chatgpt-codex-connector' } }] },
    }],
    projectItems: [issue(119, 'In review')],
    reviewDecision: null,
  });

  await executeWorkflow(harness.github, harness.context, harness.core);

  assert.deepEqual(harness.mutations, []);
  assert.match(harness.messages.info[0], /does not have a current approval signal/);
});

test('rejects a Connector reaction that predates the latest review-request edit', async () => {
  const harness = createHarness({
    closingIssueNumbers: [119],
    connectorReviewRequests: [{
      body: '@codex review head-sha',
      reactions: { nodes: [{
        created_at: '2026-08-21T00:30:00Z',
        user: { login: 'chatgpt-codex-connector' },
      }] },
      updatedAt: '2026-08-21T00:45:00Z',
    }],
    projectItems: [issue(119, 'In review')],
    reviewDecision: null,
  });

  await executeWorkflow(harness.github, harness.context, harness.core);

  assert.deepEqual(harness.mutations, []);
  assert.match(harness.messages.info[0], /does not have a current approval signal/);
});

test('rejects a Connector reaction with the same timestamp as the latest request edit', async () => {
  const harness = createHarness({
    closingIssueNumbers: [119],
    connectorReviewRequests: [{
      body: '@codex review head-sha',
      reactions: { nodes: [{
        created_at: '2026-08-21T00:45:00Z',
        user: { login: 'chatgpt-codex-connector' },
      }] },
      updatedAt: '2026-08-21T00:45:00Z',
    }],
    projectItems: [issue(119, 'In review')],
    reviewDecision: null,
  });

  await executeWorkflow(harness.github, harness.context, harness.core);

  assert.deepEqual(harness.mutations, []);
  assert.match(harness.messages.info[0], /does not have a current approval signal/);
});

test('finds an unresolved connector thread on a later review-thread page', async () => {
  const harness = createHarness({
    closingIssueNumbers: [119],
    connectorReviewRequests: [{
      body: '@codex review head-sha',
      reactions: { nodes: [{ user: { login: 'chatgpt-codex-connector' } }] },
    }],
    projectItems: [issue(119, 'In review')],
    reviewDecision: null,
    reviewThreadPages: [
      [{ isResolved: true, comments: { nodes: [] } }],
      [{
        isResolved: false,
        comments: { nodes: [{ author: { login: 'chatgpt-codex-connector' } }] },
      }],
    ],
  });

  await executeWorkflow(harness.github, harness.context, harness.core);

  assert.deepEqual(harness.mutations, []);
  assert.equal(harness.queries.filter(query => query.includes('query ReviewThreadsPage')).length, 2);
});

test('allows an authoritative current-head human approval despite connector threads', async () => {
  const harness = createHarness({
    closingIssueNumbers: [119],
    projectItems: [issue(119, 'In review')],
    reviewThreads: [{
      isResolved: false,
      comments: { nodes: [{ author: { login: 'chatgpt-codex-connector' } }] },
    }],
  });

  await executeWorkflow(harness.github, harness.context, harness.core);

  assert.deepEqual(harness.mutations.map(mutation => mutation.itemId), ['item-119']);
});

test('rejects a raw current-head approval that GitHub did not count', async () => {
  const harness = createHarness({
    approvedReviewPages: [[{
      authorCanPushToRepository: false,
      commit: { oid: 'head-sha' },
    }]],
    closingIssueNumbers: [119],
    projectItems: [issue(119, 'In review')],
    reviewDecision: 'REVIEW_REQUIRED',
  });

  await executeWorkflow(harness.github, harness.context, harness.core);

  assert.deepEqual(harness.mutations, []);
});

test('does not combine a stale authoritative approval with an unauthorized current-head review', async () => {
  const harness = createHarness({
    approvedReviewPages: [[
      { authorCanPushToRepository: true, commit: { oid: 'previous-head-sha' } },
      { authorCanPushToRepository: false, commit: { oid: 'head-sha' } },
    ]],
    closingIssueNumbers: [119],
    projectItems: [issue(119, 'In review')],
    reviewDecision: 'APPROVED',
  });

  await executeWorkflow(harness.github, harness.context, harness.core);

  assert.deepEqual(harness.mutations, []);
  assert.match(harness.messages.info[0], /does not have a current approval signal/);
});

test('finds a SHA-bound Connector approval on a later comment page', async () => {
  const harness = createHarness({
    closingIssueNumbers: [119],
    connectorRequestPages: [[], [{
      body: '@codex review head-sha',
      reactions: { nodes: [{ user: { login: 'chatgpt-codex-connector' } }] },
    }]],
    projectItems: [issue(119, 'In review')],
    reviewDecision: null,
  });

  await executeWorkflow(harness.github, harness.context, harness.core);

  assert.deepEqual(harness.mutations.map(mutation => mutation.itemId), ['item-119']);
  assert.equal(harness.queries.filter(query => query.includes('query ConnectorRequestsPage')).length, 2);
});

test('finds the Connector approval after the first reaction page', async () => {
  const harness = createHarness({
    closingIssueNumbers: [119],
    connectorReactionPages: [
      Array.from({ length: 100 }, () => ({ user: { login: 'someone-else' } })),
      [{ user: { login: 'chatgpt-codex-connector' } }],
    ],
    connectorReviewRequests: [{ body: '@codex review head-sha' }],
    projectItems: [issue(119, 'In review')],
    reviewDecision: null,
  });

  await executeWorkflow(harness.github, harness.context, harness.core);

  assert.deepEqual(harness.mutations.map(mutation => mutation.itemId), ['item-119']);
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

test('collects linked issues from every closing-issue page', async () => {
  const harness = createHarness({
    closingIssuePages: [[119], [120]],
    projectItems: [issue(119, 'In review'), issue(120, 'In review')],
  });

  await executeWorkflow(harness.github, harness.context, harness.core);

  assert.deepEqual(
    harness.mutations.map(mutation => mutation.itemId),
    ['item-119', 'item-120'],
  );
  assert.equal(harness.queries.filter(query => query.includes('query ClosingIssuesPage')).length, 2);
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
