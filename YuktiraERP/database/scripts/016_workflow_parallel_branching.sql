-- Migration 016: Add parallel branching support to workflow engine
-- Adds BranchType column to workflow_edges and ActiveTokens column to workflow_instances

ALTER TABLE yuktira_workflow.workflow_edges
    ADD COLUMN IF NOT EXISTS branch_type VARCHAR(20) NOT NULL DEFAULT 'SEQUENTIAL';

COMMENT ON COLUMN yuktira_workflow.workflow_edges.branch_type IS 'Edge type: SEQUENTIAL (default), PARALLEL (AND-split/AND-join), CONDITIONAL (exclusive-or)';

ALTER TABLE yuktira_workflow.workflow_instances
    ADD COLUMN IF NOT EXISTS active_tokens JSONB NOT NULL DEFAULT '[]';

COMMENT ON COLUMN yuktira_workflow.workflow_instances.active_tokens IS 'JSON array of active node IDs for parallel branch tracking. Empty array when workflow is sequential or completed.';
