-- 019_bank_import.sql
-- Bank Statement Import tables

CREATE TABLE IF NOT EXISTS yuktira_fi.bank_statements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    statement_number VARCHAR(100) NOT NULL,
    account_id UUID NOT NULL,
    statement_date TIMESTAMP NOT NULL,
    import_date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    source VARCHAR(50) NOT NULL DEFAULT 'MANUAL',
    status VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    total_debits DECIMAL(18,2) NOT NULL DEFAULT 0,
    total_credits DECIMAL(18,2) NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL
);

CREATE TABLE IF NOT EXISTS yuktira_fi.bank_statement_lines (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    statement_id UUID NOT NULL,
    transaction_date TIMESTAMP NOT NULL,
    value_date TIMESTAMP NULL,
    description VARCHAR(500) NOT NULL,
    reference VARCHAR(255) NOT NULL,
    debit DECIMAL(18,2) NOT NULL DEFAULT 0,
    credit DECIMAL(18,2) NOT NULL DEFAULT 0,
    balance DECIMAL(18,2) NOT NULL DEFAULT 0,
    matched_payment_id UUID NULL,
    matched_journal_id UUID NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'UNMATCHED',
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL
);

CREATE TABLE IF NOT EXISTS yuktira_fi.bank_reconciliations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    account_code VARCHAR(100) NOT NULL,
    account_name VARCHAR(255) NOT NULL,
    statement_date TIMESTAMP NOT NULL,
    statement_balance DECIMAL(18,2) NOT NULL DEFAULT 0,
    ledger_balance DECIMAL(18,2) NOT NULL DEFAULT 0,
    difference DECIMAL(18,2) NOT NULL DEFAULT 0,
    status VARCHAR(50) NOT NULL DEFAULT 'Draft',
    notes VARCHAR(1000) NOT NULL DEFAULT '',
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL
);

CREATE INDEX IF NOT EXISTS idx_bank_statements_tenant ON yuktira_fi.bank_statements(tenant_id);
CREATE INDEX IF NOT EXISTS idx_bank_statements_account ON yuktira_fi.bank_statements(account_id);
CREATE INDEX IF NOT EXISTS idx_bank_statement_lines_statement ON yuktira_fi.bank_statement_lines(statement_id);
CREATE INDEX IF NOT EXISTS idx_bank_statement_lines_status ON yuktira_fi.bank_statement_lines(status);
CREATE INDEX IF NOT EXISTS idx_bank_reconciliations_tenant ON yuktira_fi.bank_reconciliations(tenant_id);
