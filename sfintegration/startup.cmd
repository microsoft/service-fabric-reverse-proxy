set path=%PATH%;%FabricCodePath%
echo %PATH% > ./startup.log

echo GatewayMode= >> ./startup.log
echo %GatewayMode% >> ./startup.log

echo Fabric_NodeIPOrFQDN= >> ./startup.log
echo %Fabric_NodeIPOrFQDN% >> ./startup.log

if exist "%Fabric_Folder_Application%\Resolver.Endpoints.txt" (
for /f "tokens=4 delims=;" %%i in (%Fabric_Folder_Application%\Resolver.Endpoints.txt) do set Fabric_Endpoint_GatewayProxyResolverEndpoint=%%i
)

echo Fabric_Endpoint_GatewayProxyResolverEndpoint
echo %Fabric_Endpoint_GatewayProxyResolverEndpoint%

if /i "%Gateway_Resolver_Uses_Dynamic_Port%"=="false" (
set Fabric_Endpoint_GatewayProxyResolverEndpoint=19079
)

echo Fabric_Endpoint_GatewayProxyResolverEndpoint= >> ./startup.log
echo %Fabric_Endpoint_GatewayProxyResolverEndpoint% >> ./startup.log

set "Gateway_Proxy_Resolver_Endpoint=tcp://%Fabric_NodeIPOrFQDN%:%Fabric_Endpoint_GatewayProxyResolverEndpoint%"
echo Gateway_Proxy_Resolver_Endpoint=  >> ./startup.log
echo %Gateway_Proxy_Resolver_Endpoint%  >> ./startup.log

if /i NOT "%GatewayMode%"=="true" (
set "Gateway_Proxy_Resolver_Endpoint=tcp://127.0.0.1:5000"
)

echo Check if Fabric_NodeIPOrFQDN is IPAddress or FQDN
CheckHostName.exe %Fabric_NodeIPOrFQDN%

if %ERRORLEVEL% == 1 (
echo CheckHostName detected IPAddress
set "DISCOVERYTYPEVALUE=static"
) else if %ERRORLEVEL% == 2 (
echo CheckHostName detected DNS Name
set "DISCOVERYTYPEVALUE=logical_dns"
) else (
echo Unable to determine if  %Fabric_NodeIPOrFQDN% is IP or DNS. Exiting.
exit /b 1
)

@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "TEMPFILE=config.json.temp"
set "CONFIGFILE=config.gateway.json"
set "SEARCHTEXT=tcp://127.0.0.1:5000"

for /f "delims=" %%A in ('type "%CONFIGFILE%"') do (
    set "string=%%A"
    set "modified=!string:%SEARCHTEXT%=%Gateway_Proxy_Resolver_Endpoint%!"
    echo !modified!>>"%TEMPFILE%"
)

move "%TEMPFILE%" "%CONFIGFILE%"

set "SEARCHTEXT=DISCOVERYTYPE"

for /f "delims=" %%A in ('type "%CONFIGFILE%"') do (
    set "string=%%A"
    set "modified=!string:%SEARCHTEXT%=%DISCOVERYTYPEVALUE%!"
    echo !modified!>>"%TEMPFILE%"
)

move "%TEMPFILE%" "%CONFIGFILE%"

@echo on
echo start envoy -c config.gateway.json --service-cluster reverse_proxy --service-node ingress_node -l debug  >> ./startup.log
envoy -c config.gateway.json --service-cluster reverse_proxy --service-node ingress_node -l debug

if /i not "%GatewayMode%"=="true" (
echo dotnet sfintegration.dll >> ./startup.log 2>&1
dotnet sfintegration.dll >> ./startup.log 2>&1
)

