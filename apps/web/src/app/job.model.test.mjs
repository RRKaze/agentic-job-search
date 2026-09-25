import { test } from 'node:test';
import assert from 'node:assert/strict';
import { isSavedOpportunity } from './job.model.ts';
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
});
