import { test } from 'node:test';
import assert from 'node:assert/strict';
import { detailPoints, isSavedOpportunity, sortByDiscoveredDate } from './job.model.ts';
import { workflowStatusOptions } from './features/applications/application-workflow.model.ts';

const job = { id: 'fixture', title: 'Engineer', company: 'Example', location: 'Remote', createdAt: '2026-09-22T12:00:00Z' };

test('saved imported leads and manually saved jobs appear as opportunities', () => {
  assert.equal(isSavedOpportunity({ ...job, trackingStage: 'lead' }), true);
  assert.equal(isSavedOpportunity({ ...job, trackingStage: 'LEAD' }), true);
  assert.equal(isSavedOpportunity({ ...job, lifecycleState: 'Analyzed' }), true);
});

test('closed and applied stages never appear merely because an application record is missing', () => {
  for (const trackingStage of ['unavailable', 'ineligible', 'rejected', 'withdrawn', 'applied', 'interviewing', 'offer', 'unknown']) {
    assert.equal(isSavedOpportunity({ ...job, trackingStage }), false, trackingStage);
  }
});

test('an application record excludes a job even when its tracking stage is missing or inconsistent', () => {
  for (const trackingStage of [undefined, 'lead', 'applied']) {
    assert.equal(isSavedOpportunity({ ...job, trackingStage, application: { state: 'Submitted' } }), false);
  }
});

test('workflow options match the allowed application transitions', () => {
  assert.deepEqual(workflowStatusOptions({ ...job, trackingStage: 'applied', application: { state: 'Submitted' } }).map(({ value }) => value),
    ['applied', 'interviewing', 'rejected', 'withdrawn']);
  assert.deepEqual(workflowStatusOptions({ ...job, trackingStage: 'interviewing', application: { state: 'Interview' } }).map(({ value }) => value),
    ['interviewing', 'applied', 'offer', 'rejected', 'withdrawn']);
  assert.deepEqual(workflowStatusOptions({ ...job, trackingStage: 'lead' }).map(({ value }) => value), ['lead', 'applied']);
  assert.deepEqual(workflowStatusOptions({ ...job, trackingStage: 'rejected', application: { state: 'Rejected' } }).map(({ value }) => value), ['rejected']);
});

test('fit and caution text becomes readable bullet points without splitting technical abbreviations', () => {
  assert.deepEqual(detailPoints('Works with .NET services. Strong C# background; remote role\n• Team fit.'),
    ['Works with .NET services.', 'Strong C# background', 'remote role', 'Team fit.']);
});

test('saved opportunities sort by discovered date in either direction without mutating the source list', () => {
  const jobs = [
    { ...job, id: 'middle', statusDate: '2026-10-12' },
    { ...job, id: 'oldest', statusDate: '2026-09-30' },
    { ...job, id: 'newest', statusDate: '2026-10-15' }
  ];
  assert.deepEqual(sortByDiscoveredDate(jobs, 'desc').map(({ id }) => id), ['newest', 'middle', 'oldest']);
  assert.deepEqual(sortByDiscoveredDate(jobs, 'asc').map(({ id }) => id), ['oldest', 'middle', 'newest']);
  assert.deepEqual(jobs.map(({ id }) => id), ['middle', 'oldest', 'newest']);
});
