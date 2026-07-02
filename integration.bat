@echo off
cd /d "%~dp0\YuktiraERP"
title Yuktira ERP - Integration Hub
set API_BASE=http://localhost:5000/api

:menu
cls
echo ============================================
echo   YUKTIRA ERP - INTEGRATION HUB
echo ============================================
echo.
echo   API: %API_BASE%
echo.
echo  Commands:
echo    1   Login and get token
echo    2   Register a webhook
echo    3   List webhooks
echo    4   Enqueue a message
echo    5   Process pending messages
echo    6   View dead-letter queue
echo    7   Requeue from dead-letter
echo    8   Validate API client
echo    9   Convert EDI (EDIFACT)
echo   10   Dispatch an event
echo   11   Check health
echo   exit  Close
echo.

set /p CMD="Select command (1-11): "

if "%CMD%"=="1" goto :login
if "%CMD%"=="2" goto :register-webhook
if "%CMD%"=="3" goto :list-webhooks
if "%CMD%"=="4" goto :enqueue
if "%CMD%"=="5" goto :process
if "%CMD%"=="6" goto :dead-letter
if "%CMD%"=="7" goto :requeue
if "%CMD%"=="8" goto :validate-client
if "%CMD%"=="9" goto :edi-convert
if "%CMD%"=="10" goto :dispatch
if "%CMD%"=="11" goto :health
if /i "%CMD%"=="exit" exit /b
goto :menu

:login
echo.
echo  --- Login ---
set /p USER="User ID [jdoe]: "
if "%USER%"=="" set USER=jdoe
set /p PASS="Password [yuktira123]: "
if "%PASS%"=="" set PASS=yuktira123
set /p TENANT="Client Number [1000]: "
if "%TENANT%"=="" set TENANT=1000

curl -s -X POST "%API_BASE%/auth/login" ^
  -H "Content-Type: application/json" ^
  -d "{\"clientNumber\":\"%TENANT%\",\"userId\":\"%USER%\",\"password\":\"%PASS%\"}"

echo.
echo  --- Copy the accessToken and use it below ---
pause
goto :menu

:register-webhook
echo.
echo  --- Register Webhook ---
set /p TOKEN="Bearer token: "
set /p NAME="Webhook name [Test Hook]: "
if "%NAME%"=="" set NAME=Test Hook
set /p EVENT="Event type [order.created]: "
if "%EVENT%"=="" set EVENT=order.created
set /p URL="Target URL [https://webhook.site/test]: "
if "%URL%"=="" set URL=https://webhook.site/test

curl -s -X POST "%API_BASE%/integration/webhooks" ^
  -H "Authorization: Bearer %TOKEN%" ^
  -H "Content-Type: application/json" ^
  -d "{\"name\":\"%NAME%\",\"eventType\":\"%EVENT%\",\"targetUrl\":\"%URL%\"}"
echo.
pause
goto :menu

:list-webhooks
echo.
echo  --- List Webhooks ---
set /p TOKEN="Bearer token: "
curl -s "%API_BASE%/integration/webhooks" -H "Authorization: Bearer %TOKEN%"
echo.
pause
goto :menu

:enqueue
echo.
echo  --- Enqueue Message ---
set /p TOKEN="Bearer token: "
set /p MSG_TYPE="Message type [order.sync]: "
if "%MSG_TYPE%"=="" set MSG_TYPE=order.sync
set /p ORDER_ID="Order ID [SO-50001]: "
if "%ORDER_ID%"=="" set ORDER_ID=SO-50001

curl -s -X POST "%API_BASE%/integration/queue/enqueue" ^
  -H "Authorization: Bearer %TOKEN%" ^
  -H "Content-Type: application/json" ^
  -d "{\"messageType\":\"%MSG_TYPE%\",\"payload\":{\"orderId\":\"%ORDER_ID%\"},\"targetSystem\":\"EXTERNAL_ERP\"}"
echo.
pause
goto :menu

:process
echo.
echo  --- Process Queue ---
set /p TOKEN="Bearer token: "
curl -s -X POST "%API_BASE%/integration/queue/process" -H "Authorization: Bearer %TOKEN%"
echo.
pause
goto :menu

:dead-letter
echo.
echo  --- View Dead-Letter Queue ---
set /p TOKEN="Bearer token: "
curl -s "%API_BASE%/integration/queue/dead-letter" -H "Authorization: Bearer %TOKEN%"
echo.
pause
goto :menu

:requeue
echo.
echo  --- Requeue from Dead-Letter ---
set /p TOKEN="Bearer token: "
set /p DL_ID="Dead-letter ID: "
curl -s -X POST "%API_BASE%/integration/queue/requeue/%DL_ID%" -H "Authorization: Bearer %TOKEN%"
echo.
pause
goto :menu

:validate-client
echo.
echo  --- Validate API Client ---
set /p CLIENT_ID="Client ID [client-1]: "
if "%CLIENT_ID%"=="" set CLIENT_ID=client-1
set /p CLIENT_SECRET="Client Secret [secret-1]: "
if "%CLIENT_SECRET%"=="" set CLIENT_SECRET=secret-1

curl -s -X POST "%API_BASE%/integration/validate" ^
  -H "Content-Type: application/json" ^
  -d "{\"clientId\":\"%CLIENT_ID%\",\"clientSecret\":\"%CLIENT_SECRET%\"}"
echo.
pause
goto :menu

:edi-convert
echo.
echo  --- Convert EDI ---
set /p TOKEN="Bearer token: "
curl -s -X POST "%API_BASE%/integration/edi/convert" ^
  -H "Authorization: Bearer %TOKEN%" ^
  -H "Content-Type: application/json" ^
  -d "{\"ediContent\":\"UNH+1+ORDERS:D:96A:UN\",\"sourceFormat\":\"EDIFACT\",\"targetFormat\":\"JSON\"}"
echo.
pause
goto :menu

:dispatch
echo.
echo  --- Dispatch Event ---
set /p TOKEN="Bearer token: "
set /p EVENT="Event type [order.created]: "
if "%EVENT%"=="" set EVENT=order.created
set /p ORDER_ID="Entity ID [SO-50001]: "
if "%ORDER_ID%"=="" set ORDER_ID=SO-50001

curl -s -X POST "%API_BASE%/integration/dispatch" ^
  -H "Authorization: Bearer %TOKEN%" ^
  -H "Content-Type: application/json" ^
  -d "{\"eventType\":\"%EVENT%\",\"entityType\":\"SalesOrder\",\"entityId\":\"%ORDER_ID%\"}"
echo.
pause
goto :menu

:health
echo.
echo  --- Health Check ---
curl -s "%API_BASE%/../health"
echo.
pause
goto :menu
