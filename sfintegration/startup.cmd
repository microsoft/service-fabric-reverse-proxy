set path=%PATH%;%FabricCodePath%
start envoy -c config.windows.ingress.json --service-cluster ingress_proxy --service-node ingress_node -l debug
dotnet sfintegration.dll
