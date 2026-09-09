DELETE FROM yuktira_fi.currencies
WHERE "Id" IN (
    SELECT "Id" FROM (
        SELECT "Id", ROW_NUMBER() OVER (PARTITION BY "Code", "TenantId" ORDER BY "CreatedAt") as rn
        FROM yuktira_fi.currencies
    ) t WHERE rn > 1
);

SELECT 'Remaining: ' || COUNT(*)::text FROM yuktira_fi.currencies;
SELECT "Code", COUNT(*) as cnt FROM yuktira_fi.currencies GROUP BY "Code" HAVING COUNT(*) > 1;
