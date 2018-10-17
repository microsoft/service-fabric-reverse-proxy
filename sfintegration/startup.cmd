@echo off
setlocal EnableExtensions EnableDelayedExpansion

if not exist .\log mkdir .\log
set path=%PATH%;%FabricCodePath%

echo Environment
set
echo.

:waitforendpointfile
if not exist "%Fabric_Folder_Application%\Resolver.Endpoints.txt" (
    ping -n 10 127.0.0.1 > nul
    goto :waitforendpointfile
)

if exist "%Fabric_Folder_Application%\Resolver.Endpoints.txt" (
    for /f "tokens=4 delims=;" %%i in (%Fabric_Folder_Application%\Resolver.Endpoints.txt) do set Fabric_Endpoint_GatewayProxyResolverEndpoint=%%i
)

echo Run startup to generate enoy config 
startup.exe config.template.json config.gateway.json

set ENVOYCMD=envoy.exe -c config.gateway.json --service-cluster gateway_proxy --service-node ingress_node -l info
echo %ENVOYCMD%

%ENVOYCMD% 

echo envoy exited. Sleeping ...

ping -n 3600 127.0.0.1 > nul
