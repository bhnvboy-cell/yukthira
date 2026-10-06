-- =============================================================================
-- 048_exchange_rate_seed.sql
-- Task 4 (Exchange Rate Sync Service + Currency Rates UI) - optional seed data.
--
-- WRITE ONLY: this script is provided for the integrator to review and run
-- manually. It has NOT been executed by the authoring tooling.
--
-- Table:  yuktira_fi.exchange_rates
-- Columns follow the EF Core model snapshot for
-- YuktiraERP.Infrastructure.Data.Entities.ExchangeRateEntity
-- ("Id","TenantId","FromCurrency","ToCurrency","Rate","EffectiveFrom",
--  "EffectiveTo","Source","CreatedAt","UpdatedAt") - Rate is decimal(18,6).
-- >>> Integrator: verify the column list against a live schema before running. <<<
--
-- Design note: only base -> currency rows are stored (Source = 'Manual' here).
-- Inverse and cross rates are computed at read time by CurrencyService.GetRateAsync
-- (exact pair, then inverse) and by ExchangeRateMath (common-base cross), so no
-- inverse rows are seeded.
-- =============================================================================

INSERT INTO yuktira_fi.exchange_rates
    ("Id", "TenantId", "FromCurrency", "ToCurrency", "Rate",
     "EffectiveFrom", "EffectiveTo", "Source", "CreatedAt", "UpdatedAt")
VALUES
    ('f1e0a1b2-0001-4a01-8c1d-000000000001', '2eba0f9c-d565-440c-8440-044c68681110', 'EUR', 'USD', 1.163700, CURRENT_DATE, NULL, 'Manual', CURRENT_TIMESTAMP, NULL),
    ('f1e0a1b2-0002-4a01-8c1d-000000000002', '2eba0f9c-d565-440c-8440-044c68681110', 'EUR', 'INR', 96.500000, CURRENT_DATE, NULL, 'Manual', CURRENT_TIMESTAMP, NULL),
    ('f1e0a1b2-0003-4a01-8c1d-000000000003', '2eba0f9c-d565-440c-8440-044c68681110', 'EUR', 'GBP', 0.865000, CURRENT_DATE, NULL, 'Manual', CURRENT_TIMESTAMP, NULL),
    ('f1e0a1b2-0004-4a01-8c1d-000000000004', '2eba0f9c-d565-440c-8440-044c68681110', 'EUR', 'JPY', 172.300000, CURRENT_DATE, NULL, 'Manual', CURRENT_TIMESTAMP, NULL),
    ('f1e0a1b2-0005-4a01-8c1d-000000000005', '2eba0f9c-d565-440c-8440-044c68681110', 'EUR', 'CHF', 0.935000, CURRENT_DATE, NULL, 'Manual', CURRENT_TIMESTAMP, NULL),
    ('f1e0a1b2-0006-4a01-8c1d-000000000006', '2eba0f9c-d565-440c-8440-044c68681110', 'EUR', 'CNY', 8.320000, CURRENT_DATE, NULL, 'Manual', CURRENT_TIMESTAMP, NULL),
    ('f1e0a1b2-0007-4a01-8c1d-000000000007', '2eba0f9c-d565-440c-8440-044c68681110', 'EUR', 'AUD', 1.760000, CURRENT_DATE, NULL, 'Manual', CURRENT_TIMESTAMP, NULL),
    ('f1e0a1b2-0008-4a01-8c1d-000000000008', '2eba0f9c-d565-440c-8440-044c68681110', 'EUR', 'CAD', 1.605000, CURRENT_DATE, NULL, 'Manual', CURRENT_TIMESTAMP, NULL),
    ('f1e0a1b2-0009-4a01-8c1d-000000000009', '2eba0f9c-d565-440c-8440-044c68681110', 'EUR', 'SGD', 1.520000, CURRENT_DATE, NULL, 'Manual', CURRENT_TIMESTAMP, NULL),
    ('f1e0a1b2-000a-4a01-8c1d-00000000000a', '2eba0f9c-d565-440c-8440-044c68681110', 'EUR', 'HKD', 9.070000, CURRENT_DATE, NULL, 'Manual', CURRENT_TIMESTAMP, NULL)
ON CONFLICT DO NOTHING;

-- Verify:
--   SELECT "FromCurrency","ToCurrency","Rate","Source","EffectiveFrom"
--   FROM yuktira_fi.exchange_rates
--   WHERE "TenantId" = '2eba0f9c-d565-440c-8440-044c68681110'
--   ORDER BY "FromCurrency","ToCurrency";
