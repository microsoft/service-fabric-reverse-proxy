---
title: Reverse Proxy for Linux Service Fabric Clusters
...

Windows Service Fabric Clusters have a built in Reverse proxy that makes service discovery and communication easy. We not have a Reverse Proxy for Linux to bring the same benefits to Linux Clusters.

Reverse Proxy for Linux is different from the Reverse Proxy for Windows
in multiple ways.

## Built using OSS Envoy Proxy
When we were porting Service Fabric to Linux, we evaluated existing reverse proxies in OSS as opposed to porting our own. We have many different criteria in this evaluation and based on those we decided to Adopt Envoy Proxy. We are working in parallel to bring Envoy Proxy based Reverse Proxy to Windows Clusters.

## Deployment
Reverse Proxy on Linux can be deployed like any other application in the cluster. Enabling Reverse proxy requires deploying an application to the cluster and other applications communicating with it. There is no requirement to enable it at cluster deployment time neither is there a need to upgrade the cluster to enable it after the fact. Updating the reverse proxy requires upgrading the Reverse Proxy application.

Envoy Proxy and Service Fabric integration code is bundled into a Docker container Image on Docker Hub. A Docker Compose file is used to deploy it to cluster

## Routing parameters as headers
Windows based Reverse Proxy required including advanced routing information either in the url and/or as query parameters. This resulted in mixing Service specific information with routing information. Starting with Reverse Proxy for Linux we are moving to using headers for specifying routing information.

## Customizing Reverse Proxy
### Reverse Proxy and Service Fabric communication
Reverse Proxy must discover information about the services that are executing in the cluster and track any changes in the cluster. Service Fabric exposes discovery APIs through a secure endpoint that Reverse Proxy communicates with for discovering the information.

To setup communication with the Service Fabric endpoint, Revers Proxy must provide a Client Certificate and validate the certificate provided by Management endpoint.

Information about the certificates can be specified to Reverse proxy using the following environment variables in Docker Compose file.

+ **SF_ClientCertCommonName**: Common name of the Client Certificate(s) that Reverse Proxy should present to Service Fabric endpoint. Presence of this environment variable indicates a secure cluster.

    *Required*: For secure cluster.

+ **SF_ClientCertIssuerThumbprints**: Comma separated list of thumbprint of the certificate(s) specified in SF_ClientCertCommonName.

    *Optional*: Required if the SF_ClientCertCommonName is self-signed.

Information about the certificates that Service Fabric uses that Reverse
Proxy should validate.

+ **SF_ClusterCertCommonNames**: Comma separated list of common name and alternate common names of the certificate(s) used by Service Fabric to secure the cluster.

    *Optional*: This is optional if cluster uses the same Certificate as Cluster and Client Certificate. If not specified and cluster is secure, uses the value specified for SF_ClientCertCommonName.

+ **SF_ClusterCertIssuerThumbprints**: Comma separated list of thumbprints of Cluster certificate(s).

    *Optional*: Required if the name(s) specified in SF_ClusterCertCommonNames are self-signed certificates. If not specified and cluster is secure, uses the values specified for SF_ClientCertIssuerThumbprints.

### Setting that control request handling
The main functionality of a Reverse Proxy is to receive requests from clients and route them to an upstream service based on routing information. Following variables in Docker Compose file controls various settings control how requests are handled.

+ **HttpRequestConnectTimeoutMs**: Timeout for new network connections specified in milliseconds.

    *Optional*: If not specified, will use default value of 5000ms is used.

+ **DefaultHttpRequestTimeoutMs**: Timeout for the request specified in milliseconds.

    *Optional*: If not specified, will use default value of 120000ms is used.

+ **RemoveServiceResponseHeaders**: A comma separated list of headers to remove from Service Response before returning the response to client.

    *Optional*: If not specified, no headers are removed from response.

### Reverse Proxy listener to handle https connections
Reverse proxy needs a certificate and its private key in order to secure the listener. 

+ **ReverseProxyCertThumbprint**: Thumbprint of the certificate.

If the certificate is specified in cluster manifest, Service Fabric ensures that the certificate is copied to node and put under /var/lib/sfcerts. Otherwise,  place following files under /var/lib/sfcerts on each node in the cluster. 
+ <Thumbprint>.crt
+ <Thumbprint>.prv

In addition, set the following environment variable to enable secure listener.

+ **UseHttps**: Specifies whether Reverse proxy listener should secure all communications related to request handling.

### Specifying certificate for Reverse Proxy to communicate with a secure upstream service

+ **ServiceCertificateHash**: Thumbprint of the certificate that is presented by a secure Service.

+ **ServiceCertificateAlternateNames**: Comma separated list of alternate names. Reverse Proxy will validate that one of the names matches one of the server certificate’s subject alternate names.

## Docker Compose file contents
By default, the Docker Compose file has the following settings.
+   Service Name: Reverse_Proxy_Service (Do not change)

+   Instance Count: -1 i.e. runs an instance of the service on every node in the cluster (Do not change)

+   Image name: path to Docker image on Docker hub (Do not change)

+   Volumes: Maps the folder with certificates inside the container. (Do not change)

+   Environment variables: none set. Add the variables appropriate for the cluster

+   Port: 19081

    +   To change the port that Reverse proxy listens on, say **20001**, change ports section to **20001**:19081

    +   Make sure that the second value specified in the ports section is always 19081

## Deploying Reverse Proxy
+   Make sure that Service Fabric CLI is installed. [SFCtl](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-cli)

+   Login azure using CLI

+   Connect to Service Fabric Cluster

    +   sfctl cluster select –endpoint &lt;endpoint of Service Fabric cluster&gt;

+   Download and modify Docker Compose file

+   Deploy Reverse Proxy to Cluster

    +   sfctl compose create --deployment-name Reverse_Proxy_Application --file-path ./reverseproxy.yml

+   Remove Reverse Proxy from cluster

    +   sfctl compose remove --deployment-name Reverse_Proxy_Application

## Reverse Proxy URL Format and Headers
Following is the general format of to use Reverse proxy to route requests to a service.

http(s)://localhost:reverse_proxy_port/application_name/service_name/…

Headers to control routing

-   PartitionKey – Integer key. Reverse Proxy will route the request to the Stateful partition that handles the partition key

-   ListenerName – Listener name. Name of the listener in the service that the request has to be routed to

## Using Reverse Proxy
Following assumes that Reverse Proxy listens on port 19081 (default setting in Docker Compose file) and listening on http. Change the port number to match the value specified in Compose file if it has been changed. If using a https listener, change http to https in the following examples.

Reverse proxy will listen on the following address on all the nodes in the cluster.

-   <http://localhost:19081> or <https://localhost:19081>

To send request to a service with name fabric:/applicationname/servicename and call API /api/values through Reverse Proxy, create url as follows

-   <http://localhost:19081/applicationname/servicename/api/values>

Routing to Listener with Name ServiceEndpoint1 in service fabric:/applicationname/servicename use the following.

-   URL: <http://localhost:19081/applicationname/servicename/api/values>

-   Set following headers

    -   ListenerName: ServiceEndpoint1

Routing to partition that handles data with Partition Key == 1
-   URL: <http://localhost:19081/applicationname/servicename/api/values>

-   Set following headers

    -   PartitionKey: 1

Routing to partition that handles data with Partition Key == 1 and Listener named ServiceEndpoint1
-   URL: <http://localhost:19081/applicationname/servicename/api/values>

-   Set following headers

    -   PartitionKey: 1
    -   ListenerName: ServiceEndpoint1

## LAD 3.0 to collects logs
All logs from reverse proxy are stored in /var/log/sfreverseproxy folder in the container.

 
1. Perform an ARM template update to remove LAD 2.3: i.e Rremove following section from template if present and deploy. LAD 2.3 does not have the capability to upload files to storage. If LAD 2.3 is not installed, skip this step.

```json 
{
    "properties": {
        "publisher": "Microsoft.OSTCExtensions",
        "type": "LinuxDiagnostic",
        "typeHandlerVersion": "2.3",
        "autoUpgradeMinorVersion": true,
        "settings": {
            "xmlCfg": "",
            "StorageAccount": "<accountname>"
        }
    },
    "name": "<VMDiagnosticsVmExt_Name>"
}
```
2. Create new storage account (It is recommended).

3. Generate SAS key for the new storage account using the following. Alternatively, this can also be generated through portal.

```bash
az login 
az account set --subscription <your_azure_subscription_id>
az storage account generate-sas --account-name newstorageaccountname --expiry 9999-12-31T23:59Z --permissions wlacu --resource-types co --services bt -o tsv
```
	
4. Perform ARM template update to install LAD 3.0. LAD captures new text lines as they are written to the file and writes them to table rows and/or any specified sinks (JsonBlob or EventHub). Add following section to template under the "extensionProfile" -> "extensions" section and deploy:

```json
{
    "properties": {
        "publisher": "Microsoft.Azure.Diagnostics",
        "type": "LinuxDiagnostic",
        "typeHandlerVersion": "3.0",
        "autoUpgradeMinorVersion": true,
        "settings": {
            "StorageAccount": "<newaccountname>",
            "fileLogs": [
                {
                    "file": "/var/log/sfreverseproxy/stdout.log",
                    "table": "SFReverseProxyDebugLog",
                    "sinks": ""
                },
                {
                    "file": "/var/log/sfreverseproxy/request.log",
                    "table": "SFReverseProxyRequestLog",
                    "sinks": ""
                }
            ],
            "ladCfg": ""
        },
        "protectedSettings": {
            "storageAccountName": "<newaccountname>",
            "storageAccountSasToken": "<newaccount SAS token>"
        }
    },
    "name": "<VMDiagnosticsVmExt_Name>"
}
```
5. The logs will be visible under the configured table name.
 For more information about LAD, refer to [Linux Diagnostic Extension](https://docs.microsoft.com/en-us/azure/virtual-machines/linux/diagnostic-extension)

