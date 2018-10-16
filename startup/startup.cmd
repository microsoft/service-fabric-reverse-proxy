set path=%PATH%;%FabricCodePath%
echo %PATH% > ./startup.log

if exist "%Fabric_Folder_Application%\Resolver.Endpoints.txt" (
for /f "tokens=4 delims=;" %%i in (%Fabric_Folder_Application%\Resolver.Endpoints.txt) do set Fabric_Endpoint_GatewayProxyResolverEndpoint=%%i
)

echo Fabric_Endpoint_GatewayProxyResolverEndpoint= >> ./startup.log
echo %Fabric_Endpoint_GatewayProxyResolverEndpoint% >> ./startup.log

echo Run startup to generate enoy config 
startup.exe config.template.json config.gateway.json

@echo on
echo start envoy -c config.gateway.json --service-cluster reverse_proxy --service-node ingress_node -l debug  >> ./startup.log
envoy -c config.gateway.json --service-cluster reverse_proxy --service-node ingress_node -l info

if /i not "%GatewayMode%"=="true" (
echo dotnet sfintegration.dll >> ./startup.log 2>&1
dotnet sfintegration.dll >> ./startup.log 2>&1
)

