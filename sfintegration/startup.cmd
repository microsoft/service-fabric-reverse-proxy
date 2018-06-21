set path=%PATH%;%FabricCodePath%
echo %PATH% > ./startup.log
echo start envoy -c config.windows.json --service-cluster reverse_proxy --service-node ingress_node -l debug  >> ./startup.log
start envoy -c config.windows.json --service-cluster reverse_proxy --service-node ingress_node -l debug
echo dotnet sfintegration.dll >> ./startup.log 2>&1
dotnet sfintegration.dll >> ./startup.log 2>&1
pause
