@echo off
setlocal EnableExtensions EnableDelayedExpansion

if not exist .\log mkdir .\log
set STARTUP_LOG=.\log\startup.log
echo STARTUP_LOG=%STARTUP_LOG%
echo.

set path=%PATH%;%FabricCodePath%

echo Environment
set
echo.

echo PATH=%PATH% > ./%STARTUP_LOG%
echo.  >> ./%STARTUP_LOG%
echo PATH=%PATH% > ./%STARTUP_LOG%
echo.  >> ./%STARTUP_LOG%

echo GatewayMode=%GatewayMode% >> ./%STARTUP_LOG%
echo.  >> ./%STARTUP_LOG%

echo Fabric_NodeIPOrFQDN=%Fabric_NodeIPOrFQDN% >> ./%STARTUP_LOG%
echo.  >> ./%STARTUP_LOG%

:waitforendpointfile
if not exist "%Fabric_Folder_Application%\Resolver.Endpoints.txt" (
    ping -n 10 127.0.0.1 > nul
    goto :waitforendpointfile
)

if exist "%Fabric_Folder_Application%\Resolver.Endpoints.txt" (
    for /f "tokens=4 delims=;" %%i in (%Fabric_Folder_Application%\Resolver.Endpoints.txt) do set Fabric_Endpoint_GatewayProxyResolverEndpoint=%%i
)

if /i "%Gateway_Resolver_Uses_Dynamic_Port%"=="false" (
    set Fabric_Endpoint_GatewayProxyResolverEndpoint=19079
)

echo Fabric_Endpoint_GatewayProxyResolverEndpoint=%Fabric_Endpoint_GatewayProxyResolverEndpoint% >> ./%STARTUP_LOG%
echo.  >> ./%STARTUP_LOG%

set "Gateway_Proxy_Resolver_Endpoint=tcp://%Fabric_NodeIPOrFQDN%:%Fabric_Endpoint_GatewayProxyResolverEndpoint%"
echo Gateway_Proxy_Resolver_Endpoint=%Gateway_Proxy_Resolver_Endpoint%  >> ./%STARTUP_LOG%
echo. >> ./%STARTUP_LOG%

if /i NOT "%GatewayMode%"=="true" (
    set "Gateway_Proxy_Resolver_Endpoint=tcp://127.0.0.1:5000"
)

set "TEMPFILE=config.json.temp"
set "CONFIGFILE=config.gateway.json"
set "SEARCHTEXT=tcp://127.0.0.1:5000"

for /f "delims=" %%A in ('type "%CONFIGFILE%"') do (
    set "string=%%A"
    set "modified=!string:%SEARCHTEXT%=%Gateway_Proxy_Resolver_Endpoint%!"
    echo !modified!>>"%TEMPFILE%"
)

move "%TEMPFILE%" "%CONFIGFILE%"

set ENVOYCMD=envoy.exe -c config.gateway.json --service-cluster gateway_proxy --service-node ingress_node -l info
echo %ENVOYCMD%  >> ./%STARTUP_LOG%
type %STARTUP_LOG%

%ENVOYCMD% 

rem if /i not "%GatewayMode%"=="true" (
rem     echo dotnet sfintegration.dll >> ./%STARTUP_LOG% 2>&1
rem     start dotnet sfintegration.dll >> ./%STARTUP_LOG% 2>&1
rem )

echo envoy exited. Sleeping ...

powershell start-sleep 3600
