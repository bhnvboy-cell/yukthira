-- 049_edi_transmission_seed.sql
-- EDI Workbench: sample interchange log rows for public."EdiTransmissions"
-- WRITE ONLY — never executed by tooling.
--
-- Enum integer reference (src/YuktiraERP.Core/Enums/EdiEnums.cs):
--   EdiMessageType:       MdReceipt=0, Invoice=810, PurchaseOrder=850, PurchaseOrderAck=855, AdvanceShipNotice=856, FunctionalAck=997
--   EdiTransportProtocol: As2=0, As4=1, Ftp=2, Https=3
--   EdiTransactionStatus: Received=0, Processed=1, Rejected=2, Acknowledged=3, PendingTranslation=4, Translated=5, Applied=6, Error=7

-- 1) Inbound EDIFACT ORDERS (850) — parsed and translated
INSERT INTO public."EdiTransmissions"
    ("Id", "CreatedAt", "MessageType", "Protocol", "Status", "SenderId", "ReceiverId",
     "RawPayload", "AcknowledgmentId", "ErrorMessage", "Direction", "PartnerCode", "DocumentType", "TenantId")
VALUES
    ('1f9d8a44-2c31-4a5e-9b01-6a7c0d11e001',
     '2026-09-03T09:15:00Z',
     850, 0, 5,
     'ACME', 'YUKTIRA',
     'UNB+UNOA:2+ACME+YUKTIRA+260903:0915+ACMSG000931''UNH+1+ORDERS:D:96A:UN''BGM+220+PO-10042+9''DTM+137:20260903:102''NAD+BY+ACME''NAD+SU+YUKTIRA''LIN+1++RM-001:IN''QTY+21:100:EA''PRI+AAA:25.50::EA''LIN+2++RM-002:IN''QTY+21:40:EA''PRI+AAA:12.75::EA''CNT+2:2''UNT+17+1''UNZ+1+ACMSG000931''',
     'MDN-2026-09-03-0001',
     NULL,
     'Inbound',
     'ACME01',
     '850/ORDERS',
     '2eba0f9c-d565-440c-8440-044c68681110')
ON CONFLICT ("Id") DO NOTHING;

-- 2) Outbound EDIFACT DESADV (856) — generated and sent
INSERT INTO public."EdiTransmissions"
    ("Id", "CreatedAt", "MessageType", "Protocol", "Status", "SenderId", "ReceiverId",
     "RawPayload", "AcknowledgmentId", "ErrorMessage", "Direction", "PartnerCode", "DocumentType", "TenantId")
VALUES
    ('1f9d8a44-2c31-4a5e-9b01-6a7c0d11e002',
     '2026-09-05T14:20:00Z',
     856, 0, 1,
     'YUKTIRA', 'ACME',
     'UNB+UNOA:2+YUKTIRA+ACME+260905:1420+YKDSG000418''UNH+1+DESADV:D:96A:UN''BGM+351+DN-88231+9''DTM+137:20260905:102''RFF+ON:PO-10042''NAD+BY+ACME''NAD+SU+YUKTIRA''LIN+1++RM-001:IN''QTY+100:EA''LIN+2++RM-002:IN''QTY+40:EA''UNT+13+1''UNZ+1+YKDSG000418''',
     NULL,
     NULL,
     'Outbound',
     'ACME01',
     '856/DESADV',
     '2eba0f9c-d565-440c-8440-044c68681110')
ON CONFLICT ("Id") DO NOTHING;

-- 3) Inbound EDIFACT INVOIC (810) — applied to ERP
INSERT INTO public."EdiTransmissions"
    ("Id", "CreatedAt", "MessageType", "Protocol", "Status", "SenderId", "ReceiverId",
     "RawPayload", "AcknowledgmentId", "ErrorMessage", "Direction", "PartnerCode", "DocumentType", "TenantId")
VALUES
    ('1f9d8a44-2c31-4a5e-9b01-6a7c0d11e003',
     '2026-09-08T11:05:00Z',
     810, 0, 6,
     'ACME', 'YUKTIRA',
     'UNB+UNOA:2+ACME+YUKTIRA+260908:1105+ACINV001274''UNH+1+INVOIC:D:96A:UN''BGM+380+INV-20917+9''DTM+137:20260908:102''RFF+ON:PO-10042''NAD+BY+YUKTIRA''NAD+SU+ACME''LIN+1++RM-001:IN''QTY+47:100:EA''MOA+203:2550.00''LIN+2++RM-002:IN''QTY+47:40:EA''MOA+203:510.00''MOA+77:3060.00:INR''UNT+15+1''UNZ+1+ACINV001274''',
     'MDN-2026-09-08-0002',
     NULL,
     'Inbound',
     'ACME01',
     '810/INVOIC',
     '2eba0f9c-d565-440c-8440-044c68681110')
ON CONFLICT ("Id") DO NOTHING;

-- 4) Failed outbound X12 purchase order (850) — transport error
INSERT INTO public."EdiTransmissions"
    ("Id", "CreatedAt", "MessageType", "Protocol", "Status", "SenderId", "ReceiverId",
     "RawPayload", "AcknowledgmentId", "ErrorMessage", "Direction", "PartnerCode", "DocumentType", "TenantId")
VALUES
    ('1f9d8a44-2c31-4a5e-9b01-6a7c0d11e004',
     '2026-09-12T18:42:00Z',
     850, 3, 7,
     'YUKTIRA', 'GLOBEX',
     'ISA*00*          *00*          *ZZ*YUKTIRA        *ZZ*GLOBEX         *260912*1842*U*00401*GLB9081234*0*P*>~ST*850*0001~BEG*00*SA*PO-11088**20260912~N1*VN*GLOBEX~N1*BY*YUKTIRA~PO1*1*50*EA*18.25**IN*FG-021~CTT*1~SE*7*0001~IEA*1*GLB9081234~',
     NULL,
     'Transmission failed: HTTP 504 Gateway Timeout posting to https://globex.example.com/edi/as2 (attempt 3 of 3)',
     'Outbound',
     'GLOBEX01',
     '850/ORDERS',
     '2eba0f9c-d565-440c-8440-044c68681110')
ON CONFLICT ("Id") DO NOTHING;

-- 5) Outbound X12 purchase order (850) — acknowledged with a 997
INSERT INTO public."EdiTransmissions"
    ("Id", "CreatedAt", "MessageType", "Protocol", "Status", "SenderId", "ReceiverId",
     "RawPayload", "AcknowledgmentId", "ErrorMessage", "Direction", "PartnerCode", "DocumentType", "TenantId")
VALUES
    ('1f9d8a44-2c31-4a5e-9b01-6a7c0d11e005',
     '2026-09-15T08:30:00Z',
     850, 3, 3,
     'YUKTIRA', 'GLOBEX',
     'ISA*00*          *00*          *ZZ*YUKTIRA        *ZZ*GLOBEX         *260915*0830*U*00401*GLB9084411*0*P*>~ST*850*0001~BEG*00*SA*PO-11104**20260915~N1*VN*GLOBEX~N1*BY*YUKTIRA~PO1*1*200*EA*9.40**IN*RM-114~CTT*1~SE*7*0001~IEA*1*GLB9084411~',
     '997-2026-09-15-0001',
     NULL,
     'Outbound',
     'GLOBEX01',
     '850/ORDERS',
     '2eba0f9c-d565-440c-8440-044c68681110')
ON CONFLICT ("Id") DO NOTHING;

-- 6) Inbound X12 functional acknowledgment (997/CONTRL)
INSERT INTO public."EdiTransmissions"
    ("Id", "CreatedAt", "MessageType", "Protocol", "Status", "SenderId", "ReceiverId",
     "RawPayload", "AcknowledgmentId", "ErrorMessage", "Direction", "PartnerCode", "DocumentType", "TenantId")
VALUES
    ('1f9d8a44-2c31-4a5e-9b01-6a7c0d11e006',
     '2026-09-15T08:35:00Z',
     997, 3, 3,
     'GLOBEX', 'YUKTIRA',
     'ISA*00*          *00*          *ZZ*GLOBEX         *ZZ*YUKTIRA        *260915*0835*U*00401*GLB9084415*0*P*>~ST*997*0001~AK1*PO*0001~AK2*850*0001~AK9*A*1*1~SE*5*0001~IEA*1*GLB9084415~',
     '997-2026-09-15-0002',
     NULL,
     'Inbound',
     'GLOBEX01',
     '997/CONTRL',
     '2eba0f9c-d565-440c-8440-044c68681110')
ON CONFLICT ("Id") DO NOTHING;
